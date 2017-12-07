using System;
using System.Threading;
using System.Threading.Tasks;
using WindsorTests.Util.Persist.Interface;

namespace WindsorTests.Util.Persist.Base
{
    public abstract class UnitOfWorkBase : IUnitOfWork
    {
        private int _saveCount;
        public int SaveCount => _saveCount;

        async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        {
            var saveAttempt = Interlocked.Increment(ref _saveCount);
            if (saveAttempt != 1)
                throw new InvalidOperationException($"max 1 save per IUnitOfWork allowed, not {saveAttempt}");
            return await SaveChangesOnceAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract Task<int> SaveChangesOnceAsync(CancellationToken cancellationToken);
        protected abstract void Dispose(bool disposing);
    }
}