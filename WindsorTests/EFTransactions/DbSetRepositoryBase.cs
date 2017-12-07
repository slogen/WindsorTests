using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WindsorTests.EFTransactions.Design.Interface;

namespace WindsorTests.EFTransactions.Design.EF
{
    public abstract class DbSetRepositoryBase<TSource> : IRepository<TSource>
        where TSource : class
    {
        protected abstract DbSet<TSource> DbSet { get; }

        public IQueryable<TSource> Items => DbSet;
        public IEnumerable<TSource> AddRange(IEnumerable<TSource> entities) => DbSet.AddRange(entities);
        public IEnumerable<TSource> RemoveRange(IEnumerable<TSource> entities) => DbSet.RemoveRange(entities);
    }
}