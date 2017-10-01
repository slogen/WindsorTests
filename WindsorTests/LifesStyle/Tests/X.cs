namespace WindsorTests.LifesStyle.Tests
{
    class X : IX
    {
        public X(IA a, IB b, IDep<IA> ia1, IDep<IA> ia2, IDep<IB> ib1, IDep<IB> ib2)
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
        public IDep<IA> Ia1 { get; }
        public IDep<IA> Ia2 { get; }
        public IDep<IB> Ib1 { get; }
        public IDep<IB> Ib2 { get; }
    }
}