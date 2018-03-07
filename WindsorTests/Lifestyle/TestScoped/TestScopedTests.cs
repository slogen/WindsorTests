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
using Castle.MicroKernel.Lifestyle.Scoped;
using Castle.MicroKernel.Context;
using System.Collections.Concurrent;
using Castle.Core;
using Castle.MicroKernel;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace WindsorTests.Lifestyle.Tests
{
    [TestScoped]
    class TestScopedTests : AbstractWindsorContainerPerTest
    {
        class DisposeCount : IDisposable
        {
            private int _count;
            public int Count => _count;

            public void Dispose() => Interlocked.Increment(ref _count);
        }
        [Test]
        public void TestScopedShouldFollowEachTestInvocation()
        {
            DisposeCountTestInstance = WindsorContainer.Resolve<DisposeCount>();
            WindsorContainer.Resolve<DisposeCount>().Should().BeSameAs(DisposeCountTestInstance);
        }
        [Test]
        public void TestScopedShouldFollowEachTestInvocationEvenWithoutTag()
        {
            DisposeCountTestInstance = WindsorContainer.Resolve<DisposeCount>();
            WindsorContainer.Resolve<DisposeCount>().Should().BeSameAs(DisposeCountTestInstance);
        }
        DisposeCount DisposeCountTestInstance;

        [TearDown]
        override public void TearDown()
        {
            DisposeCountTestInstance.Count.Should().Be(1);
            base.TearDown();
        }

        protected override IWindsorContainer CreateWindsorContainer() =>
            new WindsorContainer().Register(Component.For<DisposeCount>().LifestyleTestScoped());
    }
    public class ConcurrentSafeDefaultLifetimeScope: ILifetimeScope
    {
        private ConcurrentDictionary<object, Burden> _scopeBurdens;
        private Action<Burden> _onAfterCreated;
        private static void _noAction(Burden b) { }
        public ConcurrentSafeDefaultLifetimeScope(
            ConcurrentDictionary<object, Burden> scopeBurdens = null, 
            Action<Burden> onAfterCreated = null)
        {
            _scopeBurdens = scopeBurdens ?? new ConcurrentDictionary<object, Burden>();
            _onAfterCreated = onAfterCreated;
        }

        public Burden GetCachedInstance(ComponentModel model, ScopedInstanceActivationCallback createInstance)
            => _scopeBurdens.GetOrAdd(model, m => createInstance(_onAfterCreated ?? _noAction));

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var burdens = Interlocked.Exchange(ref _scopeBurdens, null);
                if (burdens != null)
                    foreach (var b in burdens.Values)
                        b.Release();
            }
        }
        ~ConcurrentSafeDefaultLifetimeScope() { Dispose(false); }
    }
    public class TestLifeTimeScope : ConcurrentSafeDefaultLifetimeScope
    {
        public string Id { get; }
        public TestLifeTimeScope(
            string id, ConcurrentDictionary<object, Burden> scopeBurdens = null, Action<Burden> onAfterCreated = null) 
            : base(scopeBurdens, onAfterCreated)
        {
            Id = id;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // Unregistration is not handled through Dispose. Make accidental caller aware of that
            //throw new InvalidOperationException("Dispose through TestScopeAccessor.Release");
        }
        public void Release() => base.Dispose(true);
    }
    public class TestScopeAccessor : IScopeAccessor
    {
        private static ConcurrentDictionary<string, TestLifeTimeScope> _testScopes
            = new ConcurrentDictionary<string, TestLifeTimeScope>();
        public void Dispose()
        {
            // Assured single-call from Windsor at container destruction, so no locking required
            foreach (var scope in _testScopes.Values)
                scope.Release();
            _testScopes.Clear();
        }
        public ILifetimeScope GetScope(CreationContext context)
            => 
            _testScopes.GetOrAdd(TestContext.CurrentContext.Test.ID, id => new TestLifeTimeScope(id));
        public static bool Release(string id)
        {
            TestLifeTimeScope scope;
            var found = _testScopes.TryRemove(id, out scope);
            if ( found )
                scope.Release();
            return found;
        }
        public static bool Release() => Release(TestContext.CurrentContext.Test.ID); 
    }
    public static class TestScopedExtensions
    {
        public static ComponentRegistration<T> LifestyleTestScoped<T>(this ComponentRegistration<T> registration)
            where T: class
            => registration.LifestyleScoped<TestScopeAccessor>(); 
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public class TestScopedAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test
            | ActionTargets.Suite;
        public void AfterTest(ITest test)
        {
            TestScopeAccessor.Release(test.Id);
        }

        public void BeforeTest(ITest test) { }
    }
    [TestScoped]
    public interface ITestScoping { }
}
