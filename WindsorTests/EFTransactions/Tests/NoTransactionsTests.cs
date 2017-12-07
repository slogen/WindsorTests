using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using WindsorTests.EFTransactions.Support;

namespace WindsorTests.EFTransactions.Tests
{
    public class NoTransactionsTests : SimpleEfTransactionsTestBase
    {
        [Test]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task ContentionGoesUndetectedWhenNoTransactions()
        {
            CleanDb();
            await SetupA1().ConfigureAwait(false);
            var newIds = Enumerable.Range(2, 5).ToList();
            var a = await ContendedUpdates(newIds).ConfigureAwait(false);
            a.Key.Should().Be("a");
            a.Value.Should()
                .BeGreaterOrEqualTo(newIds.First())
                .And.BeLessOrEqualTo(newIds.Last());
        }
    }
}