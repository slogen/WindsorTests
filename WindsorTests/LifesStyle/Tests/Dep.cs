namespace WindsorTests.LifesStyle.Tests
{
    class Dep<T> : IdTrack, IDep<T>
        where T : IId
    {
        protected Dep(T dependency)
        {
            Dependency = dependency;
        }

        public T Dependency { get; }

        public override string ToString() => $"{base.ToString()}, depend on {Dependency} as {typeof(T).Name}";
    }
}