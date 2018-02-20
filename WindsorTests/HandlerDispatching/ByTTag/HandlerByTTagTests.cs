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
                Component.For<ITagHandlerFactory>().LifestyleSingleton()
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
                                        return thi != null &&
                                               thfs.TryRegister(thi.TagType, thi.Tag, t);
                                    })
                                        .LifestyleTransient()
                                        .WithServiceSelf())
                                .Cast<IRegistration>()
                                .ToArray()));
        }
    }

    #endregion

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

    public class AlarmResetCommand
    {
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
        public AlarmResetCommand FooArg { get; }
        public override object Arg => FooArg;
        public static Guid SomeTag { get; } = Guid.Parse("aab89c3c-effa-4ef8-a3ca-7f72ac781035");
        public AlarmResetHandler(AlarmResetCommand arg)
        {
            FooArg = arg;
        }
        public override int Execute() => 1;
    }


    public class HandlerByTTagTests : AbstractWindsorContainerPerTest
    {
        protected static CancellationToken CancellationToken => default(CancellationToken);

        protected override IWindsorContainer CreateWindsorContainer()
        {
            return new WindsorContainer()
                .AddFacility<TypedFactoryFacility>()
                .Install(new Installer())
                .RegisterByTagHandlerAttribute(Types.FromThisAssembly().BasedOn<ISomeHandler>());
        }


    }


    public class HandlerDispatchTest : HandlerByTTagTests
    {
        [Test]
        public void DispatchAlarmCommandShouldBeResolvedDynamicallyToCorrectHandler()
        {
            // Record actual arguments here
            object gotArg = null;
            ISomeHandler gotHandler = null;
            // Setup request for specific action
            var arg = new AlarmResetCommand("a", 2);
            var got = F.Func(WindsorContainer.Resolve<ITagHandlerFactory>)
                .UsedWith(
                    WindsorContainer.Release,
                    hf =>
                    {
                        var tag = AlarmResetHandler.SomeTag;
                        return hf.UsingHandler(tag, arg, (ISomeHandler h) =>
                        {
                            gotArg = h.Arg;
                            gotHandler = h;
                            return h.Execute();
                        });
                    });
            // Result of execution
            got.Should().Be(1);
            // Argument properly transferred
            gotArg.Should().BeSameAs(arg);
            gotHandler.Should().BeOfType<AlarmResetHandler>();
        }
    }
}