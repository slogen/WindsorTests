namespace WindsorTests.Lifestyle.Tests
{
    class B : IdTrack, IB
    {
        public string ToStringB() => $"{this} as IB";
    }
}