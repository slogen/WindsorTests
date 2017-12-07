using System.Data.Entity;

namespace WindsorTests.Util.Persist.EF
{
    public class DbContextUnitOfWork<TContext> : DbContextUnitOfWorkBase<TContext> where TContext : DbContext
    {
        public DbContextUnitOfWork(TContext context)
        {
            Context = context;
        }

        public override TContext Context { get; }
    }
}