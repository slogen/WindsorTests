namespace WindsorTests.Lifestyle.Tests
{
    public interface IId
    {
        long Id { get; }
        long DisposeCount { get; }
    }
}