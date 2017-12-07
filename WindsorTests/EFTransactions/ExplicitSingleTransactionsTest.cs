using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;
using WindsorTests.EFTransactions.Design.Interface;
using WindsorTests.EFTransactions.Tests.Support;

namespace WindsorTests.EFTransactions.Tests
{
    public class ExplicitSingleTransactionsTest : SimpleEfTransactionsTestBase
    {
        protected override IWindsorContainer CreateWindsorContainer()
        {
            return base.CreateWindsorContainer()
                .Register(
                    Component.For<TransactionalUnitOfWork>()
                        .Forward<UnitOfWork, IUnitOfWork1>()
                        .LifestyleTransient()
                );
        }

        [Test]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task ContentionDetectedWithExplicitTransaction([Values(2)] int cnt)
        {
            CleanDb();
            await SetupA1().ConfigureAwait(false);
            var newIds = Enumerable.Range(2, cnt).ToList();

            Func<Task> act = async () => await ContendedUpdates(newIds).ConfigureAwait(false);
            act.ShouldThrow<EntityException>()
                .WithInnerException<UpdateException>();
        }

        protected class TransactionalUnitOfWork : UnitOfWork
        {
            public TransactionalUnitOfWork(Context1 context)
                : base(context)
            {
                context.Database.BeginTransaction(IsolationLevel.Serializable);
            }

            protected override async Task<int> SaveChangesOnceAsync(CancellationToken cancellationToken)
            {
                var changes = await base.SaveChangesOnceAsync(cancellationToken).ConfigureAwait(false);
                Context.Database.CurrentTransaction.Commit();
                return changes;
            }
        }
    }
}