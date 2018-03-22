using System;
using System.Collections;
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
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WindsorTests.HandlerDispatching.ByTTag
{
    #region Extensions
    public static class F
    {
        public static Func<TResult> Func<TResult>(Func<TResult> f) => f;
        public static Func<TArg, TResult> Func<TArg, TResult>(Func<TArg, TResult> f) => f;
    }
    public static class UsingExtensions
    {
        public static TResult UsedWith<TResult, TResource>(
            this Func<TResource> resourceFactory,
            Action<TResource> releaseAction,
            Func<TResource, TResult> resultFunction
        )
        {
            var resource = resourceFactory();
            try
            {
                return resultFunction(resource);
            }
            finally
            {
                releaseAction?.Invoke(resource);
            }
        }
        public static void UsedWith<TResult, TResource>(
            this Func<TResource> resourceFactory,
            Action<TResource> releaseAction,
            Action<TResource> resourceAction
        )
        {
            var resource = resourceFactory();
            try
            {
                resourceAction(resource);
            }
            finally
            {
                releaseAction?.Invoke(resource);
            }
        }
    }
    #endregion

    #region Windsor
    public static class WindsorUsingExtensions
    {
        public static TResult Using<TResult, TResource>(
            this IWindsorContainer container,
            Func<TResource, TResult> f)
            where TResource: class
        {
            return F.Func(container.Resolve<TResource>)
                .UsedWith(container.Release,
                f);
        }
    }

    public interface ITagHandlerFactory: IDisposable
    {
        object HandlerFor<TArg>(Type tagType, object tag, Type handlerType, TArg arg);
        void Release(object handler);
    }
    public static class TagHandlerFactoryExtensions
    {
        public struct HandlerRef<THandler>
        {
            public ITagHandlerFactory TagHandlerFactory { get; }
            public HandlerRef(ITagHandlerFactory tagHandlerFactory)
            {
                TagHandlerFactory = tagHandlerFactory;
            }

            public THandler For<TTag, TArg>(TTag tag, TArg arg)
                => (THandler)TagHandlerFactory.HandlerFor(typeof(TTag), tag, typeof(THandler), arg);

            public TResult UsedFor<TTag, TArg, TResult>(TTag tag, TArg arg, Func<THandler, TResult> f)
            {
                var handler = For(tag, arg);
                try
                {
                    return f(handler);
                }
                finally
                {
                    TagHandlerFactory.Release(handler);
                }
            }
        }
        public static HandlerRef<THandler> Handler<THandler>(this ITagHandlerFactory tagHandlerFactory)
            => new HandlerRef<THandler>(tagHandlerFactory);

        public static TResult UsingHandler<TTag, TArg, THandler, TResult>(
            this ITagHandlerFactory tagHandlerFactory,
            TTag tag, TArg arg, Func<THandler, TResult> f)
            => tagHandlerFactory.Handler<THandler>().UsedFor(tag, arg, f);

        public static TResult UsingHandler<TTag, TArg, THandler, TResult>(
            this IWindsorContainer container,
            TTag tag,
            TArg arg, Func<THandler, TResult> f)
            => container.Using((ITagHandlerFactory thf) => 
                    thf.UsingHandler(tag, arg, f));
        public static TResult UsingHandler<TTag, TArg, THandler, TResult>(
            this IWindsorContainer container,
            TArg arg, Func<TArg, TTag> extractTag, Func<THandler, TResult> f)
            => container.UsingHandler(extractTag(arg), arg, f);
        public static TResult UsingHandler<TArg, THandler, TResult>(
            this IWindsorContainer container,
            TArg arg, Func<THandler, TResult> f)
            where TArg: ICommand<Guid>
            => container.UsingHandler(arg, x => x.Tag, f);

    }
    public class TagHandlerFactorySelector :
        DefaultTypedFactoryComponentSelector,
        IDisposable
    {
        readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, Type>> _registeredHandlers;

        public TagHandlerFactorySelector(
            ConcurrentDictionary<Type, ConcurrentDictionary<object, Type>> registeredHandlers,
            bool getMethodsResolveByName = true,
            bool fallbackToResolveByTypeIfNameNotFound = true
            )
            : base(getMethodsResolveByName, fallbackToResolveByTypeIfNameNotFound)
        {
            _registeredHandlers = registeredHandlers;
        }

        protected virtual ConcurrentDictionary<object, Type> MakeTagDictionary(
            Type tagType, object tag, Type handlerType)
            => new ConcurrentDictionary<object, Type>();

        public bool TryRegister(Type tagType, object tag, Type handlerType, IEqualityComparer comparer = null)
        {
            return _registeredHandlers.GetOrAdd(tagType, tt => MakeTagDictionary(tagType, tag, handlerType))
                .TryAdd(tag, handlerType);
        }
        public void Register(Type tagType, object tag, Type handlerType, IEqualityComparer comparer = null)
        {
            _registeredHandlers.GetOrAdd(tagType, tt => MakeTagDictionary(tagType, tag, handlerType))
                .AddOrUpdate(tag, handlerType, (t, h) => handlerType);
        }

        public bool TryUnregister(Type tagType, object tag)
        {
            ConcurrentDictionary<object, Type> tagDict;
            if (_registeredHandlers.TryGetValue(tagType, out tagDict))
            {
                Type oldType;
                return tagDict.TryRemove(tag, out oldType);
            }
            return false;
        }
        protected override IDictionary GetArguments(MethodInfo method, object[] arguments)
        {
            return base.GetArguments(method, arguments);
        }
        protected override Func<IKernelInternal, IReleasePolicy, object> BuildFactoryComponent(MethodInfo method, string componentName, Type componentType, IDictionary additionalArguments)
        {
            // TODO: Don't rely on name here -- this code is ugly and makes lots of assumtions that should not be there!
            // TODO: Dont copy this code anywhere!
            var argName = "arg";
            object arg = additionalArguments[argName];
            var parameterType = componentType.GetConstructors().Single().GetParameters()[0].ParameterType;
            if (!ReferenceEquals(null, arg)
                && parameterType != typeof(JObject)
                && arg is JObject)
            {
                var jarg = (JObject)arg;
                additionalArguments["arg"] = jarg.ToObject(parameterType);
            }
            return base.BuildFactoryComponent(method, componentName, componentType, additionalArguments);
        }
        protected override string GetComponentName(MethodInfo method, object[] arguments)
        {
            return base.GetComponentName(method, arguments);
        }
        protected override Type GetComponentType(MethodInfo method, object[] arguments)
        {
            var tagType = (Type)arguments[0];
            var tag = arguments[1];
            if (tag == null)
                return null;
            if (!tagType.IsInstanceOfType(tag))
                return null;
            ConcurrentDictionary<object, Type> tagDict;
            if (!_registeredHandlers.TryGetValue(tagType, out tagDict))
                return null;
            Type handlerType;
            if (!tagDict.TryGetValue(tag, out handlerType))
                return null;
            return handlerType;
        }

        public void Dispose()
        {
            // Noop
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TagHandlerAttribute : Attribute
    {
        public Type HandlerType { get; }
        public string Member { get; }

        public TagHandlerAttribute(Type handlerType, string member = null)
        {
            HandlerType = handlerType;
            Member = member ?? "Tag";
        }
    }
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<ConcurrentDictionary<Type, ConcurrentDictionary<object, Type>>>()
                    .Named("registeredHandlers").IsFallback().LifestyleSingleton(),
                Component.For<TagHandlerFactorySelector>()
                    .DependsOn(Dependency.OnComponent("registeredHandlers", "registeredHandlers"))
                    .LifestyleTransient(),
                Component.For<ITagHandlerFactory>().LifestyleTransient()
                    .AsFactory(c => c.SelectedWith<TagHandlerFactorySelector>())
            );
        }
    }

    public class TagHandlerInfo
    {
        public Type TagType { get; }
        public object Tag { get; }
        public Type HandlerType { get; }

        public TagHandlerInfo(Type tagType, object tag, Type handlerType)
        {
            TagType = tagType;
            Tag = tag;
            HandlerType = handlerType;
        }

        public static TagHandlerInfo ByTagHandlerAttribute(Type t)
        {
            var tha = t.GetCustomAttribute<TagHandlerAttribute>();
            if (tha == null)
                return null;
            var pInfo = t.GetProperty(tha.Member,
                BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (pInfo == null)
                return null;
            var tag = pInfo.GetValue(null);
            return new TagHandlerInfo(tag.GetType(), tag, tha.HandlerType);
        }
    }

    public static class WindsorTaggedExtensions
    {
        public static IWindsorContainer RegisterByTagHandlerAttribute(
            this IWindsorContainer container,
            params BasedOnDescriptor[] fromDescriptors)
            => container.RegisterByTagHandlerAttribute((IEnumerable<BasedOnDescriptor>)fromDescriptors);
        public static IWindsorContainer RegisterByTagHandlerAttribute(
            this IWindsorContainer container,
            IEnumerable<BasedOnDescriptor> fromDescriptors)
        {
            return container.Register(
                F.Func(container.Resolve<TagHandlerFactorySelector>)
                    .UsedWith(container.Release,
                        thfs =>
                            fromDescriptors
                                .Select(fd =>
                                    fd.If(t =>
                                    {
                                        var thi = TagHandlerInfo.ByTagHandlerAttribute(t);
                                        if (thi == null)
                                            return false;
                                        thfs.Register(thi.TagType, thi.Tag, t);
                                        return true;
                                        })
                                        .LifestyleTransient()
                                        .WithServiceSelf())
                                .Cast<IRegistration>()
                                .ToArray()));
        }
    }

    #endregion

    public interface ICommand<TTag>
    {
        TTag Tag { get; }
    }


    public class HandlerByTTagTests : AbstractWindsorContainerPerTest
    {
        protected static CancellationToken CancellationToken => default(CancellationToken);

        protected override IWindsorContainer CreateWindsorContainer()
        {
            return new WindsorContainer()
                .AddFacility<TypedFactoryFacility>()
                .Install(new Installer());
        }
    }


    public class HandlerShouldDispatchBasedOnCommand : HandlerByTTagTests
    {
        protected override IWindsorContainer CreateWindsorContainer()
        {
            return base.CreateWindsorContainer()
                .RegisterByTagHandlerAttribute(Types.FromThisAssembly().BasedOn<ISomeHandler>());
        }
        public interface ISomeHandler
        {
            object Arg { get; }
            int Execute();
        }
        [TagHandler(typeof(ISomeHandler), "SomeTag")]
        public abstract class SomeHandler : ISomeHandler
        {
            public abstract int Execute();
            public abstract object Arg { get; }
        }

        public class AlarmResetCommand : ICommand<Guid>
        {
            public static Guid SorType = Guid.Parse("aab89c3c-effa-4ef8-a3ca-7f72ac781035");
            public Guid Tag => SorType;
            public string S { get; }
            public int I { get; }

            public AlarmResetCommand(string s, int i)
            {
                S = s;
                I = i;
            }
        }

        public class AlarmResetHandler : SomeHandler
        {
            public AlarmResetCommand Command { get; }
            public override object Arg => Command;
            public static Guid SomeTag { get; } = AlarmResetCommand.SorType;
            public AlarmResetHandler(AlarmResetCommand arg)
            {
                Command = arg;
            }
            public override int Execute() => 1;
        }


        [Test]
        public void DispatchAlarmCommandShouldBeResolvedDynamicallyToCorrectHandler()
        {
            // Record actual arguments here
            object gotArg = null;
            ISomeHandler gotHandler = null;
            // Setup request for specific action
            var arg = new AlarmResetCommand("a", 2);
            var got = WindsorContainer.UsingHandler(
                    arg,
                    (ISomeHandler h) =>
                        {
                            gotArg = h.Arg;
                            gotHandler = h;
                            return h.Execute();
                        });
            // The right handler should be selected
            gotHandler.Should().BeOfType<AlarmResetHandler>();
            // Argument properly transferred
            gotArg.Should().BeSameAs(arg);
            // Result of execution should be as expected by AlarmResetHandler
            got.Should().Be(1);
        }
    }

    public class HandlerUpdatesDynamically: HandlerByTTagTests
    {
        interface I
        {
            string DoStuff();
        }
        public class A {
            public static readonly Guid Tag = Guid.Parse("81066cb4-1979-4598-ae9e-7b895a0da9f9");
        }
        [TagHandler(typeof(I), "Tag")]
        public class A1: I {
            public static Guid Tag => A.Tag;
            public string DoStuff() => "A1";
        }
        [TagHandler(typeof(I), "Tag")]
        public class A2 : I {
            public static Guid Tag => A.Tag;
            public string DoStuff() => "A2";
        }
        protected override IWindsorContainer CreateWindsorContainer()
        {
            return base.CreateWindsorContainer()
                .RegisterByTagHandlerAttribute(Types.FromThisAssembly().BasedOn<I>().If(t => t == typeof(A1)));
        }
        [Test]
        public void DynamicUpdateOfHandlerShouldBeSupportedAndLatestRegisteredShouldWin() {
            string arg = null;
            WindsorContainer
                .UsingHandler(A.Tag, arg, (I h) => { h.Should().BeOfType<A1>(); return h.DoStuff(); })
                .Should().Be("A1");

            WindsorContainer.RegisterByTagHandlerAttribute(Types.FromThisAssembly().BasedOn<I>().If(t => t == typeof(A2)));

            WindsorContainer
                .UsingHandler(A.Tag, arg, (I h) => { h.Should().BeOfType<A2>(); return h.DoStuff(); })
                .Should().Be("A2");
        }
    }

    public class HandlerDeserializeOnCreation : HandlerByTTagTests
    {
        interface I
        {
            string DoStuff();
        }
        public class ACommand
        {
            public string Stuff { get; set; }
        }
        [TagHandler(typeof(I), "Tag")]
        public class A : I
        {
            public static Guid Tag { get; } = Guid.Parse("81066cb4-1979-4598-ae9e-7b895a0da9f9");
            ACommand Arg;
            public A(ACommand arg) { Arg = arg; }
            public string DoStuff() => Arg.Stuff;
        }
        protected override IWindsorContainer CreateWindsorContainer()
        {
            return base.CreateWindsorContainer()
                .RegisterByTagHandlerAttribute(Types.FromThisAssembly().BasedOn<I>());
        }
        [Test]
        public void DynamicUpdateOfHandlerShouldBeSupportedAndLatestRegisteredShouldWin()
        {
            var acmd = new ACommand { Stuff = "CommandStuff" };
            var acmdJson = JsonConvert.SerializeObject(acmd);
            var acmdJsonObject = JsonConvert.DeserializeObject(acmdJson);
            WindsorContainer
                .UsingHandler(A.Tag, acmdJsonObject, (I h) => { h.Should().BeOfType<A>(); return h.DoStuff(); })
                .Should().Be(acmd.Stuff);
        }
    }

}