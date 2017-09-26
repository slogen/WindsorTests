using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests
{
    #region TestStructures
    #region Identity Tracking
    public interface IId
    {
        long Id { get; }
        long DisposeCount { get; }
    }
    public class IdTrack : IId, IDisposable
    {

        public long Id { get; }
        private static long _nextId;
        private static long NextId() => Interlocked.Increment(ref _nextId);
        private long _disposeCount;
        private long _noDisposeCount;
        public long DisposeCount => Interlocked.Read(ref _disposeCount);
        private static long _totalDisposeCount;
        private static long _totalUnDisposeCount;
        public static long TotalDisposeCount => Interlocked.Read(ref _totalDisposeCount);
        public static long TotalUnDisposeCount => Interlocked.Read(ref _totalUnDisposeCount);



        public IdTrack()
        {
            Id = NextId();
        }

        public void Dispose() => Dispose(true);

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

    interface IA : IId
    {
        string ToStringA();
    }

    class A : IdTrack, IA
    {
        public string ToStringA() => $"{this} as IA";
    }

    interface IB : IId
    {
        string ToStringB();
    }

    class B : IdTrack, IB
    {
        public string ToStringB() => $"{this} as IB";
    }
    class C : IdTrack, IA, IB
    {
        public string ToStringA()
        {
            return $"{this} as IA";
        }
        public string ToStringB()
        {
            return $"{this} as IB";
        }
    }

    public interface IDep<out T>
    {
        T Dependency { get; }
    }


    class Dep<T> : IdTrack, IDep<T>
        where T : IId
    {
        public T Dependency { get; }

        protected Dep(T dependency)
        {
            Dependency = dependency;
        }

        public override string ToString() => $"{base.ToString()}, depend on {Dependency} as {typeof(T).Name}";
    }

    class DepA : Dep<IA>
    {
        public DepA(IA dependency) : base(dependency)
        {
        }
    }
    class DepB : Dep<IB>
    {
        public DepB(IB dependency) : base(dependency)
        {
        }
    }

    class DepAb : IdTrack
    {
        public IA A { get; }
        public IB B { get; }

        public DepAb(IA a, IB b)
        {
            A = a;
            B = b;
        }
    }

    interface IX
    {
        IA A { get; }
        IB B { get; }
        IDep<IA> Ia1 { get; }
        IDep<IA> Ia2 { get; }
        IDep<IB> Ib1 { get; }
        IDep<IB> Ib2 { get; }
    }
    class X : IX
    {
        public IA A { get; }
        public IB B { get; }
        public IDep<IA> Ia1 { get; }
        public IDep<IA> Ia2 { get; }
        public IDep<IB> Ib1 { get; }
        public IDep<IB> Ib2 { get; }

        public X(IA a, IB b, IDep<IA> ia1, IDep<IA> ia2, IDep<IB> ib1, IDep<IB> ib2)
        {
            A = a;
            B = b;
            Ia1 = ia1;
            Ib1 = ib1;
            Ib2 = ib2;
            Ia2 = ia2;
        }

    }

    #endregion

    public class WindsorTest
    {
        [Test]
        public void TestLifetimeBoundTo()
        {
            var tu = IdTrack.TotalUnDisposeCount;
            using (var cw = new WindsorContainer())
                TestLifetimeBoundTo(cw);
            IdTrack.TotalUnDisposeCount.Should().Be(tu);
        }

        private void TestLifetimeBoundTo(IWindsorContainer cw) {
            cw.Register(
                Component.For<IA, IB>().ImplementedBy<C>().Named("C")
                    .LifeStyle.BoundToFirstType().InOrder<IDep<IA>,DepAb,IX>(),
                Component.For<IA>().ImplementedBy<A>().Named("A").LifestyleTransient(),
                Component.For<IB>().ImplementedBy<B>().Named("B").LifestyleBoundTo<IDep<IB>>(),
                Component.For<IDep<IA>>().ImplementedBy<DepA>().LifestyleTransient().Named("DA"),
                Component.For<IDep<IB>>().ImplementedBy<DepB>().LifestyleTransient().Named("DB")
                    .DependsOn(ServiceOverride.ForKey<IB>().Eq("B")),
                Component.For<DepAb>().LifestyleTransient(),
                Component.For<IDep<IA>>().ImplementedBy<DepA>().LifestyleBoundTo<IX>().Named("DAIX"),
                Component.For<IX>().ImplementedBy<X>()
                    .DependsOn(ServiceOverride.ForKey<IA>().Eq("A"))
                    .DependsOn(ServiceOverride.ForKey<IDep<IA>>().Eq("DAIX"))
                    .LifestyleTransient()
                );

            var a1 = cw.ResolveAndCount((DepAb r) => { r.A.Should().BeSameAs(r.B); return r.A; }, 2);
            a1.DisposeCount.Should().Be(1);

            cw.ResolveAndCount((DepAb r) => r.A.Should().BeSameAs(r.B).And.NotBe(a1), 2);

            cw.ResolveAndCount((IDep<IA> r) => r.Dependency.Should().BeOfType<C>(), 2);

            cw.ResolveAndCount((IDep<IB> r) => r.Dependency.Should().BeOfType<B>(), 2);

            cw.ResolveAndCount((IX x) =>
                {
                    x.A.Should()
                        .NotBe(x.Ia1.Dependency)
                        .And.NotBe(x.Ia2.Dependency);
                    x.B.Should()
                        .NotBe(x.Ib1.Dependency)
                        .And.NotBe(x.Ib2.Dependency);
                    x.Ia1.Should().BeSameAs(x.Ia2);
                    x.Ia1.Dependency.Should().BeSameAs(x.Ia2.Dependency).And.BeSameAs(x.B);
                    x.Ib1.Should().NotBe(x.Ib2);
                    x.Ib1.Dependency.Should().NotBe(x.Ib2.Dependency);
                },
                5);
        }
    }

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
        public static TResult ResolveAndCount<T, TResult>(this IWindsorContainer cw, Func<T, TResult> act, int expectCount)
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

    public static class WindsorExtensions
    {
        private static IHandler FirstMatchingScopeRootSelector(this IEnumerable<Type> types, IHandler[] resolutionStack)
        {
            return resolutionStack
                .FirstOrDefault(h => types.Any(t => t.GetTypeInfo().IsAssignableFrom((TypeInfo) h.ComponentModel.Implementation)));
        }

        public class BoundToAnyCapture<T> where T : class
        {
            private readonly List<Type> _types = new List<Type>();
            public BoundToAnyCapture(LifestyleGroup<T> lifeStyleGroup)
            {
                LifeStyleGroup = lifeStyleGroup;
            }

            public LifestyleGroup<T> LifeStyleGroup { get; }
            public ComponentRegistration<T> Final()
            {
                return LifeStyleGroup.BoundTo(_types.FirstMatchingScopeRootSelector);
            }

            public BoundToAnyCapture<T> Then(Type t)
            {
                _types.Add(t);
                return this;
            }

            public BoundToAnyCapture<T> Then<TBound>() => Then(typeof(TBound));

            public ComponentRegistration<T> InOrder(params Type[] types)
            {
                foreach (var type in types)
                    Then(type);
                return Final();
            }
            public ComponentRegistration<T> InOrder<TBound1>() => Then<TBound1>().Final();
            public ComponentRegistration<T> InOrder<TBound1, TBound2>() => Then<TBound1>().Then<TBound2>().Final();
            public ComponentRegistration<T> InOrder<TBound1, TBound2, TBound3>() => Then<TBound1>().Then<TBound2>().Then<TBound3>().Final();
        }

        public static BoundToAnyCapture<T>  BoundToFirstType<T>(
            this LifestyleGroup<T> lifestyleGroup)
            where T: class
        {
            Contract.Requires(!ReferenceEquals(lifestyleGroup, null));
            return new BoundToAnyCapture<T>(lifestyleGroup);
        }
    }
}
