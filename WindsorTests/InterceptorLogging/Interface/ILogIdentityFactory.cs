namespace WindsorTests.InterceptorLogging.Interface
{
    public interface ILogIdentityFactory<out TKey>
    {
        TKey NewId();
    }
}