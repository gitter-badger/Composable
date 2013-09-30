using System;
using System.Configuration;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using Composable.CQRS.ViewModels;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using NServiceBus;
using NUnit.Framework;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable MemberCanBePrivate.Global
namespace CQRS.Tests.JobCommunicationTransactionAbortedBug
{
    [TestFixture]
    public class ThisBugRecreationShouldNotCauseATransactionAbortedException
    {
        private WindsorContainer Container { get; set; }

        [SetUp]
        protected void Initialize()
        {
            Container = new WindsorContainer();
            
            Container.Install(FromAssembly.This());
            Container.Register(Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>());


            Container.Register(
                Component.For<IWindsorContainer>().Instance(Container),
                Component.For<IEventStore>().ImplementedBy<SqlServerEventStore>()
                    .DependsOn(new Dependency[] {Dependency.OnValue(typeof(string), ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString)})
                    .LifestyleSingleton(),
                Component.For<IEventStoreSession>().ImplementedBy<EventStoreSession>().LifeStyle.Scoped(),
                Component.For<IServiceBus>().ImplementedBy<DummyServiceBus>());

            Container.Register(
                Component.For<IHandleMessages<CreateCandidateCommand>>().ImplementedBy<CreateCandidateCommandHandler>().LifestyleScoped(),
                Component.For<IHandleMessages<CandidateCreatedEvent>>().ImplementedBy<CandidateViewModelUpdater>().LifestyleScoped()
                );

            Container.Register(
                Component.For<IDocumentDb>()
                    .ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(new { connectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString })
                    .LifestyleScoped(),
                Component.For<IDocumentDbSessionInterceptor>()
                    .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                    .LifestyleSingleton(),
                Component.For<IDocumentDbSession>().ImplementedBy<DocumentDbSession>().LifestyleScoped()
                );
        }

        public class Candidate : EventStoredAggregateRoot<Candidate>
        {
            public Candidate(Guid candidateId)
            {
                Register(Handler.For<CandidateCreatedEvent>().OnApply(createdEvent => SetIdBeVerySureYouKnowWhatYouAreDoing(createdEvent.AggregateRootId)));
                ApplyEvent(new CandidateCreatedEvent(candidateId));
            }
        }
               
        public class CreateCandidateCommandHandler : IHandleMessages<CreateCandidateCommand>            
        {
            private readonly IEventStoreSession _session;

            public CreateCandidateCommandHandler(IEventStoreSession session)
            {
                _session = session;
            }

            public void Handle(CreateCandidateCommand command)
            {
                var c = new Candidate(command.AggregateRootId);
                _session.Save(c);
                _session.SaveChanges();
            }
        }

        public class CandidateViewModelUpdater : ViewModelUpdater<CandidateViewModelUpdater, CandidateViewModel, CandidateCreatedEvent, IDocumentDbSession>
        {
            public CandidateViewModelUpdater(IDocumentDbSession session) : base(session, creationEvent: typeof(CandidateCreatedEvent))
            {
                RegisterHandlers()
                    .For<CandidateCreatedEvent>(e => { Model = new CandidateViewModel(e.AggregateRootId); });
            }
        }

        public class CandidateViewModel : PersistentEntity<CandidateViewModel>
        {
            public CandidateViewModel(Guid aggregateRootId): base(aggregateRootId) {}
        }

        public class CreateCandidateCommand : AggregateRootEvent
        {
            public CreateCandidateCommand(Guid candidateId) : base(candidateId) {}
        }

        public class CandidateCreatedEvent : AggregateRootEvent
        {
            public CandidateCreatedEvent(Guid jobCommunicationCandidateId) : base(jobCommunicationCandidateId) {}
        }

        [Test]
        public void RunRecreationLogic()
        {
            using(Container.BeginScope())
            {
                Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
            }
        }
    }
}
// ReSharper restore MemberCanBePrivate.Global
// ReSharper restore RedundantArgumentDefaultValue