using System.Diagnostics.CodeAnalysis;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using WindsorTests.InterceptorLogging.Detail;

namespace WindsorTests.InterceptorLogging
{
    public static class WindsorNLogRegistration
    {
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
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