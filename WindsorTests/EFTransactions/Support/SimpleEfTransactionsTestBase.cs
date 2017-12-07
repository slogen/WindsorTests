using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using WindsorTests.Util.Async;
using WindsorTests.Util.Persist.EF;
using WindsorTests.Util.Persist.Interface;

namespace WindsorTests.EFTransactions.Support
{
    public abstract class SimpleEfTransactionsTestBase : EfTransactionsTestBase
    {
        private IUnitOfWorkFactory<IUnitOfWork1> UnitOfWorkFactory
            => WindsorContainer.Resolve<IUnitOfWorkFactory<IUnitOfWork1>>();

        protected CancellationToken CancellationToken => default(CancellationToken);

        protected override IWindsorContainer CreateWindsorContainer()
        {
            return new WindsorContainer()
                .AddFacility<TypedFactoryFacility>()
                .Register(
                    Component.For<Context1>().Named("Context1()").LifestyleTransient().IsFallback(),
                    Component.For<UnitOfWork>().LifestyleTransient().IsFallback()
                        .Forward<IUnitOfWork1>().IsFallback(),
                    Component.For<IUnitOfWorkFactory<IUnitOfWork1>>().LifestyleSingleton()
                        .AsFactory()
                );
        }

        protected async Task InUnitOfWork(Func<IUnitOfWork1, Task> f)
        {
            var uow = UnitOfWorkFactory.CreateUnitOfWork();
            try
            {
                await f(uow).ConfigureAwait(false);
            }
            finally
            {
                UnitOfWorkFactory.Release(uow);
            }
        }

        protected async Task<T> InUnitOfWork<T>(Func<IUnitOfWork1, Task<T>> f)
        {
            var uow = UnitOfWorkFactory.CreateUnitOfWork();
            try
            {
                return await f(uow).ConfigureAwait(false);
            }
            finally
            {
                UnitOfWorkFactory.Release(uow);
            }
        }

        protected virtual Task Update(
            IAsyncBarrier barrier,
            Expression<Func<A, bool>> predicate,
            Action<A> updateAction)
        {
            return InUnitOfWork(async uow =>
            {
                await barrier.SignalAndWait(CancellationToken).ConfigureAwait(false);
                foreach (var a in uow.ARepository.Items.Where(predicate))
                    updateAction(a);
                await barrier.SignalAndWait(CancellationToken).ConfigureAwait(false);
                await uow.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
            });
        }


        protected void CleanDb()
        {
            using (var ctx = new Context1())
            {
                var db = ctx.Database;
                if (db.Exists())
                    db.Delete();
                db.Initialize(false);
            }
        }

        protected Task SetupA1()
        {
            return InUnitOfWork(async uow =>
            {
                uow.ARepository.AddRange(new[] {new A {Key = "a", Value = 1}});
                await uow.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
            });
        }

        protected Task<A> ReadA()
            => InUnitOfWork(uow => uow.ARepository.Items.Where(x => x.Key == "a").SingleAsync(CancellationToken));

        protected async Task<A> ContendedUpdates(ICollection<int> newIds)
        {
            var sequence = new AsyncBarrier(newIds.Count);
            await Task.WhenAll(
                    newIds
                        .Select(async i =>
                            await Update(
                                    sequence,
                                    a => a.Key == "a" && a.Value == 1,
                                    a => a.Value = i)
                                .ConfigureAwait(false)))
                .ConfigureAwait(false);
            return await ReadA().ConfigureAwait(false);
        }

        public interface IUnitOfWork1 : IUnitOfWork
        {
            IRepository<A> ARepository { get; }
        }


        public class UnitOfWork : DbContextUnitOfWork<Context1>, IUnitOfWork1
        {
            public UnitOfWork(Context1 context) : base(context)
            {
            }

            public IRepository<A> ARepository => new DbSetRepository<A>(Context.As);
        }
    }
}