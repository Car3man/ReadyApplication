using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace ReadyApplication.Core
{
    public class UnityMainThreadDispatcher
    {
        private static readonly ConcurrentQueue<System.Action> ActionQueue = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var subscription = new PlayerLoopSystemSubscription<Update>(Update);
            Application.quitting += subscription.Dispose;
        }

        private static void Update()
        {
            while (ActionQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        public static void Enqueue(System.Action action)
        {
            ActionQueue.Enqueue(action);
        }
    }
    
    internal class PlayerLoopSystemSubscription<T> : System.IDisposable
    {
        private readonly System.Action _callback;

        public PlayerLoopSystemSubscription(System.Action callback)
        {
            _callback = callback;
            Subscribe();
        }

        private void Invoke()
        {
            _callback.Invoke();
        }

        private void Subscribe()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            ref var system = ref loop.Find<T>();
            system.updateDelegate += Invoke;
            PlayerLoop.SetPlayerLoop(loop);
        }

        private void Unsubscribe()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            ref var system = ref loop.Find<T>();
            system.updateDelegate -= Invoke;
            PlayerLoop.SetPlayerLoop(loop);
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }
    
    internal static class PlayerLoopSystemExtensions
    {
        public static ref PlayerLoopSystem Find<T>(this PlayerLoopSystem root)
        {
            for (int i = 0; i < root.subSystemList.Length; i++)
            {
                if (root.subSystemList[i].type == typeof(T))
                {
                    return ref root.subSystemList[i];
                }
            }

            throw new System.Exception($"System of type '{typeof(T).Name}' not found inside system '{root.type.Name}'.");
        }
    }
}