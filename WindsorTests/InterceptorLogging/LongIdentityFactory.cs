using System;
using System.Threading;

namespace WindsorTests.InterceptorLogging
{
    public struct LongIdentityFactory : ILogIdentityFactory<long>
    {
        private long _nextId;

        public LongIdentityFactory(long? firstId = null)
        {
            _nextId = firstId ?? DateTime.UtcNow.Ticks;
        }

        public long Next() => Interlocked.Increment(ref _nextId);
    }
}