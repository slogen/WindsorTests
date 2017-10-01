namespace WindsorTests.Lifestyle.Tests
{
    class DependentOnB : DependentOn<IB>
    {
        public DependentOnB(IB dependency) : base(dependency)
        {
        }
    }
}