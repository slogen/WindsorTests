using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace WindsorTests.InterceptorLogging
{
    public abstract class TraceInterceptor<TKey> : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            var callId = MakeNextCallId();
            ReactOnEntry(callId, invocation);
            var startTime = DateTime.UtcNow;
            try
            {
                invocation.Proceed();
            }
            catch (Exception ex) // Allow avoiding getting on the stack
                when (ReactException(callId, startTime, invocation, ex))
            {
                throw; // If ReactException returns true, then we rethrow and get on the stack
            }
            if (invocation.ReturnValue is Task)
            {
                var task = (Task) invocation.ReturnValue;
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        ReactException(callId, startTime, invocation, t.Exception);
                    else if (t.IsCanceled)
                        ReactException(callId, startTime, invocation, new TaskCanceledException(task));
                    else
                        ReactOnReturn(callId, startTime, invocation, ((dynamic) t).Result);
                }, default(CancellationToken), TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
            }
            else
            {
                ReactOnReturn(callId, startTime, invocation, invocation.ReturnValue);
            }
        }

        protected abstract TKey MakeNextCallId();

        protected abstract void ReactOnEntry(TKey id, IInvocation invocation);
        protected abstract void ReactOnReturn(TKey id, DateTime startTime, IInvocation invocation, object value);
        protected abstract bool ReactException(TKey id, DateTime startTime, IInvocation invocation, Exception ex);
    }
}