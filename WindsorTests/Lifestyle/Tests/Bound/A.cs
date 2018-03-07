namespace WindsorTests.Lifestyle.Tests
{
    class A : IdTrack, IA
    {
        public string ToStringA() => $"{this} as IA";
    }
}