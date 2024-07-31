using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ReadyApplication.Core
{
    public class AwaiterFluentAction : INotifyCompletion
    {
        private readonly Task _task;

        public AwaiterFluentAction(Task task)
        {
            _task = task;
        }

        public bool IsCompleted => _task.IsCompleted;

        public void GetResult() => _task.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation)
        {
            if (_task.IsCompleted)
            {
                continuation();
            }
            else
            {
                _task.ContinueWith(_ => UnityMainThreadDispatcher.Enqueue(continuation));
            }
        }
    }
    
    public class AwaiterFluentAction<TValue> : INotifyCompletion
    {
        private readonly Task<TValue> _task;

        public AwaiterFluentAction(Task<TValue> task)
        {
            _task = task;
        }

        public bool IsCompleted => _task.IsCompleted;

        public TValue GetResult() => _task.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation)
        {
            if (_task.IsCompleted)
            {
                continuation();
            }
            else
            {
                _task.ContinueWith(_ => UnityMainThreadDispatcher.Enqueue(continuation));
            }
        }
    }
}