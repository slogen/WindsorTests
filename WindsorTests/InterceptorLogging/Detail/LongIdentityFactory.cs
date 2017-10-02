using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Detail
{
    public struct LongIdentityFactory : ILogIdentityFactory<long>
    {
        private long _nextId;

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public LongIdentityFactory(long? firstId = null)
        {
            _nextId = firstId ?? DateTime.UtcNow.Ticks;
        }

        public long NewId() => Interlocked.Increment(ref _nextId);

        public override bool Equals(object obj)
        {
            return obj is LongIdentityFactory && ((LongIdentityFactory) obj)._nextId == _nextId;
        }

        public static bool operator ==(LongIdentityFactory left, LongIdentityFactory right)
            => left._nextId == right._nextId;

        public static bool operator !=(LongIdentityFactory left, LongIdentityFactory right) => !(left == right);
        public override int GetHashCode() => base.GetHashCode();
    }
}