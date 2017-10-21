using System;
using System.Threading;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;
// ReSharper disable RedundantArgumentDefaultValue

namespace WindsorTests.FactoryOwnership
{
    public class FactoryOwnershipTests : AbstractWindsorContainerPerTest
    {
        public static class Name
        {
            public const string ManagedExternally = nameof(ManagedExternally);
            public const string ManagedInternally = nameof(ManagedInternally);
            public const string ByFactory = nameof(ByFactory);
            public const string Factory = nameof(Factory);
        }

        public interface IFoo: IDisposable
        {
            int Id { get; }
            int DisposeCount { get; }
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
                var foo = obj as Foo;
                return (foo != null) && foo.Id == Id;
            }
            public override int GetHashCode() => Id;
            private int _disposeCount;

            public void Dispose()
            {
                Interlocked.Increment(ref _disposeCount);
            }

            public int DisposeCount => _disposeCount;
        }

        public interface IFooFactory: IDisposable
        {
            IFoo GetByFactory();
            void Release(IFoo foo);
        }

        protected override IWindsorContainer CreateWindsorContainer()
        {
            var c = new WindsorContainer();
            var id = 0;
            c.AddFacility<TypedFactoryFacility>();
            c.Register(
                Component.For<Foo>().LifestyleTransient()
                    .UsingFactoryMethod(kernel => new Foo(Interlocked.Increment(ref id)), managedExternally: true)
                    .Named(Name.ManagedExternally).Forward<IFoo>(),
                Component.For<Foo>().LifestyleTransient()
                    .UsingFactoryMethod(kernel => new Foo(Interlocked.Increment(ref id)), managedExternally: false)
                    .Named(Name.ManagedInternally).Forward<IFoo>(),
                Component.For<Foo>().LifestyleTransient()
                    .DynamicParameters((kernel, parameters) => parameters["id"] = Interlocked.Increment(ref id))
                    .Named(Name.ByFactory).Forward<IFoo>(),
                Component.For<IFooFactory>()
                    .AsFactory().LifestyleTransient()
            );
            return c;
        }

        private static bool WasLastReference<T>(ref T obj) where T : class
        {
            var weak = new WeakReference<T>(obj);
            // ReSharper disable once RedundantAssignment -- Need to remove reference to detect if it was last
            obj = null;
            GC.Collect();
            return !weak.TryGetTarget(out obj);
        }

        [Test]
        public void ContainerShouldNotKeepExternallyManageObjectsAlive()
        {
            var foo = WindsorContainer.Resolve<IFoo>(Name.ManagedExternally);
            WasLastReference(ref foo).Should().BeTrue();
        }
        [Test]
        public void DisposingContainerShouldNotDisposeExternallyManagedObjects()
        {
            var foo = WindsorContainer.Resolve<IFoo>(Name.ManagedExternally);
            ClearContainer();
            foo.DisposeCount.Should().Be(0);
            WasLastReference(ref foo).Should().BeTrue();
        }
        [Test]
        public void ContainerShouldKeepInternallyManageObjectsAlive()
        {
            var foo = WindsorContainer.Resolve<IFoo>(Name.ManagedInternally);
            WasLastReference(ref foo).Should().BeFalse();
        }
        [Test]
        public void DisposingContainerShouldDisposeInternallyManagedObjects()
        {
            var foo = WindsorContainer.Resolve<IFoo>(Name.ManagedInternally);
            WindsorContainer.Dispose();
            foo.DisposeCount.Should().Be(1);
            WasLastReference(ref foo).Should().BeTrue();
        }
        [Test]
        public void TypedFactoryShouldTrackProducts()
        {
            var fooFactory = WindsorContainer.Resolve<IFooFactory>();
            var foo = fooFactory.GetByFactory();
            WasLastReference(ref foo).Should().BeFalse();
        }
        [Test]
        public void TypedFactoryShouldDisposeAndUntrackProductsWhenTheyAreExplicitlyReleased()
        {
            var fooFactory = WindsorContainer.Resolve<IFooFactory>();
            var foo = fooFactory.GetByFactory();
            fooFactory.Release(foo);
            foo.DisposeCount.Should().Be(1);
            WasLastReference(ref foo).Should().BeTrue();
        }
        [Test]
        public void TypedFactoryShouldDisposeAndUntrackProductsWhenTheFactoryIsDisposed()
        {
            var fooFactory = WindsorContainer.Resolve<IFooFactory>();
            var foo = fooFactory.GetByFactory();
            fooFactory.Dispose();
            foo.DisposeCount.Should().Be(1);
            WasLastReference(ref foo).Should().BeTrue();
        }
        [Test]
        public void TypedFactoryShouldDisposeAndUntrackProductsWhenTheFactoryIsReleased()
        {
            var fooFactory = WindsorContainer.Resolve<IFooFactory>();
            var foo = fooFactory.GetByFactory();
            WindsorContainer.Release(fooFactory);
            foo.DisposeCount.Should().Be(1);
            WasLastReference(ref foo).Should().BeTrue();
        }
    }
}
