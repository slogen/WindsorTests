using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using WindsorTests.InterceptorLogging.Formatter;
using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Installer
{
    public class ArgumentFormatterWindsorInstaller : IWindsorInstaller
    {
        public static readonly ArgumentFormatterWindsorInstaller Default = new ArgumentFormatterWindsorInstaller();

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            if (ReferenceEquals(container, null))
                throw new ArgumentNullException(nameof(container));
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));

            container.Register(
                Classes.FromAssemblyInThisApplication()
                    .BasedOn(typeof(IArgumentFormatter))
                    .If(x => !x.IsAbstract && !x.IsInterface)
                    .WithServiceSelf()
                    .WithServiceFromInterface(typeof(IArgumentFormatter))
                    .Configure(r => r.IsFallback()));
            container.Register(
                Component.For<IArgumentFormatter>()
                    .UsingFactoryMethod(kernel =>
                        new CombinedFormatter(kernel.ResolveAll<IArgumentFormatter>())));
        }
    }
}