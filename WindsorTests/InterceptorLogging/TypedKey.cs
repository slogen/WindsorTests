namespace WindsorTests.InterceptorLogging
{
    public struct TypedKey<T, TKey>
    {
        public readonly TKey Id;

        public TypedKey(TKey id)
        {
            Id = id;
        }

        public override string ToString() => $"{typeof(T).Name}{Id}";
    }
}