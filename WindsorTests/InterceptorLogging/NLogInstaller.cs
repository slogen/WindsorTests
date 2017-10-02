using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace WindsorTests.InterceptorLogging
{
    public class NLogInstaller : IWindsorInstaller
    {
        public static readonly NLogInstaller Default = new NLogInstaller();

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            if (ReferenceEquals(container, null))
                throw new ArgumentNullException(nameof(container));
            container.Install(ArgumentFormatterWindsorInstaller.Default);
            container.Register(
                Component.For(typeof(ILogIdentityFactory<long>))
                    .ImplementedBy<LongIdentityFactory>()
                    .LifestyleSingleton().IsFallback().Named("DefaultLogIdentityFactory"),
                Component.For(typeof(INLogInterceptor)).ImplementedBy(typeof(DefaultNLogInterceptor<>))
                    .IsFallback().Named("DefaultNLogInterceptor")
            );
        }
    }
}