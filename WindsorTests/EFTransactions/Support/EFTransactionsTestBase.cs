using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;

namespace WindsorTests.EFTransactions.Support
{
    public abstract class EfTransactionsTestBase : AbstractWindsorContainerPerTest
    {
        public class A
        {
            [Key]
            public string Key { get; set; }

            public int Value { get; set; }
        }

        public class Context1 : DbContext
        {
            public Context1()
            {
            }

            public Context1(DbTransaction transaction)
            {
                Database.UseTransaction(transaction);
            }

            private DbSet<A> _a;
            public DbSet<A> As => _a ?? (_a = Set<A>());
        }
    }
}