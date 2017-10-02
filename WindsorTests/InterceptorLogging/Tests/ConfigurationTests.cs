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
            Action a = () => _foo.DoThrow();
            a.ShouldThrow<InvalidOperationException>().And.Message.Should().Be("In DoThrow");
            Target.LogEvents.ShouldBeEquivalentTo(new[]
            {
                new {LogLevel = LogLevel.Fatal},
                new {LogLevel = LogLevel.Info}
            }, cfg => cfg.WithStrictOrdering().ExcludingMissingMembers());
        }

        internal class Foo
        {
            public virtual int DoNothing()
            {
                return 0;
            }

            public virtual void DoThrow()
            {
                throw new InvalidOperationException($"In {nameof(DoThrow)}");
            }
        }
    }
}