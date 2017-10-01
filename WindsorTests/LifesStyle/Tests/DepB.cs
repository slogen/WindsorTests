namespace WindsorTests.LifesStyle.Tests
{
    class DepB : Dep<IB>
    {
        public DepB(IB dependency) : base(dependency)
        {
        }
    }
}