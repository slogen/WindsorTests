using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using WindsorTests.Util.Persist.Base;

namespace WindsorTests.Util.Persist.EF
{
    public abstract class DbContextUnitOfWorkBase<TContext> : UnitOfWorkBase
        where TContext : DbContext
    {
        public abstract TContext Context { get; }

        protected override async Task<int> SaveChangesOnceAsync(CancellationToken cancellationToken)
            => await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Context.Dispose();
        }
    }
}