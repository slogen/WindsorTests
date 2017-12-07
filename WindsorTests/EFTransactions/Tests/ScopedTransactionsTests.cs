using System;
using System.Data.Entity.Core;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using NUnit.Framework;
using WindsorTests.EFTransactions.Support;
using WindsorTests.Util.Async;

namespace WindsorTests.EFTransactions.Tests
{
    public class ScopedTransactionsTests : SimpleEfTransactionsTestBase
    {
        protected override async Task Update(IAsyncBarrier barrier, Expression<Func<A, bool>> predicate,
            Action<A> updateAction)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await base.Update(barrier, predicate, updateAction);
                scope.Complete();
            }
        }

        [Test]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        [TestCase(2)]
        [TestCase(100, Explicit = true, Reason = "Time consuming")]
        public async Task ContentionDetectedWhenScopedTransaction(int cnt)
        {
            CleanDb();
            await SetupA1().ConfigureAwait(false);
            var newIds = Enumerable.Range(2, cnt).ToList();

            Func<Task> act = async () => await ContendedUpdates(newIds).ConfigureAwait(false);
            act.ShouldThrow<EntityException>()
                .WithInnerException<UpdateException>();
        }
    }
}