using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NLog;

namespace WindsorTests.InterceptorLogging
{
    public static class WindsorNlogRegistration
    {
        public static NLogConfigurator
            <T, long, LongIdentityFactory, DefaultNLogInterceptor<long>>
            NLog<T>(
                this ComponentRegistration<T> registration,
                IWindsorContainer continer) where T : class
        {
            return new NLogConfigurator<T, long, LongIdentityFactory, DefaultNLogInterceptor<long>>
                (registration, continer);
        }

        public class NLogConfigurator<T, TKey, TKeyFactory, TInterceptor>
            where T : class
            where TKeyFactory : ILogIdentityFactory<TKey>
            where TInterceptor : class, INLogInterceptor<TKey>
        {
            protected readonly IWindsorContainer Container;

            protected readonly ComponentRegistration<T> Registration;
            protected List<Dependency> Dependencies = new List<Dependency>();

            public NLogConfigurator(ComponentRegistration<T> registration, IWindsorContainer container)
            {
                Registration = registration;
                Container = container;
            }

            public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> Logger(ILogger logger)
                => DependsOn(Dependency.OnValue(nameof(logger), logger));

            public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> EntryLogLevel(LogLevel entryLogLevel)
                => DependsOn(Dependency.OnValue(nameof(entryLogLevel), entryLogLevel));

            public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> ReturnLogLevel(LogLevel returnLogLevel)
                => DependsOn(Dependency.OnValue(nameof(returnLogLevel), returnLogLevel));

            public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> ExceptionLogLevel(LogLevel exceptionLogLevel)
                => DependsOn(Dependency.OnValue(nameof(exceptionLogLevel), exceptionLogLevel));

            public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> DependsOn(params Dependency[] dependencies)
            {
                Dependencies.AddRange(dependencies);
                return this;
            }

            public static implicit operator ComponentRegistration<T>(
                NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> c)
                => c.Complete();

            public ComponentRegistration<T> Complete()
            {
                Container.Register(
                    Component.For<TInterceptor>()
                        // Setup ILogger to 
                        .DependsOn(Dependency.OnValue<ILogger>(LogManager.GetLogger(typeof(T).FullName)))
                        .DependsOn(Dependencies.ToArray())
                );
                return Registration.Interceptors<TInterceptor>();
            }
        }
    }
}