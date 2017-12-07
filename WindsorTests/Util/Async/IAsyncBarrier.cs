using System.Threading;
using System.Threading.Tasks;

namespace WindsorTests.Util.Async
{
    public interface IAsyncBarrier
    {
        /// <summary>
        /// Amount of participants required to break barrier
        /// </summary>
        long ParticipantCount { get; }

        /// <summary>
        /// Amount of participants pending to break barrier
        /// </summary>
        long Pending { get; }

        /// <summary>
        /// Signal once, and return a task that completes successfully when the barrier is broken.
        /// 
        /// If any waiters cancellationToken is pulled before barrier is broken, then the Task will be cancelled
        /// </summary>
        Task SignalAndWait(CancellationToken cancellationToken);
    }
}