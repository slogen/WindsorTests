using System.Collections.Generic;
using System.Linq;

namespace WindsorTests.Util.Persist.Interface
{
    public interface IRepository<TSource>
    {
        IQueryable<TSource> Items { get; }
        IEnumerable<TSource> AddRange(IEnumerable<TSource> entities);
        IEnumerable<TSource> RemoveRange(IEnumerable<TSource> entities);
    }
}