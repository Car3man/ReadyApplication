using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RGN;
using RGN.Impl.Firebase;
using RGN.ImplDependencies.Core.Auth;
using RGN.Modules.SignIn;

namespace ReadyApplication.Core
{
    public delegate void ReadyAppInitializedDelegate();
    public delegate void UserAuthStateChangedDelegate(AuthState authState);
    
    public interface IReadyApp
    {
        bool HasInitialized { get; }
        EnumRGNEnvironment Environment { get; }
        string AppId { get; }
        IUser User { get; }
        bool IsLoggedIn { get; }
        event ReadyAppInitializedDelegate Initialized;
        event UserAuthStateChangedDelegate UserAuthStateChanged;
		Task InitializeAsync(CancellationToken cancellationToken = default);
        IFluentAction<bool> SignIn(CancellationToken cancellationToken = default);
        IFluentAction<bool> SignInAsGuest(CancellationToken cancellationToken = default);
        IFluentAction SignOut(CancellationToken cancellationToken = default);
    }

    public class BaseReadyApp : MonoBehaviour, IReadyApp
    {
        [SerializeField] private bool autoInitOnStart;
        [SerializeField] private bool autoGuestSignIn = true;

        private bool _isInitializing;
        private bool _hasInitialized;
        private TaskCompletionSource<AuthState> _waitForInitAuthTask;

        public bool HasInitialized => _hasInitialized;
        public EnumRGNEnvironment Environment => RGNCoreBuilder.I.Dependencies.ApplicationStore.GetRGNEnvironment;
        public string AppId => RGNCoreBuilder.I.AppIDForRequests;
        public IUser User => RGNCoreBuilder.I.MasterAppUser;
        public bool IsLoggedIn => RGNCoreBuilder.I.IsLoggedIn;

		public event ReadyAppInitializedDelegate Initialized;
		public event UserAuthStateChangedDelegate UserAuthStateChanged;

		protected virtual async void Start()
        {
            if (autoInitOnStart && !_isInitializing && !_hasInitialized)
            {
                await InitializeAsync(this.GetDestroyCancellationToken());
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (RGNCoreBuilder.Initialized)
            {
                RGNCoreBuilder.I.AuthenticationChanged -= OnAuthStateChangeInternal;
                RGNCoreBuilder.Dispose();
            }
        }
        
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_hasInitialized)
            {
                return;
            }
            if (_isInitializing)
            {
                throw new System.Exception("Another initialization process was started for ReadyApp.");
            }
            _isInitializing = true;
            await InitializeSdkAsync(cancellationToken);
            await InitializeAuthAsync(cancellationToken);
            await PostInitializationAsync(cancellationToken);
            _hasInitialized = true;
            _isInitializing = false;
            OnAppReady();
            OnAuthStateChangeInternal(RGNCoreBuilder.I.CurrentAuthState);
            Initialized?.Invoke();
        }
        
        public IFluentAction<bool> SignIn(CancellationToken cancellationToken = default)
        {
            return new FluentAction<bool>(async () =>
            {
                TaskCompletionSource<bool> signInCompletionTask = new TaskCompletionSource<bool>();
                try
                {
                    EmailSignInModule.I.TryToSignIn(success => signInCompletionTask.SetResult(success));
                }
                catch (System.Exception exception)
                {
                    signInCompletionTask.SetException(exception);
                }
                bool result = await signInCompletionTask.Task;
                cancellationToken.ThrowIfCancellationRequested();
                await PostSignInAsync(cancellationToken);
                return result;
            });
        }

        public IFluentAction<bool> SignInAsGuest(CancellationToken cancellationToken = default)
        {
            return new FluentAction<bool>(async () =>
            {
                TaskCompletionSource<bool> signInCompletionTask = new TaskCompletionSource<bool>();
                try
                {
                    GuestSignInModule.I.TryToSignInAsync(success => signInCompletionTask.SetResult(success));
                }
                catch (System.Exception exception)
                {
                    signInCompletionTask.SetException(exception);
                }
                bool result = await signInCompletionTask.Task;
                cancellationToken.ThrowIfCancellationRequested();
                await PostSignInAsync(cancellationToken);
                return result;
            });
        }
        
        public IFluentAction SignOut(CancellationToken cancellationToken = default)
        {
            return new FluentAction(async () =>
            {
                EmailSignInModule.I.SignOut();
                
                if (autoGuestSignIn)
                {
                    await SignInAsGuest(cancellationToken);
                }
            });
        }
        
        private async Task InitializeSdkAsync(CancellationToken cancellationToken = default)
        {
            RGNCoreBuilder.CreateInstance(new Dependencies());
            RGNCoreBuilder.I.AuthenticationChanged += OnAuthStateChangeInternal;
            _waitForInitAuthTask = new TaskCompletionSource<AuthState>();
            await RGNCoreBuilder.BuildAsync(cancellationToken);
            await _waitForInitAuthTask.Task;
            _waitForInitAuthTask = null;
            cancellationToken.ThrowIfCancellationRequested();
            OnSdkReady();
        }
        
        private async Task InitializeAuthAsync(CancellationToken cancellationToken = default)
        {
            if (autoGuestSignIn && !RGNCoreBuilder.I.IsLoggedIn)
            {
                bool isGuestSignInSuccess = await SignInAsGuest(cancellationToken).Retry(3);
                if (!isGuestSignInSuccess)
                {
                    throw new System.Exception("ReadyApp initialization was completed with error. Auto guest sign in failed.");
                }
            }
            OnAuthReady();
        }
        
        protected virtual Task PostInitializationAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
        
        protected virtual Task PostSignInAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
        
        private void OnAuthStateChangeInternal(AuthState authState)
        {
            if (!_hasInitialized && _waitForInitAuthTask == null)
            {
                return;
            }
            if (_waitForInitAuthTask != null)
            {
                _waitForInitAuthTask.SetResult(authState);
                return;
            }
            OnAuthenticationChange(authState);
            UserAuthStateChanged?.Invoke(authState);
        }
        
        protected virtual void OnSdkReady() { }
        protected virtual void OnAuthReady() { }
        protected virtual void OnAppReady() { }
        protected virtual void OnAuthenticationChange(AuthState authState) { }
    }
}