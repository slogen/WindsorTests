using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Castle.MicroKernel.Registration;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.InterceptorLogging.Tests
{
    public class ArgumentFormatterTests : LoggingTests
    {
        private Foo _foo;

        public override void SetUp()
        {
            base.SetUp();
            Container.Register(
                Component.For<Foo>().LifestyleTransient()
                    .NLog(Container)
                    .Logger(Logger)
                    .Complete());
            _foo = Container.Resolve<Foo>();
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void DefaultDictionaryArgumentFormatterShouldApplyOnCancellationToken(
            bool can, bool canceled)
        {
            CancellationToken token;
            if (can)
            {
                var cts = new CancellationTokenSource();
                if (canceled)
                    cts.Cancel();
                token = cts.Token;
            }
            else
            {
                token = CancellationToken.None;
                canceled.Should().BeFalse();
            }
            var expect = $"CancellationToken(can={can}, is={canceled})";
            _foo.IsCancellationRequested(token);
            Target.LogEvents.Should().Contain(x => x.FormattedMessage.Contains(expect));
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(11)]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration",
            Justification = "Tets relies on difference between Enumerable and Collection")]
        public void SequenceFormatterShouldApply(int elements)
        {
            var limit = 10;
            var items = Enumerable.Range(0, elements);
            var expectParts = items.Take(limit).Select(x => x.ToString());
            var expect = $"#{elements}({string.Join(", ", expectParts)}{(elements > limit ? "..." : "")})";
            _foo.ToList(items);
            var first = Target.LogEvents[0];
            first.FormattedMessage.Should()
                .NotContain(expect, because: "Non-collection arguments should be be unfolded");
            var second = Target.LogEvents[1];
            second.FormattedMessage.Should().Contain(expect, because: "Any ICollection argument should be unfolded");
        }


        internal class Foo
        {
            public virtual bool IsCancellationRequested(CancellationToken cancellationToken)
            {
                return cancellationToken.IsCancellationRequested;
            }

            public virtual IList<int> ToList(IEnumerable<int> numbers)
            {
                return numbers.ToArray();
            }
        }
    }
}