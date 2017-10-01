using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NLog;

namespace WindsorTests.InterceptorLogging
{
    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes")]
    public class NLogConfigurator<T, TKey, TKeyFactory, TInterceptor>
        where T : class
        where TKeyFactory : ILogIdentityFactory<TKey>
        where TInterceptor : class, INLogInterceptor
    {
        public NLogConfigurator(ComponentRegistration<T> registration, IWindsorContainer container)
        {
            Registration = registration;
            Container = container;
        }

        protected IWindsorContainer Container { get; }

        protected ComponentRegistration<T> Registration { get; }
        protected ICollection<Dependency> Dependencies { get; } = new List<Dependency>();

        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#",
            Justification = "Name similarity is useful for caller")]
        public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> Logger(ILogger logger)
            => DependsOn(Dependency.OnValue(nameof(logger), logger));

        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#",
            Justification = "Name similarity is useful for caller")]
        public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> EntryLogLevel(LogLevel entryLogLevel)
            => DependsOn(Dependency.OnValue(nameof(entryLogLevel), entryLogLevel));

        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#",
            Justification = "Name similarity is useful for caller")]
        public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> ReturnLogLevel(LogLevel returnLogLevel)
            => DependsOn(Dependency.OnValue(nameof(returnLogLevel), returnLogLevel));

        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#",
            Justification = "Name similarity is useful for caller")]
        public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> ExceptionLogLevel(LogLevel exceptionLogLevel)
            => DependsOn(Dependency.OnValue(nameof(exceptionLogLevel), exceptionLogLevel));

        public NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> DependsOn(params Dependency[] dependencies)
        {
            if (dependencies != null)
                foreach (var dependency in dependencies)
                    Dependencies.Add(dependency);
            return this;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static implicit operator ComponentRegistration<T>(
            NLogConfigurator<T, TKey, TKeyFactory, TInterceptor> configurator)
        {
            return configurator.Complete();
        }

        public ComponentRegistration<T> ToComponentRegistration() => Complete();

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