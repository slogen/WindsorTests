using System.Diagnostics.CodeAnalysis;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.Lifestyle.Tests
{
    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors")]
    public class LifestyleWindsorTest
    {
        [Test]
        public static void TestLifetimeBoundTo()
        {
            var tu = IdTrack.TotalUndisposedCount;
            using (var cw = new WindsorContainer())
            {
                TestLifetimeBoundTo(cw);
            }
            IdTrack.TotalUndisposedCount.Should().Be(tu);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private static void TestLifetimeBoundTo(IWindsorContainer cw)
        {
            cw.Register(
                Component.For<IA, IB>().ImplementedBy<C>().Named("C")
                    .LifeStyle.BoundToAny().OfType<IDependentOn<IA>, DepAb, IX>(),
                Component.For<IA>().ImplementedBy<A>().Named("A").LifestyleTransient(),
                Component.For<IB>().ImplementedBy<B>().Named("B").LifestyleBoundTo<IDependentOn<IB>>(),
                Component.For<IDependentOn<IA>>().ImplementedBy<DependentOnA>().LifestyleTransient().Named("DA"),
                Component.For<IDependentOn<IB>>().ImplementedBy<DependentOnB>().LifestyleTransient().Named("DB")
                    .DependsOn(ServiceOverride.ForKey<IB>().Eq("B")),
                Component.For<DepAb>().LifestyleTransient(),
                Component.For<IDependentOn<IA>>().ImplementedBy<DependentOnA>().LifestyleBoundTo<IX>().Named("DAIX"),
                Component.For<IX>().ImplementedBy<X>()
                    .DependsOn(ServiceOverride.ForKey<IA>().Eq("A"))
                    .DependsOn(ServiceOverride.ForKey<IDependentOn<IA>>().Eq("DAIX"))
                    .LifestyleTransient()
            );

            var a1 = cw.ResolveAndCount((DepAb r) =>
            {
                r.A.Should().BeSameAs(r.B);
                return r.A;
            }, 2);
            a1.DisposeCount.Should().Be(1);

            cw.ResolveAndCount((DepAb r) => r.A.Should().BeSameAs(r.B).And.NotBe(a1), 2);

            cw.ResolveAndCount((IDependentOn<IA> r) => r.Dependency.Should().BeOfType<C>(), 2);

            cw.ResolveAndCount((IDependentOn<IB> r) => r.Dependency.Should().BeOfType<B>(), 2);

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
}