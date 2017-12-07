using System;
using System.Threading;
using System.Threading.Tasks;

namespace WindsorTests.Util.Persist.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}