using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.CQRS.Windsor.Testing;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.UnitsOfWork;
using JetBrains.Annotations;
using Manpower.Applications.JobCommunication.ViewModelsUpdaters.Services;

namespace Manpower.Applications.JobCommunication.ViewModelsUpdaters.ContainerInstallers
{
    [UsedImplicitly]
    public class ViewModelsUpdatersDocumentDbInstaller : IWindsorInstaller
    {
        public const string ConnectionStringName = "JobCommunicationReadModels";
        public const string KeyForDocumentDb = "Manpower.Applications.JobCommunication.ViewModelUpdater.IDocumentDb.NoTranslation";
        public const string KeyForSession = "Manpower.Applications.JobCommunication.ViewModelUpdater.IDocumentDbSession.NoTranslation";

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.Register(
                Component.For<IDocumentDb>()
                    .ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(new Dependency[] { Dependency.OnValue(typeof(string), GetConnectionStringFromConfiguration(ConnectionStringName)) })
                    .Named(KeyForDocumentDb)
                    .LifestylePerWebRequest(),
                Component.For<IDocumentDbSession, IViewModelsUpdaterSession, IUnitOfWorkParticipant>()
                    .ImplementedBy<ViewModelsUpdaterSession>()
                    .DependsOn(
                        Dependency.OnComponent(typeof(IDocumentDb), KeyForDocumentDb),
                        Dependency.OnValue(typeof(IDocumentDbSessionInterceptor), NullOpDocumentDbSessionInterceptor.Instance)
                        )
                    .Named(KeyForSession)
                    .LifestylePerWebRequest(),
                Component.For<IConfigureWiringForTests, IResetTestDatabases>()
                    .Instance(new DocumentDbTestConfigurer(container))
                );
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[key];
            if (connectionString == null)
            {
                throw new ConfigurationErrorsException(string.Format("Missing connectionstring for '{0}'", key));
            }
            return connectionString.ConnectionString;
        }

        private class DocumentDbTestConfigurer : IConfigureWiringForTests, IResetTestDatabases
        {
            private readonly IWindsorContainer _container;

            public DocumentDbTestConfigurer(IWindsorContainer container)
            {
                _container = container;
            }

            public void ConfigureWiringForTesting()
            {
                //The ViewModelUpdatersSession and the ViewModelsSession must use the same document db for things to be sane.
                //Sometimes only the wiring for one is used. Sometimes the wiring for both. This if clause takes care of that issue.
                if (!_container.Kernel.HasComponent(ViewModels.ContainerInstallers.DocumentDbInstaller.KeyForInMemoryDocumentDb))
                {
                    _container.Register(
                        Component.For<IDocumentDb, InMemoryDocumentDb>()
                            .ImplementedBy<InMemoryDocumentDb>()
                            .Named(ViewModels.ContainerInstallers.DocumentDbInstaller.KeyForInMemoryDocumentDb)
                            .IsDefault()
                            .LifestyleSingleton());
                }

                _container.Kernel.AddHandlerSelector(
                    new KeyReplacementHandlerSelector(
                        typeof(IDocumentDb),
                        KeyForDocumentDb,
                        ViewModels.ContainerInstallers.DocumentDbInstaller.KeyForInMemoryDocumentDb));
            }

            public void ResetDatabase()
            {
                _container.Resolve<InMemoryDocumentDb>(ViewModels.ContainerInstallers.DocumentDbInstaller.KeyForInMemoryDocumentDb).Clear();
            }
        }
    }
}
