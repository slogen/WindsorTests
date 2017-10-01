using System;
using Castle.Windsor;
using FluentAssertions;

namespace WindsorTests.LifesStyle.Tests
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
            var td = IdTrack.TotalDisposeCount;
            var ud = IdTrack.TotalUnDisposeCount;
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
            IdTrack.TotalUnDisposeCount.Should().Be(ud);
            IdTrack.TotalDisposeCount.Should().BeGreaterThan(td);
            return result;
        }
    }
}