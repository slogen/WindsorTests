using System.Threading;
using System.Threading.Tasks;

namespace WindsorTests.Util.Async
{
    public class AsyncBarrier : IAsyncBarrier
    {
        private long _awaitingCount;
        private TaskCompletionSource<int> _nextTaskCompletion;

        public AsyncBarrier(long participantCount)
        {
            ParticipantCount = participantCount;
        }

        public long ParticipantCount { get; }

        public async Task SignalAndWait(CancellationToken cancellationToken)
        {
            TaskCompletionSource<int> tcs;
            lock (this)
            {
                // Will we be waiting for more?
                if (++_awaitingCount >= ParticipantCount)
                {
                    // Nope, we are there
                    _awaitingCount = 0;
                    var oldTaskCompletion = _nextTaskCompletion;
                    _nextTaskCompletion = null;
                    oldTaskCompletion?.TrySetResult(0);
                    return;
                }
                // Yes -- get taskcompletionsource to wait for
                tcs = _nextTaskCompletion ?? (_nextTaskCompletion = new TaskCompletionSource<int>());
            }
            using (cancellationToken.Register(t2 => ((TaskCompletionSource<int>) t2).TrySetCanceled(), tcs))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        public long Pending => ParticipantCount - Interlocked.Read(ref _awaitingCount);
    }
}