using System;
using System.Configuration;
using Castle.Core;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using Composable.CQRS.Windsor;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using Manpower.Applications.CVManagement.Events;
using NServiceBus;
using NUnit.Framework;

namespace Manpower.Applications.JobCommunication.ChannelAdapter.Inbound.CVManagement.Tests
{
    [TestFixture]
    public class DIUsingTest
    {
        protected WindsorContainer Container { get; private set; }

        protected DummyServiceBus Bus { get { return (DummyServiceBus)Container.Resolve<IServiceBus>(); } }

        [TestFixtureSetUp]
        protected void Initialize()
        {
            Container = new WindsorContainer();
            Container.Kernel.ComponentModelBuilder.AddContributor(new LifestyleRegistrationMutator(originalLifestyle: LifestyleType.PerWebRequest,
                newLifestyleType: LifestyleType.Scoped));

            Container.Install(FromAssembly.This());

            Container.Register(Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>());

            Container.Register(
                Component.For<IWindsorContainer>().Instance(Container),
                Component.For<IEventStore>().ImplementedBy<SqlServerEventStore>()
                    .DependsOn(new Dependency[] {Dependency.OnValue(typeof(string), ConfigurationManager.ConnectionStrings["JobCommunicationDomain"].ConnectionString)})
                    .LifestyleSingleton(),
                Component.For<IEventStoreSession>()
                    .ImplementedBy<EventStoreSession>()
                    .LifeStyle.PerWebRequest,
                //temp code to get working right now with synchronous view updating...
                Component.For<IServiceBus>().ImplementedBy<DummyServiceBus>());

            Container.Register(
                AllTypes.FromAssemblyContaining<JobCommunication.ChannelAdapter.Inbound.CVManagement.CVRegisteredEventHandler>()
                    .BasedOn(typeof(IHandleMessages<>)).WithService.AllInterfaces().LifestyleScoped(),
                AllTypes.FromAssemblyContaining<JobCommunication.ViewModelsUpdaters.JobCommunicationCandidateViewModelUpdater>()
                    .BasedOn(typeof(IHandleMessages<>)).WithService.AllInterfaces().LifestyleScoped()
                );
        }

        [Test]
        public void ExecuteScenario()
        {
            using(Container.BeginScope())
            {
                Bus.Publish(
                    new CVRegisteredEvent(
                        cvId: new Guid("8ABFB1BD-9807-44C4-B2FB-7764AFD4ECB1"),
                        userGroupId: new Guid("04544584-F881-40B1-BAED-19E162DB3701"),
                        userId: new Guid("0DB80A9C-DAAC-4347-AF2B-0CC7EC032500"),
                        email: "test@test.org",
                        registrationDate: new DateTime(2011, 10, 10)));
            }
        }
    }
}
