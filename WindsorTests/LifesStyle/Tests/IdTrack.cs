using System;
using System.Threading;

namespace WindsorTests.LifesStyle.Tests
{

    #region TestStructures

    #region Identity Tracking

    public class IdTrack : IId, IDisposable
    {
        private static long _nextId;
        private static long _totalDisposeCount;
        private static long _totalUnDisposeCount;
        private long _disposeCount;
        private long _noDisposeCount;


        public IdTrack()
        {
            Id = NextId();
        }

        public static long TotalDisposeCount => Interlocked.Read(ref _totalDisposeCount);
        public static long TotalUnDisposeCount => Interlocked.Read(ref _totalUnDisposeCount);

        public void Dispose() => Dispose(true);

        public long Id { get; }
        public long DisposeCount => Interlocked.Read(ref _disposeCount);
        private static long NextId() => Interlocked.Increment(ref _nextId);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Increment(ref _disposeCount);
                Interlocked.Increment(ref _totalDisposeCount);
            }
            else
            {
                var cnt = Interlocked.Increment(ref _noDisposeCount);
                Interlocked.Increment(ref _totalUnDisposeCount);
                Console.WriteLine("Missed DisposeCount: {0}", cnt);
            }
        }

        ~IdTrack()
        {
            Dispose(false);
        }

        public override string ToString() => $"{GetType().Name}{Id}";
    }

    #endregion

    #endregion
}