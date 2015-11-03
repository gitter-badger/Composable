using System;
using System.Configuration;
using System.Transactions;
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
using Composable.KeyValueStorage.Population;
using Composable.KeyValueStorage.SqlServer;
using Composable.ServiceBus;
using Composable.System;
using Composable.System.Transactions;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using NCrunch.Framework;
using NServiceBus;
using NUnit.Framework;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable MemberCanBePrivate.Global
namespace CQRS.Tests.JobCommunicationTransactionAbortedBug
{
    [TestFixture]
    [Isolated]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.DocumentDbMdf, NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public class ThisBugRecreationShouldNotCauseATransactionAbortedException
    {
        private WindsorContainer Container { get; set; }

        [SetUp]
        protected void Initialize()
        {
            Container = new WindsorContainer();
            var eventStoreConnectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
            var documentDbConnectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;

            using(var tran = new TransactionScope())
            {
                SqlServerDocumentDb.ResetDB(documentDbConnectionString);
                SqlServerEventStore.ResetDB(eventStoreConnectionString);
                tran.Complete();
            }

            //log4net.Config.XmlConfigurator.Configure();
            
            Container.Install(FromAssembly.This());
            Container.Register(Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>());


            Container.Register(
                Component.For<IWindsorContainer>().Instance(Container),
                Component.For<IEventStore>().ImplementedBy<SqlServerEventStore>()
                    .DependsOn(new Dependency[] {Dependency.OnValue(typeof(string), eventStoreConnectionString)})
                    .LifestyleSingleton(),
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>().ImplementedBy<EventStoreSession>().LifeStyle.Scoped(),
                Component.For<IServiceBus>().ImplementedBy<DummyServiceBus>());

            Container.Register(
                Component.For<IHandleMessages<CreateCandidateCommand>>().ImplementedBy<CreateCandidateCommandHandler>().LifestyleScoped(),
                Component.For<IHandleMessages<CandidateCreatedEvent>>().ImplementedBy<CandidateViewModelUpdater>().LifestyleScoped()
                );
            
            Container.Register(
                Component.For<IDocumentDb>()
                    .ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(new { connectionString = documentDbConnectionString })
                    .LifestyleScoped(),
                Component.For<IDocumentDbSessionInterceptor>()
                    .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                    .LifestyleSingleton(),
                Component.For<IDocumentDbSession, IUnitOfWorkParticipant>().ImplementedBy<DocumentDbSession>().LifestyleScoped()
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
        public void CausesSomethingVariation()
        {
            using (Container.BeginScope())
            {
                Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
            }
        }

        [Test]
        public void CausesTransactionAbortedException()
        {
            using (Container.BeginScope())
            {
                using(var transaction = new TransactionScope())
                {
                    Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
                    transaction.Complete();
                }
            }
        }

        public class ForceDistributionParticipant : IEnlistmentNotification 
        {
            public Guid Id { get; set; }

            public ForceDistributionParticipant()
            {
                Id = Guid.NewGuid();
            }
            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Done();
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }

        [Test]
        public void TestIfItsAllAboutForcingDistributionEarly()
        {
            var participant = new ForceDistributionParticipant();
            using(var transaction = new TransactionScope())
            {
                Transaction.Current.EnlistDurable(participant.Id, participant, EnlistmentOptions.None);
                //Does not work: Transaction.Current.EnlistVolatile(participant, EnlistmentOptions.EnlistDuringPrepareRequired);
                //Transaction.Current.EnlistVolatile(participant, EnlistmentOptions.None);
                using(Container.BeginScope())
                {
                    Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));

                }
                transaction.Complete();
            }
        }

        [Test]
        public void TestIfItsAllAboutForcingDistributionEarly2()
        {
            Console.WriteLine(Transaction.Current);
            using (var transaction = new TransactionScope())
            {
                Console.WriteLine(Transaction.Current.TransactionInformation.DistributedIdentifier);
                using (Container.BeginScope())
                {
                    Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
                }
                transaction.Complete();
            }
        }

         [Test]
        public void CausesTransactionAbortedExceptionVariation345()
        {
            using(var transaction = new TransactionScope())
            {
                using(Container.BeginScope())
                {
                    Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));

                }
                transaction.Complete();
            }
        }

        [Test]
        public void CausesOperationNotValidException()
        {
            using(Container.BeginScope())
            {
                using(var scope = Container.BeginTransactionalUnitOfWorkScope())
                {
                    Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
                    scope.Commit();
                }
            }
        }

        [Test]
        public void CausesOperationNotValidExceptionoe()
        {
                using (Container.BeginScope())
                {
                    using (var scope = Container.BeginTransactionalUnitOfWorkScope())
                    {
                        using(var transaction = new TransactionScope())
                        {
                            Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
                            scope.Commit();
                            transaction.Complete();
                        }
                    }
                }
        }

        [Test]
        public void CausesOperationNotValidExceptionoe34()
        {
            using(var transaction = new TransactionScope())
            {
                using(Container.BeginScope())
                {
                    using(var scope = Container.BeginTransactionalUnitOfWorkScope())
                    {
                        Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
                        scope.Commit();
                    }
                }
                transaction.Complete();
            }
        }

        [Test]
        public void CausesOperationNotValidExceptionoee()
        {
            using (var transaction = new TransactionScope())
            {
                using (Container.BeginScope())
                {
                    using(var t2 = new TransactionScope())
                    {
                        using(var scope = Container.BeginTransactionalUnitOfWorkScope())
                        {
                            Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
                            scope.Commit();
                        }
                        t2.Complete();
                    }
                }
                transaction.Complete();
            }
        }

        [Test]
        public void CausesOperationNotValidExceptionoee4()
        {
            using (var transaction = new TransactionScope())
            {
                using (Container.BeginScope())
                {
                    using (var t2 = new TransactionScope())
                    {
                        using(var scope = Container.BeginTransactionalUnitOfWorkScope())
                        {
                            using(var t3 = new TransactionScope())
                            {
                                Container.Resolve<IServiceBus>().Send(new CreateCandidateCommand(candidateId: Guid.Parse("00000000-0000-0000-0000-000000000001")));
                                scope.Commit();
                                t3.Complete();
                            }
                        }
                        t2.Complete();
                    }
                }
                transaction.Complete();
            }
        }
    }    
}
// ReSharper restore MemberCanBePrivate.Global
// ReSharper restore RedundantArgumentDefaultValue