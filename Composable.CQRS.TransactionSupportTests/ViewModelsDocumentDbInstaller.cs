using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.CQRS.Windsor.Testing;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.UnitsOfWork;
using Manpower.Applications.JobCommunication.ViewModels.Services;

namespace Manpower.Applications.JobCommunication.ViewModels.ContainerInstallers
{
    public class DocumentDbInstaller : IWindsorInstaller
    {
        public const string ConnectionStringName = "JobCommunicationReadModels";
        public const string KeyForDocumentDb = "Manpower.Applications.JobCommunication.ViewModels.IDocumentDb";
        public const string KeyForInMemoryDocumentDb = "Manpower.Applications.JobCommunication.ViewModels.IDocumentDb.InMemory";
        public const string KeyForSession = "Manpower.Applications.JobCommunication.ViewModels.IDocumentDbSession";
        public const string KeyForNullOpSessionInterceptor = "Manpower.Applications.JobCommunication.ViewModels.NullOpSessionInterceptor";

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.Register(
                Component.For<IDocumentDb>()
                    .ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(new { connectionString = GetConnectionStringFromConfiguration(ConnectionStringName) })
                    .Named(KeyForDocumentDb)
                    .LifestylePerWebRequest(),

                Component.For<IDocumentDbSessionInterceptor>()
                    .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                    .Named(KeyForNullOpSessionInterceptor)
                    .LifestyleSingleton(),

                Component.For<IDocumentDbSession, IViewModelsSession>()
                    .ImplementedBy<ViewModelsSession>()
                    .DependsOn(
                        Dependency.OnComponent(typeof(IDocumentDb), KeyForDocumentDb),
                        Dependency.OnComponent(typeof(IDocumentDbSessionInterceptor), KeyForNullOpSessionInterceptor))
                    .Named(KeyForSession)
                    .LifestylePerWebRequest()
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
    }
}