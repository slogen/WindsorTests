using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;

namespace WindsorTests.Lifestyle
{
    public class BoundToAnyCapture<T> where T : class
    {
        private readonly List<Type> _types = new List<Type>();

        public BoundToAnyCapture(LifestyleGroup<T> lifestyleGroup)
        {
            LifestyleGroup = lifestyleGroup;
        }

        public LifestyleGroup<T> LifestyleGroup { get; }

        private static IHandler FirstMatchingScopeRootSelector(IEnumerable<Type> types, IHandler[] resolutionStack)
        {
            var selected = types.SelectMany((t, ti) =>
                    resolutionStack.Select(
                            (h, hi) =>
                                new
                                {
                                    h,
                                    hi,
                                    ti,
                                    canApply =
                                    t.GetTypeInfo().IsAssignableFrom((TypeInfo) h.ComponentModel.Implementation)
                                })
                        .Where(x => x.canApply))
                .OrderBy(x => x.hi)
                .ThenBy(x => x.ti)
                .FirstOrDefault();
            return selected?.h;
        }

        public ComponentRegistration<T> Final()
        {
            return LifestyleGroup.BoundTo(handlers => FirstMatchingScopeRootSelector(_types, handlers));
        }

        public BoundToAnyCapture<T> Or(Type type)
        {
            _types.Add(type);
            return this;
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter",
            Justification = "Shorthand for user")]
        public BoundToAnyCapture<T> Or<TBound>() => Or(typeof(TBound));

        public ComponentRegistration<T> OfType(params Type[] types)
        {
            if (types != null)
                foreach (var type in types)
                    Or(type);
            return Final();
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public ComponentRegistration<T> OfType<TBound1>() => Or<TBound1>().Final();

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public ComponentRegistration<T> OfType<TBound1, TBound2>() => Or<TBound1>().Or<TBound2>().Final();

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public ComponentRegistration<T> OfType<TBound1, TBound2, TBound3>()
            => Or<TBound1>().Or<TBound2>().Or<TBound3>().Final();
    }
}