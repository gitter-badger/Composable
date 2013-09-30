using System;
using System.Configuration;
using System.Transactions;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using Composable.CQRS.Windsor;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using NCrunch.Framework;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.UnitOfWorkTests
{
    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf, NCrunchExlusivelyUsesResources.DocumentDbMdf)]
    public class EnlistmentNotificationImplementationTests
    {
        private WindsorContainer _container;
        private IDisposable _containerScope;

        [SetUp]
        public void SetupTask()
        {
            var documentDbConnectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
            var eventStoreConnectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
            SqlServerDocumentDb.ResetDB(documentDbConnectionString);
            SqlServerEventStore.ResetDB(eventStoreConnectionString);

            _container = new WindsorContainer();            
            _container.Register(
                Component.For<IWindsorContainer>().Instance(_container),
                Component.For<IDocumentDb>().ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(Dependency.OnValue(typeof(string), documentDbConnectionString))
                    .LifeStyle.Singleton,
                Component.For<IDocumentDbSession>().ImplementedBy<DocumentDbSession>()
                    .DependsOn(Dependency.OnValue(typeof(IDocumentDbSessionInterceptor), NullOpDocumentDbSessionInterceptor.Instance))
                    .LifeStyle.Scoped(),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<IServiceBus>().ImplementedBy<DummyServiceBus>(),
                Component.For<IEventStore>().ImplementedBy<SqlServerEventStore>()
                    .DependsOn(Dependency.OnValue(typeof(string), eventStoreConnectionString))
                    .LifestyleSingleton(),
                Component.For<IEventStoreSession>().ImplementedBy<EventStoreSession>().LifestyleScoped(),

                Component.For<IHandleMessages<MyMessage>>().ImplementedBy<MyMessageHandler>().LifestyleScoped()
                );

            _container.AssertConfigurationValid();

            _containerScope = _container.BeginScope();
        }

        [TearDown]
        public void TearDownTask()
        {
            _containerScope.Dispose();
        }

        [Test]
        public void Simple()
        {
            var session = _container.Resolve<IDocumentDbSession>();            
            using(var transaction = new TransactionScope())
            {
                session.Save(new MyEntity(Guid.Parse("00000000-0000-0000-0000-000000000001")));   
                transaction.Complete();
            }

            _container.Resolve<IServiceBus>().Send(new MyMessage(Guid.Parse("00000000-0000-0000-0000-000000000002")));
            
        }

        public class MyEntity : IHasPersistentIdentity<Guid>
        {
            public MyEntity(Guid id)
            {
                Id = id;
            }
            public Guid Id { get; private set; }
        }

        public class MyMessage : IMessage
        {
            public MyMessage(Guid id)
            {
                Id = id;
            }

            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            private IDocumentDbSession _session;
            public MyMessageHandler(IDocumentDbSession session)
            {
                _session = session;
            }

            public void Handle(MyMessage message)
            {
                _session.Save(new MyEntity(message.Id));
            }
        }
    }    
}