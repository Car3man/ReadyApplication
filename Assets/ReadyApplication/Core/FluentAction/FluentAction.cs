using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadyApplication.Core
{
    public delegate bool GetCachedValue<T>(out T value);
    public delegate void SetCachedValue<in T>(T value, TimeSpan ttl);
    
    public interface IFluentAction
    {
        // TODO: Timeout policy
        IFluentAction Retry(int maxAttempts = 3, int minBackoff = 1000);
        IFluentAction RetryWhen<TException>() where TException : Exception;
        IFluentAction RetryWhen<TException>(Func<TException, bool> selector) where TException : Exception;
        IFluentAction NoRetry();
        IFluentAction Fallback();
        IFluentAction Fallback(Func<Task> fallbackFunc);
        IFluentAction FallbackWhen<TException>() where TException : Exception;
        IFluentAction FallbackWhen<TException>(Func<TException, bool> selector) where TException : Exception;
        IFluentAction NoFallback();
        IFluentAction OnStart(Action startAction);
		IFluentAction OnComplete(Action completeAction);
		IFluentAction OnError(Action<Exception> errorAction);
		Task ExecuteAsync();
        AwaiterFluentAction GetAwaiter();
    }
    
    public interface IFluentAction<TResult>
    {
	    // TODO: Timeout policy
		IFluentAction<TResult> Retry(int maxAttempts = 3, int minBackoff = 1000);
        IFluentAction<TResult> RetryWhen<TException>() where TException : Exception;
        IFluentAction<TResult> RetryWhen<TException>(Func<TException, bool> selector) where TException : Exception;
        IFluentAction<TResult> NoRetry();
        IFluentAction<TResult> Fallback(Func<Task<TResult>> fallbackFunc);
        IFluentAction<TResult> FallbackWhen<TException>() where TException : Exception;
        IFluentAction<TResult> FallbackWhen<TException>(Func<TException, bool> selector) where TException : Exception;
        IFluentAction<TResult> NoFallback();
        IFluentAction<TResult> Cache(GetCachedValue<TResult> getCachedValue, SetCachedValue<TResult> setCachedValue, TimeSpan ttl);
        IFluentAction<TResult> Cache(string key, ICache cache, TimeSpan ttl);
        IFluentAction<TResult> SetCacheTtl(TimeSpan ttl);
        IFluentAction<TResult> Fresh();
        IFluentAction<TResult> RefreshIfInCache(Action<TResult> freshResult, CancellationToken cancellationToken = default);
        IFluentAction<TResult> NoCache();
        IFluentAction<TResult> OnStart(Action startAction);
        IFluentAction<TResult> OnComplete(Action<TResult> completeAction);
        IFluentAction<TResult> OnError(Action<Exception> errorAction);
        Task<TResult> ExecuteAsync();
        AwaiterFluentAction<TResult> GetAwaiter();
    }
    
    public abstract class FluentActionBase
    {
        /* Retry options */
        private bool _retryEnabled;
        private int _retryMaxAttempts;
        private int _retryMinBackoff;
        private List<Func<Exception, bool>> _retryWhenSelectors;
        /* Fallback options */
        private bool _fallbackEnabled;
        private List<Func<Exception, bool>> _fallbackWhenSelectors;
        /* Actions */
        private readonly List<Action> _startActions = new();
        private readonly List<Action<Exception>> _errorActions = new();

        protected FluentActionBase RetryInternal(int maxAttempts, int minBackoff)
        {
            _retryEnabled = true;
            _retryMaxAttempts = maxAttempts;
            _retryMinBackoff = minBackoff;
            _retryWhenSelectors = new List<Func<Exception, bool>>();
            return this;
        }
        
        protected FluentActionBase RetryWhenInternal<TException>(Func<TException, bool> selector)
        {
            if (!_retryEnabled)
            {
                throw new InvalidOperationException("RetryWhen can only be called after Retry");
            }
            _retryWhenSelectors.Add(exception => exception is TException specifiedException && selector(specifiedException));
            return this;
        }

        protected FluentActionBase NoRetryInternal()
        {
            _retryEnabled = false;
            _retryMaxAttempts = 0;
            _retryMinBackoff = 0;
            _retryWhenSelectors = null;
            return this;
        }
        
        protected FluentActionBase FallbackInternal()
        {
            _fallbackEnabled = true;
            _fallbackWhenSelectors = new List<Func<Exception, bool>>();
            return this;
        }
        
        protected FluentActionBase FallbackWhenInternal<TException>(Func<TException, bool> selector)
        {
            if (!_fallbackEnabled)
            {
                throw new InvalidOperationException("FallbackWhen can only be called after Fallback");
            }
            _fallbackWhenSelectors.Add(exception => exception is TException specifiedException && selector(specifiedException));
            return this;
        }
        
        protected FluentActionBase NoFallbackInternal()
        {
            _fallbackEnabled = false;
            _fallbackWhenSelectors = null;
            return this;
        }

        protected FluentActionBase OnStartInternal(Action startAction)
		{
			_startActions.Add(startAction);
            return this;
		}

        protected FluentActionBase OnErrorInternal(Action<Exception> errorAction)
        {
	        _errorActions.Add(errorAction);
	        return this;
        }

        protected void OnExecutionStartInternal()
		{
			_startActions.ForEach(action => action?.Invoke());
		}

		protected async Task ExecuteInternalAsync(Func<Task> actionFunc, Func<Task> fallbackFunc)
        {
            await ExecuteInternalAsync<object>(
                async () =>
                {
                    await actionFunc();
                    return null;
                },
                async () =>
                {
                    await fallbackFunc();
                    return null;
                }
            );
        }

        protected async Task<(bool, T)> ExecuteInternalAsync<T>(Func<Task<T>> actionFunc, Func<Task<T>> fallbackFunc)
        {
            try
            {
                return (true, await ExecuteWithRetriesAsync(actionFunc));
            }
            catch (Exception exception)
            {
                bool fallbackFilterFailed = _fallbackWhenSelectors is { Count: > 0 } && !_fallbackWhenSelectors.Any(selector => selector(exception));
                if (_fallbackEnabled && !fallbackFilterFailed)
                {
					return (true, await fallbackFunc());
				}

                if (_errorActions.Count > 0)
				{
	                _errorActions.ForEach(action => action?.Invoke(exception));
	                return (false, default);
				}

				throw;
            }
        }

        private async Task<T> ExecuteWithRetriesAsync<T>(Func<Task<T>> actionFunc)
        {
            int attempts = 0;
            
            while (true)
            {
                try
                {
                    return await actionFunc();
                }
                catch (Exception exception)
                {
                    attempts++;
                    
                    bool outOfAttempts = attempts >= _retryMaxAttempts;
                    bool retryFilterFailed = _retryWhenSelectors is { Count: > 0 } && !_retryWhenSelectors.Any(selector => selector(exception));
                    
                    if (!_retryEnabled || outOfAttempts || retryFilterFailed)
                    {
                        throw;
                    }
                    
                    await Task.Delay(_retryMinBackoff * attempts ^ 2);
                }
            }
        }
    }

    public class FluentAction : FluentActionBase, IFluentAction
    {
        /* Untyped action options */
        private readonly Func<Task> _actionFunc;
        private readonly List<Action> _completeActions;
        private Func<Task> _fallbackFunc;

        public FluentAction(Func<Task> actionFunc)
        {
            _actionFunc = actionFunc;
			_completeActions = new List<Action>();
        }
        
        public IFluentAction Retry(int maxAttempts, int minBackoff = 1000)
            => (IFluentAction)RetryInternal(maxAttempts, minBackoff);

        public IFluentAction RetryWhen<TException>() where TException : Exception 
            => (IFluentAction)RetryWhenInternal<TException>(_ => true);

        public IFluentAction RetryWhen<TException>(Func<TException, bool> selector) where TException : Exception
            => (IFluentAction)RetryWhenInternal(selector);

        public IFluentAction NoRetry()
            => (IFluentAction)NoRetryInternal();

        public IFluentAction Fallback()
            => Fallback(() => Task.CompletedTask);
        
        public IFluentAction Fallback(Func<Task> fallbackFunc)
        {
            _fallbackFunc = fallbackFunc;
            return (IFluentAction)FallbackInternal();
        }
        
        public IFluentAction FallbackWhen<TException>() where TException : Exception 
            => (IFluentAction)FallbackWhenInternal<TException>(_ => true);

        public IFluentAction FallbackWhen<TException>(Func<TException, bool> selector) where TException : Exception
            => (IFluentAction)FallbackWhenInternal(selector);

        public IFluentAction NoFallback()
        {
            _fallbackFunc = null;
            return (IFluentAction)NoFallbackInternal();
        }

        public IFluentAction OnStart(Action startAction)
		{
			return (IFluentAction)OnStartInternal(startAction);
		}

        public IFluentAction OnComplete(Action completeAction)
        {
            _completeActions.Add(completeAction);
            return this;
        }

        public IFluentAction OnError(Action<Exception> errorAction)
        {
	        return (IFluentAction)OnErrorInternal(errorAction);
        }

		public async Task ExecuteAsync()
        {
	        OnExecutionStartInternal();
			await ExecuteInternalAsync(_actionFunc, _fallbackFunc);
            _completeActions?.ForEach(action => action?.Invoke());
        }

        public AwaiterFluentAction GetAwaiter()
        {
            return new AwaiterFluentAction(ExecuteAsync());
        }
    }
    
    public class FluentAction<TResult> : FluentActionBase, IFluentAction<TResult>
    {
        /* Typed action options */
        private readonly Func<Task<TResult>> _actionFunc;
        private readonly List<Action<TResult>> _completeActions;
        private Func<Task<TResult>> _fallbackFunc;
        private Action<TResult> _refreshIfCacheAction;
        private CancellationToken _refreshIfCacheCancellationToken;

        /* Cache options */
        private bool _cacheEnabled;
        private TimeSpan _cacheTtl;
        private bool _cacheFreshRequest;
        private GetCachedValue<TResult> _cacheGetValueFunc;
        private SetCachedValue<TResult> _cacheSetValueFunc;

        public FluentAction(Func<Task<TResult>> actionFunc)
        {
            _actionFunc = actionFunc;
            _completeActions = new List<Action<TResult>>();
        }
        
        public IFluentAction<TResult> Retry(int maxAttempts, int minBackoff = 1000)
            => (IFluentAction<TResult>)RetryInternal(maxAttempts, minBackoff);

        public IFluentAction<TResult> RetryWhen<TException>() where TException : Exception 
            => (IFluentAction<TResult>)RetryWhenInternal<TException>(_ => true);

        public IFluentAction<TResult> RetryWhen<TException>(Func<TException, bool> selector) where TException : Exception
            => (IFluentAction<TResult>)RetryWhenInternal(selector);

        public IFluentAction<TResult> NoRetry()
            => (IFluentAction<TResult>)NoRetryInternal();
        
        public IFluentAction<TResult> Fallback(Func<Task<TResult>> fallbackFunc)
        {
            _fallbackFunc = fallbackFunc;
            return (IFluentAction<TResult>)FallbackInternal();
        }
        
        public IFluentAction<TResult> FallbackWhen<TException>() where TException : Exception
            => (IFluentAction<TResult>)FallbackWhenInternal<TException>(_ => true);

        public IFluentAction<TResult> FallbackWhen<TException>(Func<TException, bool> selector) where TException : Exception
            => (IFluentAction<TResult>)FallbackWhenInternal(selector);

        public IFluentAction<TResult> NoFallback()
        {
            _fallbackFunc = null;
            return (IFluentAction<TResult>)NoFallbackInternal();
        }

        public IFluentAction<TResult> Cache(GetCachedValue<TResult> getCachedValue, SetCachedValue<TResult> setCachedValue, TimeSpan ttl)
        {
            _cacheEnabled = true;
            _cacheGetValueFunc = getCachedValue;
            _cacheSetValueFunc = setCachedValue;
            _cacheTtl = ttl;
            return this;
        }

        public IFluentAction<TResult> Cache(string key, ICache cache, TimeSpan ttl)
        {
            return Cache(
                (out TResult cachedValue) => cache.TryGet(key, out cachedValue),
                (valueToCache, ttlToCache) => cache.Set(key, valueToCache, ttlToCache),
                ttl
            );
        }

        public IFluentAction<TResult> SetCacheTtl(TimeSpan ttl)
        {
            _cacheTtl = ttl;
            return this;
        }

        public IFluentAction<TResult> Fresh()
        {
            _cacheFreshRequest = true;
            return this;
        }

        public IFluentAction<TResult> RefreshIfInCache(Action<TResult> freshResult, CancellationToken cancellationToken = default)
		{
			_refreshIfCacheAction = freshResult;
            _refreshIfCacheCancellationToken = cancellationToken;
			return this;
		}

		public IFluentAction<TResult> NoCache()
        {
            _cacheEnabled = false;
            _cacheTtl = TimeSpan.Zero;
            _cacheFreshRequest = false;
            _cacheGetValueFunc = null;
            _cacheSetValueFunc = null;
            return this;
        }

        public IFluentAction<TResult> OnStart(Action startAction)
        {
	        return (IFluentAction<TResult>)OnStartInternal(startAction);
        }

		public IFluentAction<TResult> OnComplete(Action<TResult> completeAction)
        {
	        _completeActions.Add(completeAction);
	        return this;
        }

        public IFluentAction<TResult> OnError(Action<Exception> errorAction)
        {
	        return (IFluentAction<TResult>)OnErrorInternal(errorAction);
		}

        public async Task<TResult> ExecuteAsync()
        {
	        OnExecutionStartInternal();

            if (_cacheEnabled && !_cacheFreshRequest && _cacheGetValueFunc(out TResult cachedValue))
            {
	            if (_refreshIfCacheAction != null)
	            {
		            _ = Task.Run(async () =>
		            {
			            (bool freshRequestSuccess, TResult freshResult) = await ExecuteInternalAsync(_actionFunc, _fallbackFunc);
                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
	                        if (freshRequestSuccess)
	                        {
		                        _cacheSetValueFunc(freshResult, _cacheTtl);
	                        }
	                        _refreshIfCacheAction(freshResult);
						});
		            }, _refreshIfCacheCancellationToken);
	            }

	            _completeActions?.ForEach(action => action?.Invoke(cachedValue));
				return cachedValue;
            }
            
            (bool executionSuccess, TResult executionResult) = await ExecuteInternalAsync(_actionFunc, _fallbackFunc);
            
            if (executionSuccess && _cacheEnabled)
            {
                _cacheSetValueFunc(executionResult, _cacheTtl);
            }
            
            _completeActions?.ForEach(action => action?.Invoke(executionResult));
            return executionResult;
        }

        public AwaiterFluentAction<TResult> GetAwaiter()
        {
            return new AwaiterFluentAction<TResult>(ExecuteAsync());
        }
    }
}
