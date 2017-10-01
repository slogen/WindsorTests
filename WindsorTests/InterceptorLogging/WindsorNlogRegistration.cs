using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace WindsorTests.InterceptorLogging
{
    public static class WindsorNLogRegistration
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static NLogConfigurator
            <T, long, LongIdentityFactory, DefaultNLogInterceptor<long>>
            NLog<T>(
                this ComponentRegistration<T> registration,
                IWindsorContainer container) where T : class
        {
            return new NLogConfigurator<T, long, LongIdentityFactory, DefaultNLogInterceptor<long>>
                (registration, container);
        }
    }
}