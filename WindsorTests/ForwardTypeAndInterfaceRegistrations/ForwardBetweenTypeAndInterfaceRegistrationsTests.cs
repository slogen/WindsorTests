using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.ForwardTypeAndInterfaceRegistrations
{
    public class ForwardBetweenTypeAndInterfaceRegistrationsTests : AbstractWindsorContainerPerTest
    {
        protected override IWindsorContainer CreateWindsorContainer()
        {
            var c = new WindsorContainer();
            c.Register(
                Component.For<Foo>().LifestyleTransient()
                    .DependsOn(Dependency.OnValue("id", -1))
                    .Named("Foo").Forward<IFoo>(),
                Component.For<Bar>().LifestyleTransient()
                    .Named("Bar").Forward<IFoo>()
            );
            return c;
        }

        [Test]
        public void UnnamedResolveReturnsFirstRegistration()
        {
            WindsorContainer.Resolve<IFoo>()
                .Should()
                .BeOfType<Foo>()
                .And.Be(new Foo(-1), because: "We should get the Fpo registration");
        }

        [Test]
        public void NamedFooResolveReturnsFoo()
        {
            WindsorContainer.Resolve<IFoo>("Foo")
                .Should()
                .BeOfType<Foo>()
                .And.Be(new Foo(-1), because: "We should get the Foo registration");
        }

        [Test]
        public void NamedBarResolveReturnsBar()
        {
            WindsorContainer.Resolve<IFoo>("Bar")
                .Should()
                .BeOfType<Bar>()
                .And.Be(new Bar(new Foo(-1)),
                    because:
                    "We should get Bar registration, and then resolve the inner registration through the default");
        }

        public interface IFoo
        {
            int Id { get; }
        }

        public class Foo : IFoo
        {
            public Foo(int id)
            {
                Id = id;
            }

            public int Id { get; }

            public override bool Equals(object obj)
            {
                return obj is Foo &&
                       ((Foo) obj).Id == Id;
            }

            public override int GetHashCode() => Id;
        }

        public class Bar : IFoo
        {
            public Bar(IFoo other)
            {
                Other = other;
            }

            public IFoo Other { get; }

            public int Id => Other.Id;

            public override bool Equals(object obj)
            {
                return obj is Bar &&
                       Other.Equals(((Bar) obj).Other);
            }

            public override int GetHashCode() => Id;
        }
    }
}