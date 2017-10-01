using System;
using Castle.MicroKernel.Registration;
using FluentAssertions;
using NLog;
using NUnit.Framework;

namespace WindsorTests.InterceptorLogging.Tests
{
    public class ConfigurationTests : LoggingTests
    {
        private Foo _foo;

        public override void SetUp()
        {
            base.SetUp();
            Container.Register(
                Component.For<Foo>().LifestyleTransient()
                    .NLog(Container)
                    .Logger(Logger)
                    .EntryLogLevel(LogLevel.Fatal)
                    .ReturnLogLevel(LogLevel.Warn)
                    .ExceptionLogLevel(LogLevel.Info)
                    .Complete());
            _foo = Container.Resolve<Foo>();
        }

        [Test]
        public void LogLevelsShouldApplyWhenCallingNormally()
        {
            _foo.DoNothing();
            Target.LogEvents.ShouldBeEquivalentTo(new[]
            {
                new {LogLevel = LogLevel.Fatal},
                new {LogLevel = LogLevel.Warn}
            }, cfg => cfg.WithStrictOrdering().ExcludingMissingMembers());
        }

        [Test]
        public void LogLevelsShouldApplyWhenCallingThrowing()
        {
            Action a = () => _foo.Throw();
            a.ShouldThrow<Exception>();
            Target.LogEvents.ShouldBeEquivalentTo(new[]
            {
                new {LogLevel = LogLevel.Fatal},
                new {LogLevel = LogLevel.Info}
            }, cfg => cfg.WithStrictOrdering().ExcludingMissingMembers());
        }

        public class Foo
        {
            public virtual int DoNothing()
            {
                return 0;
            }

            public virtual void Throw()
            {
                throw new Exception();
            }
        }
    }
}