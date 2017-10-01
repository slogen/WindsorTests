namespace WindsorTests.LifesStyle.Tests
{
    public interface IId
    {
        long Id { get; }
        long DisposeCount { get; }
    }
}