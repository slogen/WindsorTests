namespace WindsorTests.LifesStyle.Tests
{
    class B : IdTrack, IB
    {
        public string ToStringB() => $"{this} as IB";
    }
}