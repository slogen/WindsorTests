using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace WindsorTests.InterceptorLogging
{
    public class NLogInstaller : IWindsorInstaller
    {
        public static NLogInstaller Default = new NLogInstaller();

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For(typeof(ILogIdentityFactory<long>))
                    .ImplementedBy<LongIdentityFactory>()
                    .LifestyleSingleton().IsFallback().Named("DefaultLogIdentityFactory"),
                Component.For(typeof(TypedIdentityFactory<,>))
                    .LifestyleSingleton().IsFallback().Named("DefaultTypesIdentityFactory"),
                Component.For(typeof(INLogInterceptor<>)).ImplementedBy(typeof(DefaultNLogInterceptor<>))
                    .IsFallback().Named("DefaultNLogInterceptor")
            );
        }
    }
}