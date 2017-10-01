namespace WindsorTests.LifesStyle.Tests
{
    public interface IDep<out T>
    {
        T Dependency { get; }
    }
}