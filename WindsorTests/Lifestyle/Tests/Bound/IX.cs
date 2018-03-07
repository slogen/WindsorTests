namespace WindsorTests.Lifestyle.Tests
{
    interface IX
    {
        IA A { get; }
        IB B { get; }
        IDependentOn<IA> Ia1 { get; }
        IDependentOn<IA> Ia2 { get; }
        IDependentOn<IB> Ib1 { get; }
        IDependentOn<IB> Ib2 { get; }
    }
}