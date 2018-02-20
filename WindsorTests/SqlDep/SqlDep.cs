#if _NOT_DONE
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindsorTests.SqlDep
{
    interface IChange { }

    public static class Dependency
    {
        public IObservable<T> Events(this SqlCommand cmd, SqlDependency dependency)
        {
            dependency.AddCommandDependency(cmd);
            dependency.OnChange += Dependency_OnChange;
        }

        private void Dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            e.Info.
            throw new NotImplementedException();
        }
    }

    class QuerySource<TState>
    {
        protected SqlDependency SqlDependency;
        protected string ConnectionString;
        public QuerySource(ICollection<TState> state)
        {
            State = state;
        }
    }
}
#endif