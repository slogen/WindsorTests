using Castle.Windsor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindsorTests
{
    public abstract class AbstractWindsorContainerPerTest
    {
        protected abstract IWindsorContainer CreateWindsorContainer();
        private IWindsorContainer _windsorContainer;
        protected IWindsorContainer WindsorContainer => _windsorContainer ?? (_windsorContainer = CreateWindsorContainer());

        protected void ClearContainer()
        {
            _windsorContainer?.Dispose();
            _windsorContainer = null;
        }
        [SetUp]
        public void SetUp() { ClearContainer(); }
        [TearDown]
        public void TearDown() { ClearContainer(); }
    }
}
