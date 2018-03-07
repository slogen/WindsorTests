namespace WindsorTests.Lifestyle.Tests
{
    public interface IDependentOn<out T>
    {
        T Dependency { get; }
    }
}