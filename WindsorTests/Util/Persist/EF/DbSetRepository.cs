using System.Data.Entity;

namespace WindsorTests.Util.Persist.EF
{
    public class DbSetRepository<TSource> : DbSetRepositoryBase<TSource>
        where TSource : class
    {
        public DbSetRepository(DbSet<TSource> dbSet)
        {
            DbSet = dbSet;
        }

        protected override DbSet<TSource> DbSet { get; }
    }
}