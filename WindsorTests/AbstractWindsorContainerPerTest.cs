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
        protected IWindsorContainer WindsorContainer
        {
            get
            {
                return _windsorContainer ?? (_windsorContainer = CreateWindsorContainer());
            }
        }
        [SetUp]
        public void SetUp() { _windsorContainer = null; }
        [TearDown]
        public void TearDown() { _windsorContainer?.Dispose(); }
    }
}
