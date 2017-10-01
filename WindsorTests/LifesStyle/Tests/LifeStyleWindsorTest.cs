using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.LifesStyle.Tests
{
    public class LifeStyleWindsorTest
    {
        [Test]
        public void TestLifetimeBoundTo()
        {
            var tu = IdTrack.TotalUnDisposeCount;
            using (var cw = new WindsorContainer())
            {
                TestLifetimeBoundTo(cw);
            }
            IdTrack.TotalUnDisposeCount.Should().Be(tu);
        }

        private void TestLifetimeBoundTo(IWindsorContainer cw)
        {
            cw.Register(
                Component.For<IA, IB>().ImplementedBy<C>().Named("C")
                    .LifeStyle.BoundToAny().OfType<IDep<IA>, DepAb, IX>(),
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

            var a1 = cw.ResolveAndCount((DepAb r) =>
            {
                r.A.Should().BeSameAs(r.B);
                return r.A;
            }, 2);
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
}