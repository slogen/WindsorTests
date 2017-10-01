namespace WindsorTests.Lifestyle.Tests
{
    class DependentOn<T> : IdTrack, IDependentOn<T>
        where T : IId
    {
        protected DependentOn(T dependency)
        {
            Dependency = dependency;
        }

        public T Dependency { get; }

        public override string ToString() => $"{base.ToString()}, depend on {Dependency} as {typeof(T).Name}";
    }
}