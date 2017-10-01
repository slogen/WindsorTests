namespace WindsorTests.InterceptorLogging
{
    public interface ILogIdentityFactory<out TKey>
    {
        TKey Next();
    }
}