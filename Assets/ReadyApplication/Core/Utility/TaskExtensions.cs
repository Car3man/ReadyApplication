using System.Threading;
using UnityEngine;

namespace ReadyApplication.Core
{
    public static class TaskExtensions
    {
#if UNITY_2022_2_OR_NEWER
        /// <summary>This CancellationToken is canceled when the MonoBehaviour will be destroyed.</summary>
        public static CancellationToken GetDestroyCancellationToken(this MonoBehaviour monoBehaviour)
        {
            return monoBehaviour.destroyCancellationToken;
        }
#endif
        /// <summary>This CancellationToken is canceled when the MonoBehaviour will be destroyed.</summary>
        public static CancellationToken GetDestroyCancellationToken(this GameObject gameObject)
        {
            return gameObject.GetAsyncDestroyTrigger().CancellationToken;
        }
        /// <summary>This CancellationToken is canceled when the MonoBehaviour will be destroyed.</summary>
        public static CancellationToken GetDestroyCancellationToken(this Component component)
        {
#if UNITY_2022_2_OR_NEWER
            if (component is MonoBehaviour mb)
            {
                return mb.destroyCancellationToken;
            }
#endif
            return component.GetAsyncDestroyTrigger().CancellationToken;
        }
        /// <summary>This CancellationToken is canceled when the App is quiting.</summary>
        public static CancellationToken GetAppQuitCancellationToken(this MonoBehaviour monoBehaviour)
        {
	        return GetAppQuitCancellationToken();
        }
        /// <summary>This CancellationToken is canceled when the App is quiting.</summary>
		public static CancellationToken GetAppQuitCancellationToken(this GameObject gameObject)
        {
	        return GetAppQuitCancellationToken();
        }
        /// <summary>This CancellationToken is canceled when the App is quiting.</summary>
		public static CancellationToken GetAppQuitCancellationToken(this Component component)
        {
            return GetAppQuitCancellationToken();
        }
        /// <summary>This CancellationToken is canceled when the App is quiting.</summary>
		public static CancellationToken GetAppQuitCancellationToken()
        {
#if UNITY_2022_2_OR_NEWER
			return Application.exitCancellationToken;
#else
			return AsyncTriggerExtensions.GetAsyncAppQuitTrigger().CancellationToken;
#endif
        }
    }
    public static class AsyncTriggerExtensions
    {
        public static AsyncDestroyTrigger GetAsyncDestroyTrigger(this GameObject gameObject)
        {
            return GetOrAddComponent<AsyncDestroyTrigger>(gameObject);
        }
        public static AsyncDestroyTrigger GetAsyncDestroyTrigger(this Component component)
        {
            return component.gameObject.GetAsyncDestroyTrigger();
        }
        public static AsyncAppQuitTrigger GetAsyncAppQuitTrigger()
        {
	        var appQuitTrigger = Object.FindFirstObjectByType<AsyncAppQuitTrigger>();
	        if (appQuitTrigger == null)
	        {
                GameObject appQuitTriggerObject = new GameObject("AsyncAppQuitTrigger");
                appQuitTrigger = appQuitTriggerObject.AddComponent<AsyncAppQuitTrigger>();
                Object.DontDestroyOnLoad(appQuitTriggerObject);
	        }
            return appQuitTrigger;
        }
		private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
#if UNITY_2019_2_OR_NEWER
            if (!gameObject.TryGetComponent<T>(out var component))
            {
                component = gameObject.AddComponent<T>();
            }
#else
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
#endif
            return component;
        }
    }
    [DisallowMultipleComponent]
    public sealed class AsyncDestroyTrigger : MonoBehaviour
    {
        private CancellationTokenSource _cancellationTokenSource;

        public CancellationToken CancellationToken
        {
            get
            {
                _cancellationTokenSource ??= new CancellationTokenSource();
                return _cancellationTokenSource.Token;
            }
        }
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
    [DisallowMultipleComponent]
    public sealed class AsyncAppQuitTrigger : MonoBehaviour
    {
	    private CancellationTokenSource _cancellationTokenSource;

	    public CancellationToken CancellationToken
	    {
		    get
		    {
			    _cancellationTokenSource ??= new CancellationTokenSource();
			    return _cancellationTokenSource.Token;
		    }
	    }

	    private void Awake()
	    {
            Application.quitting += () =>
			{
				_cancellationTokenSource?.Cancel();
				_cancellationTokenSource?.Dispose();
			};
	    }
    }
}