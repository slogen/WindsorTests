using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.HandlerDispatching.ByCommandType
{
    public interface IHandler
    {
        Task Execute(CancellationToken cancellationToken);
    }
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "Handler selection is done on exact command type")]
    public interface IHandler<TCommand>: IHandler
    {
        TCommand Command { get; }
    }

    public interface IHandlerFactory
    {
        IHandler ForCommand(object command);
    }

    public class HandlerFactorySelector : DefaultTypedFactoryComponentSelector
    {
        public HandlerFactorySelector(
            bool getMethodsResolveByName = true, 
            bool fallbackToResolveByTypeIfNameNotFound = true) 
            : base(getMethodsResolveByName, fallbackToResolveByTypeIfNameNotFound)
        {
        }

        protected override Type GetComponentType(MethodInfo method, object[] arguments)
        {
            var command = arguments[0];
            var componentType = typeof(IHandler<>).MakeGenericType(command.GetType());
            return componentType;
        }
    }
    public class HandlerBase<TCommand> : IHandler<TCommand>
    {
        public TCommand Command { get; }
        public HandlerBase(TCommand command)
        {
            Command = command;
        }
        public Task Execute(CancellationToken cancellationToken) => Task.FromResult(Command);
    }


    public class HandlerByCommandTypeTests : AbstractWindsorContainerPerTest
    {
        protected override IWindsorContainer CreateWindsorContainer() => new WindsorContainer()
            .AddFacility<TypedFactoryFacility>()
            .Register(
                Component.For<HandlerFactorySelector>().LifestyleSingleton(),
                Component.For<IHandlerFactory>().LifestyleSingleton().AsFactory(c => c.SelectedWith<HandlerFactorySelector>()),
                Types.FromAssemblyContaining(GetType())
                    .BasedOn(typeof(IHandler<>))
                    .WithService.FromInterface(typeof(IHandler<>))
                    .Configure(c => c.LifestyleTransient().IsFallback())
            );

        public class Command0
        {
        }
        public class Handler0 : HandlerBase<Command0>
        {
            public Handler0(Command0 command) : base(command)
            {
            }
        }
        [Test]
        public void DispatchCommand0ToCommand0Handler()
        {
            var hf = WindsorContainer.Resolve<IHandlerFactory>();
            var cmd = new Command0();
            var handler = hf.ForCommand(cmd);
            handler
                .Should().BeOfType<Handler0>()
                .Which.Command.Should().BeSameAs(cmd);
        }


        public class Command1
        {
        }

        public class Handler1 : HandlerBase<Command1>
        {
            public Handler1(Command1 command) : base(command)
            {
            }
        }
        [Test]
        public void DispatchCommand1ToCommand1Handler()
        {
            var hf = WindsorContainer.Resolve<IHandlerFactory>();
            var cmd = new Command1();
            var handler = hf.ForCommand(cmd);
            handler
                .Should().BeOfType<Handler1>()
                .Which.Command.Should().BeSameAs(cmd);
        }

        public class Command2
        {
        }
        [Test]
        public void DispatchCommand2ToNoHandler()
        {
            var hf = WindsorContainer.Resolve<IHandlerFactory>();
            var cmd = new Command2();
            Action a = () => hf.ForCommand(cmd);
            a.ShouldThrow<ComponentRegistrationException>();
        }
    }
}
