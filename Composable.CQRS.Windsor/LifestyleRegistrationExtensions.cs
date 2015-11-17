using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;

namespace Composable.CQRS.Windsor
{
    [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
    public static class WindsorLifestyleRegistrationExtensions
    {
        /// <summary>
        /// Currently just an alias for Scoped since that is how we implement per message lifestyle in nservicebus.
        /// </summary>
        [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
        public static ComponentRegistration<TComponent> PerNserviceBusMessage<TComponent>(this LifestyleGroup<TComponent> lifestyleGroup) where TComponent : class
        {
            return lifestyleGroup.Scoped();
        }
    }
}