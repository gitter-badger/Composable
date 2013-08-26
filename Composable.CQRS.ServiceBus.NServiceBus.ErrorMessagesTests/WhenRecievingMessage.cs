#region usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.CQRS.Testing;
using Composable.ServiceBus;
using NCrunch.Framework;
using NServiceBus;
using NUnit.Framework;
using Composable.SystemExtensions;
using FluentAssertions;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.ErrorMessagesTests
{
    [TestFixture, NUnit.Framework.Category("NSBFullSetupTests")]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.NServiceBus)]
    [NCrunch.Framework.Isolated]
    public class WhenReceivingMessage
    {
        [Test]
        public void ExceptionPassedToStackTraceFormatterContainsOriginalException()
        {
            var endpointConfigurer = new MyEndPointConfigurer("Composable.CQRS.ServiceBus.NServicebus.Tests.ErrorMessages");


            endpointConfigurer.Init();
            var bus = endpointConfigurer.Container.Resolve<IServiceBus>();
            var nsbBus = endpointConfigurer.Container.Resolve<IBus>();

            var messageHandled = new ManualResetEvent(false);
            TransactionStatus status = TransactionStatus.Active;
            TestingSupportMessageModule.OnHandleBeginMessage += transaction =>
            {
                transaction.TransactionCompleted += (_, __) =>
                {
                    messageHandled.Set();
                    status = __.Transaction.TransactionInformation.Status;
                };
            };

            Exception exceptionPassedToFailureHeaderProvider = null;

            bus.SendLocal(new ErrorGeneratingMessage());            


            Thread.Sleep(3000);

            
            exceptionPassedToFailureHeaderProvider.GetRootCauseException().Should().BeOfType<RootCauseException>();            

            ((IDisposable)nsbBus).Dispose();

        }     
    }

    public class ErrorGeneratingMessage : ICommand
    {
    }

   
    public class ErrorGeneratingMessageHandler : IHandleMessages<ErrorGeneratingMessage>
    {
        public void Handle(ErrorGeneratingMessage message)
        {
            throw new RootCauseException();
        }
    }

    public class RootCauseException : Exception
    {
    }

    public class MyEndPointConfigurer : NServicebusEndpointConfigurationBase<MyEndPointConfigurer>, IConfigureThisEndpoint
    {
        private readonly string _queueName;

        public MyEndPointConfigurer(string queueName)
        {
            _queueName = queueName;
        }

        override protected Configure ConfigureLogging(Configure config)
        {
            return config;
        }

        protected override void ConfigureContainer(IWindsorContainer container)
        {
            Container = container;
            container.Register(Component.For<IServiceBus>().ImplementedBy<NServiceBusServiceBus>()
                );
        }

        public IWindsorContainer Container { get; set; }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }
}