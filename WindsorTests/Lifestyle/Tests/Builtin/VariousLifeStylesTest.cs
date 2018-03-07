using Castle.MicroKernel.Registration;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;

namespace WindsorTests.Lifestyle.Tests
{
    class VariousLifeStylesTest: AbstractWindsorContainerPerTest
    {
        class DisposeCount: IDisposable
        {
            private int _count;
            public int Count => _count;

            public void Dispose() => Interlocked.Increment(ref _count);
        }
        [Test]
        public void InstanceShouldNeverBeDisposedByWindsor()
        {

            DisposeCount dc = new DisposeCount();
            using (var c = WindsorContainer)
            {
                c.Register(Component.For<DisposeCount>().Instance(dc));
                dc = c.Resolve<DisposeCount>();
                dc.Count.Should().Be(0);
                c.Release(dc);
                dc.Count.Should().Be(0);
                var dc2 = c.Resolve<DisposeCount>();
                dc2.Should().BeSameAs(dc);
            }
            dc.Count.Should().Be(0);
            dc.Dispose();
            dc.Count.Should().Be(1);
        }
        [Test]
        public void SingletonShouldBeDisposedWhenContainerDisposed()
        {
            DisposeCount dc;
            using (var c = WindsorContainer)
            {
                c.Register(Component.For<DisposeCount>().LifestyleSingleton());
                dc = c.Resolve<DisposeCount>();
                dc.Count.Should().Be(0);
                c.Release(dc);
                dc.Count.Should().Be(0);
                var dc2 = c.Resolve<DisposeCount>();
                dc2.Should().BeSameAs(dc);
            }
            dc.Count.Should().Be(1);
        }
        [Test]
        public void TransientShouldBeDisposedWhenReleased()
        {
            DisposeCount dc;
            using (var c = WindsorContainer)
            {
                c.Register(Component.For<DisposeCount>().LifestyleTransient());
                dc = c.Resolve<DisposeCount>();
                dc.Count.Should().Be(0);
                c.Release(dc);
                dc.Count.Should().Be(1);
            }
            dc.Count.Should().Be(1);
        }
        [Test]
        public void ScopedShouldBeDisposedScopeEnds()
        {
            DisposeCount dc;
            using (var c = WindsorContainer)
            {
                c.Register(Component.For<DisposeCount>().LifestyleScoped());
                using (c.BeginScope())
                {
                    dc = c.Resolve<DisposeCount>();
                    var dc2 = c.Resolve<DisposeCount>();
                    dc2.Should().BeSameAs(dc);
                    dc.Count.Should().Be(0);
                }
                dc.Count.Should().Be(1);
            }
            dc.Count.Should().Be(1);
        }

        protected override IWindsorContainer CreateWindsorContainer() => new WindsorContainer();
    }
}
