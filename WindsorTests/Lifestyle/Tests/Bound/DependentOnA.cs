namespace WindsorTests.Lifestyle.Tests
{
    class DependentOnA : DependentOn<IA>
    {
        public DependentOnA(IA dependency) : base(dependency)
        {
        }
    }
}