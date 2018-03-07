using System;
using Castle.Windsor;
using FluentAssertions;

namespace WindsorTests.Lifestyle.Tests
{
    internal static class WindsorTestExtensions
    {
        public static void ResolveAndCount<T>(this IWindsorContainer cw, Action<T> act, int expectCount)
        {
            Func<T, int> f = t =>
            {
                act(t);
                return 0;
            };
            cw.ResolveAndCount(f, expectCount);
        }

        public static TResult ResolveAndCount<T, TResult>(this IWindsorContainer cw, Func<T, TResult> act,
            int expectCount)
        {
            var td = IdTrack.TotalDisposedCount;
            var ud = IdTrack.TotalUndisposedCount;
            var t = cw.Resolve<T>();
            TResult result;
            try
            {
                result = act(t);
            }
            finally
            {
                cw.Release(t);
            }
            IdTrack.TotalUndisposedCount.Should().Be(ud);
            IdTrack.TotalDisposedCount.Should().BeGreaterThan(td);
            return result;
        }
    }
}