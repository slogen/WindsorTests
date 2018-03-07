namespace WindsorTests.Lifestyle.Tests
{
    class C : IdTrack, IA, IB
    {
        public string ToStringA()
        {
            return $"{this} as IA";
        }

        public string ToStringB()
        {
            return $"{this} as IB";
        }
    }
}