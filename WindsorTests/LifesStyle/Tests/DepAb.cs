namespace WindsorTests.LifesStyle.Tests
{
    class DepAb : IdTrack
    {
        public DepAb(IA a, IB b)
        {
            A = a;
            B = b;
        }

        public IA A { get; }
        public IB B { get; }
    }
}