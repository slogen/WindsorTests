namespace WindsorTests.Util.Persist.Interface
{
    public interface IUnitOfWorkFactory<TUnitOfWork>
        where TUnitOfWork : IUnitOfWork
    {
        TUnitOfWork CreateUnitOfWork();
        void Release(TUnitOfWork uow);
    }
}