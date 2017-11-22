using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.HandlerDispatching.ByCommandTag
{


    public class HandlerByCommandTagTests : AbstractWindsorContainerPerTest
    {
        public interface ICommand
        {
            int CommandTag { get; }
        }
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
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

        public interface IHandlerFactory
        {
            IHandler ForCommand(ICommand command);
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

            public void Register(int commandType, Type handlerType)
            {
                _registeredHandlers[commandType] = handlerType;
            }

            protected override Type GetComponentType(MethodInfo method, object[] arguments)
            {
                var command = (ICommand)arguments[0];
                var commandType = command.CommandTag;
                Type t;
                return !_registeredHandlers.TryGetValue(commandType, out t) ? null : t;
            }
        }

        public class HandlerBase : IHandler
        {
            public ICommand Command { get; }
            public HandlerBase(ICommand command)
            {
                Command = command;
            }
            public Task Execute(CancellationToken cancellationToken) => Task.FromResult(Command);
        }

        public class Command : ICommand
        {
            public Command(int commandTag)
            {
                CommandTag = commandTag;
            }

            public int CommandTag { get; }
        }

        public class Installer : IWindsorInstaller
        {
            public HandlerFactorySelector HandlerFactorySelector { get; } = new HandlerFactorySelector();
            private readonly FromDescriptor[] _searchHandlers;

            public Installer(params FromDescriptor[] searchHandlers)
            {
                _searchHandlers = searchHandlers;
            }

            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.Register(
                    Component.For<HandlerFactorySelector>().Instance(HandlerFactorySelector),
                    Component.For<IHandlerFactory>().LifestyleSingleton()
                        .AsFactory(c => c.SelectedWith<HandlerFactorySelector>())
                );
                if (_searchHandlers != null)
                {
                    container.Register(
                        _searchHandlers.Select(fromDescriptor =>
                                fromDescriptor.BasedOn<IHandler>()
                                    .If(t =>
                                    {
                                        var a = t.GetCustomAttribute<CommandHandlerAttribute>();
                                        if (a != null)
                                            HandlerFactorySelector.Register(a.CommandTag, t);
                                        return a != null;
                                    })
                                    .WithServiceAllInterfaces()
                                    .WithServiceSelf()
                                    .Configure(c => c.LifestyleTransient().IsFallback()))
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
            public class Handler0 : HandlerBase
            {
                public Handler0(ICommand command) : base(command)
                {
                }
            }

            [Test]
            public void DispatchCommand0ToCommand0Handler()
            {
                var hf = WindsorContainer.Resolve<IHandlerFactory>();
                var cmd = new Command(0);
                var handler = hf.ForCommand(cmd);
                handler
                    .Should().BeOfType<Handler0>()
                    .Which.Command.Should().BeSameAs(cmd);
            }

            [Test]
            public void Command0HandlerRegisteredToIHandler()
                => WindsorContainer.Resolve<IHandler>(new {command = new Command(0)})
                    .Should().BeOfType<Handler0>();

            [Test]
            public void Command0HandlerRegisteredToHandler0()
                => WindsorContainer.Resolve<Handler0>(new {command = new Command(0)})
                    .Should().BeOfType<Handler0>();
        }

        public class Handler1CommandTests : HandlerByCommandTagTests
        {


            [CommandHandler(1)]
            public class Handler1 : HandlerBase
            {
                public Handler1(ICommand command) : base(command)
                {
                }
            }

            [Test]
            public void DispatchCommand1ToCommand1Handler()
            {
                var hf = WindsorContainer.Resolve<IHandlerFactory>();
                var cmd = new Command(1);
                var handler = hf.ForCommand(cmd);
                handler
                    .Should().BeOfType<Handler1>()
                    .Which.Command.Should().BeSameAs(cmd);
            }
        }


        public class Hander2CommandTests: HandlerByCommandTagTests
        {
            [Test]
            public void DispatchCommand2ToNoHandler()
            {
                var hf = WindsorContainer.Resolve<IHandlerFactory>();
                var cmd = new Command(2);
                Action a = () => hf.ForCommand(cmd);
                a.ShouldThrow<Exception>();
            }
        }
    }
}
