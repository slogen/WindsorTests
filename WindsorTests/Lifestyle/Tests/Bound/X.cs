namespace WindsorTests.Lifestyle.Tests
{
    class X : IX
    {
        public X(IA a, IB b, IDependentOn<IA> ia1, IDependentOn<IA> ia2, IDependentOn<IB> ib1, IDependentOn<IB> ib2)
        {
            A = a;
            B = b;
            Ia1 = ia1;
            Ib1 = ib1;
            Ib2 = ib2;
            Ia2 = ia2;
        }

        public IA A { get; }
        public IB B { get; }
        public IDependentOn<IA> Ia1 { get; }
        public IDependentOn<IA> Ia2 { get; }
        public IDependentOn<IB> Ib1 { get; }
        public IDependentOn<IB> Ib2 { get; }
    }
}