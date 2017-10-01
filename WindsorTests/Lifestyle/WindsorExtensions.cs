using System.Diagnostics.Contracts;
using Castle.MicroKernel.Registration.Lifestyle;

namespace WindsorTests.Lifestyle
{
    public static class WindsorExtensions
    {
        public static BoundToAnyCapture<T> BoundToAny<T>(
            this LifestyleGroup<T> lifestyleGroup)
            where T : class
        {
            Contract.Requires(!ReferenceEquals(lifestyleGroup, null));
            return new BoundToAnyCapture<T>(lifestyleGroup);
        }
    }
}