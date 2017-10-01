namespace WindsorTests.LifesStyle.Tests
{
    class DepA : Dep<IA>
    {
        public DepA(IA dependency) : base(dependency)
        {
        }
    }
}