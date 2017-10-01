namespace WindsorTests.InterceptorLogging
{
    public class TypedIdentityFactory<T, TKey> : ILogIdentityFactory<TypedKey<T, TKey>>
    {
        public TypedIdentityFactory(ILogIdentityFactory<TKey> parentIdentityFactory)
        {
            ParentIdentityFactory = parentIdentityFactory;
        }

        public ILogIdentityFactory<TKey> ParentIdentityFactory { get; }
        public TypedKey<T, TKey> Next() => new TypedKey<T, TKey>(ParentIdentityFactory.Next());
    }
}