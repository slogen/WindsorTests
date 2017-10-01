using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace WindsorTests.InterceptorLogging.Tests
{
    public class LoggingTests
    {
        public IWindsorContainer Container;
        protected ILogger Logger;

        protected TestTarget Target;

        [SetUp]
        public virtual void SetUp()
        {
            Container = new WindsorContainer();
            Target = new TestTarget();
            var logCfg = new LoggingConfiguration();
            logCfg.AddTarget("testtarget", Target);
            logCfg.AddRule(LogLevel.Trace, LogLevel.Fatal, Target);
            Logger = new LogFactory(logCfg).GetLogger("testLogger");
            Container.Install(NLogInstaller.Default);
            Container.Register(
                Component.For<ILogIdentityFactory<long>>()
                    // Start id's from 0 for reproducable tests
                    .ImplementedBy<LongIdentityFactory>()
                    .DependsOn(Dependency.OnValue<long?>(0L)));
        }

        [TearDown]
        public virtual void TearDown()
        {
            Container?.Dispose();
        }

        public class TestTarget : Target
        {
            public ICollection<LogEventInfo> LogEvents = new List<LogEventInfo>();

            protected override void Write(LogEventInfo logEvent)
            {
                LogEvents.Add(logEvent);
            }
        }
    }
}