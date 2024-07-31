using System;
using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN;
using RGN.Modules.Currency;
using RGN.Modules.GameProgress;

namespace ReadyApplication.Standard
{
    public interface IGameProgressService
    {
        IFluentAction<T> AddUserProgressAsync<T>(T userProgress, CancellationToken cancellationToken = default);
        IFluentAction UpdateUserProgressAsync<T>(T userProgress, List<Currency> reward = null, CancellationToken cancellationToken = default);
        IFluentAction<T> GetUserProgressAsync<T>(CancellationToken cancellationToken = default);
    }

    public class GameProgressService : IGameProgressService
    {
        private readonly IUserCurrencyRepository _userCurrencyRepository;
        private readonly CacheDictionary<Type, object> _userProgressCache;

        public GameProgressService(IReadyApp readyApp, IUserCurrencyRepository userCurrencyRepository)
        {
            _userCurrencyRepository = userCurrencyRepository;
            _userProgressCache = new CacheDictionary<Type, object>(TimeSpan.FromSeconds(60));
            
            readyApp.UserAuthStateChanged += authState =>
            {
                if (authState.LoginState == EnumLoginState.NotLoggedIn)
                {
                    _userProgressCache.Clear();
                }
            };
        }

        public IFluentAction<T> AddUserProgressAsync<T>(T userProgress, CancellationToken cancellationToken = default)
        {
            return new FluentAction<T>(async () =>
            {
                T updatedUserProgress = await GameProgressModule.I.AddUserProgressAsync(userProgress, cancellationToken);
                _userProgressCache[typeof(T)] = updatedUserProgress;
                return updatedUserProgress;
            });
        }

        public IFluentAction UpdateUserProgressAsync<T>(T userProgress, List<Currency> reward = null, CancellationToken cancellationToken = default)
        {
            return new FluentAction(async () =>
            {
                UpdateUserLevelResponseData<T> updateUserProgressResponse = await GameProgressModule.I.UpdateUserProgressAsync(userProgress, reward, cancellationToken);
                _userProgressCache[typeof(T)] = updateUserProgressResponse.playerProgress;
                if (reward != null)
                {
                    _userCurrencyRepository.CachedCurrencies.Clear();
                    reward.ForEach(currency => _userCurrencyRepository.CachedCurrencies[currency.name] = currency);
                };
            });
        }

        public IFluentAction<T> GetUserProgressAsync<T>(CancellationToken cancellationToken = default)
        {
            return new FluentAction<T>(async () =>
            {
                if (_userProgressCache.TryGetValue(typeof(T), out object userProgressInCache))
                {
                    return (T)userProgressInCache;
                }
                GetPlayerLevelResponseData<T> getUserProgressResponse = await GameProgressModule.I.GetUserProgressAsync<T>(cancellationToken);
                _userProgressCache[typeof(T)] = getUserProgressResponse.playerProgress;
                return getUserProgressResponse.playerProgress;
            });
        }
    }
}