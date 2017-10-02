using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using WindsorTests.InterceptorLogging.Detail;
using WindsorTests.InterceptorLogging.Installer;
using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Tests
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Test manages disposables in SetUp/TearDown")]
    public class LoggingTests
    {
        protected IWindsorContainer Container { get; set; }
        protected ILogger Logger { get; set; }
        protected TestTarget Target { get; set; }

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

        protected class TestTarget : Target
        {
            public IList<LogEventInfo> LogEvents { get; } = new List<LogEventInfo>();

            protected override void Write(LogEventInfo logEvent)
            {
                LogEvents.Add(logEvent);
            }
        }
    }
}