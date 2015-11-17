using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.KeyValueStorage;
using Composable.Windsor;

namespace Composable.CQRS.Windsor.Testing
{
    public static class WindsorTestWiringExtensions
    {
        [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
        public static RewiringHelper WhenTesting(this IWindsorContainer @this)
        {
            return new RewiringHelper(@this);
        }

        public class RewiringHelper
        {
            private readonly IWindsorContainer _container;

            public RewiringHelper(IWindsorContainer container)
            {
                _container = container;
            }

            [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
            public RewiringHelper ReplaceDocumentDb(string dbToReplace, string replacementName = null)
            {
                return ReplaceComponent(
                    componentName: dbToReplace,
                    replacement: Component.For<IDocumentDb>()
                        .ImplementedBy<InMemoryDocumentDb>()
                        .LifestyleSingleton(),
                    replacementName: replacementName);
            }

            [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
            public RewiringHelper ReplaceEventStore(string name, string replacementName = null)
            {
                return ReplaceComponent(
                    componentName: name,
                    replacement: Component.For<IEventStore>()
                        .ImplementedBy<InMemoryEventStore>()
                        .LifestyleSingleton(),
                        replacementName: replacementName);
            }

            [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
            public RewiringHelper ReplaceComponent<TServiceType>(string componentName, ComponentRegistration<TServiceType> replacement, string replacementName = null) where TServiceType : class
            {
                _container.Register(
                    Component.For<IConfigureWiringForTests>()
                        .Instance(new LambdaBasedTestRewirer(() => WindsorComponentReplacer.ReplaceComponent(_container, componentName, replacement, replacementName)))
                        .Named(Guid.NewGuid().ToString())
                    );

                return this;
            }

            [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
            public RewiringHelper ReplaceDefault<TServiceType>(ComponentRegistration<TServiceType> replacement) where TServiceType : class
            {
                _container.Register(
                    Component.For<IConfigureWiringForTests>()
                        .Instance(new LambdaBasedTestRewirer(() => WindsorComponentReplacer.ReplaceDefault(_container, replacement)))
                        .Named(Guid.NewGuid().ToString())
                    );

                return this;
            }           

            private class LambdaBasedTestRewirer : IConfigureWiringForTests
            {
                private readonly Action _action;

                public LambdaBasedTestRewirer(Action action)
                {
                    _action = action;
                }

                public void ConfigureWiringForTesting()
                {
                    _action();
                }
            }
        }
    }
}
