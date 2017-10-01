namespace WindsorTests.LifesStyle.Tests
{
    interface IX
    {
        IA A { get; }
        IB B { get; }
        IDep<IA> Ia1 { get; }
        IDep<IA> Ia2 { get; }
        IDep<IB> Ib1 { get; }
        IDep<IB> Ib2 { get; }
    }
}