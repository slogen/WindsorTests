using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.HandlerDispatching.ByCommandTag
{


    public class HandlerByCommandTagTests : AbstractWindsorContainerPerTest
    {
        protected static CancellationToken CancellationToken => default(CancellationToken);

        public interface ICommand
        {
            int CommandTag { get; }
        }
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public class CommandHandlerAttribute : Attribute
        {
            public int CommandTag { get; }

            public CommandHandlerAttribute(int commandTag)
            {
                CommandTag = commandTag;
            }
        }

        public interface IHandler
        {
            Task Execute(CancellationToken cancellationToken);
        }

        public interface IHandlerFactory: IDisposable
        {
            IHandler ForCommand(ICommand command);
            void Release(IHandler handler);
        }

        public class HandlerFactorySelector : DefaultTypedFactoryComponentSelector
        {
            /// TODO: Remove this hacky way of registering
            private readonly ConcurrentDictionary<int, Type> _registeredHandlers = new ConcurrentDictionary<int, Type>();
            public HandlerFactorySelector(
                bool getMethodsResolveByName = true,
                bool fallbackToResolveByTypeIfNameNotFound = true)
                : base(getMethodsResolveByName, fallbackToResolveByTypeIfNameNotFound)
            {
            }

            public bool TryRegister(int commandType, Type handlerType)
                => _registeredHandlers.TryAdd(commandType, handlerType);

            public bool TryRegister(Type handlerType)
            {
                var a = handlerType.GetCustomAttribute<CommandHandlerAttribute>(inherit: true);
                if (a != null)
                    TryRegister(a.CommandTag, handlerType);
                return a != null;
            }

            protected override Type GetComponentType(MethodInfo method, object[] arguments)
            {
                var command = (ICommand)arguments[0];
                var commandType = command.CommandTag;
                Type t;
                return !_registeredHandlers.TryGetValue(commandType, out t) ? null : t;
            }
        }

        public class SpecificCommand : ICommand
        {
            public SpecificCommand(int commandTag)
            {
                CommandTag = commandTag;
            }

            public int CommandTag { get; }
        }

        public class Installer : IWindsorInstaller
        {
            // BUG: This releaser does *not* cause IDisposable invocation when using TypedFactory!
            public class ReleaseHandlerImmediatelyAfterExecuteInterceptor : IInterceptor
            {
                private static readonly MethodInfo Execute = typeof(IHandler).GetMethod("Execute");
                private readonly IKernel _kernel;

                private static MethodInfo GetInterfaceMethod(Type implementingClass, Type implementedInterface, MethodInfo classMethod)
                {
                    if (classMethod.DeclaringType == implementedInterface)
                        return classMethod;
                    var map = implementingClass.GetInterfaceMap(implementedInterface);
                    var index = Array.IndexOf(map.TargetMethods, classMethod);
                    return map.InterfaceMethods[index];
                }
                public ReleaseHandlerImmediatelyAfterExecuteInterceptor(IKernel kernel)
                {
                    this._kernel = kernel;
                }
                public void Intercept(IInvocation invocation)
                {
                    var mi = GetInterfaceMethod(invocation.TargetType, typeof(IHandler), invocation.Method);
                    if (mi != Execute)
                    {
                        invocation.Proceed();
                        return;
                    }

                    if (typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
                    {
                        try
                        {
                            invocation.Proceed();
                            var returnValue = (Task) invocation.ReturnValue;
                            invocation.ReturnValue = returnValue.ContinueWith(t =>
                            {
                                _kernel.ReleaseComponent(invocation.Proxy);
                            });
                        }
                        catch
                        {
                            _kernel.ReleaseComponent(invocation.Proxy);
                        }
                        return;
                    }

                    try
                    {
                        invocation.Proceed();
                    }
                    finally
                    {
                        _kernel.ReleaseComponent(invocation.Proxy);
                    }
                }
            }
            public HandlerFactorySelector HandlerFactorySelector { get; } = new HandlerFactorySelector();
            private readonly FromDescriptor[] _searchHandlers;

            public Installer(params FromDescriptor[] searchHandlers)
            {
                _searchHandlers = searchHandlers;
            }

            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.Register(
                    Component.For<ReleaseHandlerImmediatelyAfterExecuteInterceptor>().LifestyleTransient(),
                    Component.For<HandlerFactorySelector>().Instance(HandlerFactorySelector),
                    Component.For<IHandlerFactory>().LifestyleSingleton()
                        .AsFactory(c => c.SelectedWith<HandlerFactorySelector>())
                );
                if (_searchHandlers != null)
                {
                    container.Register(
                        _searchHandlers.Select(fromDescriptor =>
                                fromDescriptor.BasedOn<IHandler>()
                                    .If(HandlerFactorySelector.TryRegister)
                                    .WithServiceFromInterface(typeof(IHandler))
                                    .WithServiceSelf()
                                    .Configure(c => 
                                        c.LifeStyle.Is(LifestyleType.Transient)
                                            .Interceptors<ReleaseHandlerImmediatelyAfterExecuteInterceptor>()
                                            ))
                                    .Cast<IRegistration>()
                            .ToArray());
                }
            }
        }

        protected override IWindsorContainer CreateWindsorContainer()
        {
            return new WindsorContainer()
                .AddFacility<TypedFactoryFacility>()
                .Install(new Installer(Types.FromAssemblyContaining(GetType())));
        }

        public class Handler0CommandTests : HandlerByCommandTagTests
        {
            [CommandHandler(0)]
            public class Handler0 : IHandler, IDisposable
            {
                public ICommand Command { get; }
                public Handler0(ICommand command)
                {
                    Command = command;
                }
                Task IHandler.Execute(CancellationToken cancellationToken) => Task.FromResult(Command);
                public bool Disposed { get; private set; }
                public void Dispose() { Disposed = true; }
            }

            [Test]
            public async Task DispatchCommand0ToCommand0Handler()
            {
                var hf = WindsorContainer.Resolve<IHandlerFactory>();
                var cmd = new SpecificCommand(0);
                Handler0 handler0;
                var handler = hf.ForCommand(cmd);
                try
                {
                    handler
                        .Should().BeAssignableTo<Handler0>()
                        .Which.Command.Should().BeSameAs(cmd);
                    handler0 = (Handler0) handler;
                    handler0.Disposed.Should().BeFalse();
                    await handler.Execute(CancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    hf.Release(handler);
                }
                handler0.Disposed.Should().BeTrue();
            }

            [Test]
            public void Command0HandlerRegisteredToIHandler()
                => WindsorContainer.Resolve<IHandler>(new {command = new SpecificCommand(0)})
                    .Should().BeAssignableTo<Handler0>();

            [Test]
            public void Command0HandlerRegisteredToHandler0()
                => WindsorContainer.Resolve<Handler0>(new {command = new SpecificCommand(0)})
                    .Should().BeAssignableTo<Handler0>();
        }

        public class Handler1CommandTests : HandlerByCommandTagTests
        {

            [CommandHandler(1)]
            public class Handler1 : IHandler, IDisposable
            {
                public ICommand Command { get; }

                public Handler1(ICommand command)
                {
                    Command = command;
                }

                public virtual Task Execute(CancellationToken cancellationToken) => Task.FromResult(Command);
                public bool Disposed { get; private set; }
                public void Dispose() { Disposed = true; }
            }

            [Test]
            public async Task DispatchCommand1ToCommand1HandlerAndVerifyExecutionTerminatesLifetime()
            {
                var hf = WindsorContainer.Resolve<IHandlerFactory>();
                var cmd = new SpecificCommand(1);
                Handler1 handler1;
                var handler = hf.ForCommand(cmd);
                try
                {
                    handler
                        .Should().BeAssignableTo<Handler1>()
                        .Which.Command.Should().BeSameAs(cmd);
                    handler1 = (Handler1) handler;
                    handler1.Disposed.Should().BeFalse();
                    await handler1.Execute(CancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    hf.Release(handler);
                }
                handler1.Disposed.Should().BeTrue();
            }
        }


        public class Hander2CommandTests: HandlerByCommandTagTests
        {
            [Test]
            public void DispatchCommand2ToNoHandler()
            {
                var hf = WindsorContainer.Resolve<IHandlerFactory>();
                var cmd = new SpecificCommand(2);
                Action a = () => hf.ForCommand(cmd);
                a.ShouldThrow<Exception>();
            }
        }
    }
}
