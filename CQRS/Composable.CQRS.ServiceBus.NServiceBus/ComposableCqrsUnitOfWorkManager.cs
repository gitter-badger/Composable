﻿using System;
using System.Transactions;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using NServiceBus.UnitOfWork;
using log4net;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class ComposableCqrsUnitOfWorkManager : IManageUnitsOfWork
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComposableCqrsUnitOfWorkManager));
        private readonly IWindsorContainer _container;
        private UnitOfWork _unit;

        public ComposableCqrsUnitOfWorkManager(IWindsorContainer container)
        {
            Log.Debug("Constructor called");
            _container = container;
        }

        public void Begin()
        {
            Log.Debug("Begin called");
            try
            {
                if (_unit != null)
                {
                    throw new UnitOfWorkManagerReused();
                }

                AssertAmbientTransactionPresent();
                _unit = new UnitOfWork(_container.Resolve<ISingleContextUseGuard>());
                _unit.AddParticipants(_container.ResolveAll<IUnitOfWorkParticipant>());
            }
            catch (Exception e)
            {
                Log.Error("Begin failed", e);
                throw;
            }
        }

        public void End(Exception ex = null)
        {
            Log.Debug("End called");
            
            if (_unit == null)
            {
                throw new Exception("No unit present, bailing out.");
            }

            if(ex == null)
            {
                HandleEndMessage();
            }
            else
            {
                HandleError();
            }
        }


        private void HandleEndMessage()
        {
            Log.Debug("HandleEndMessage called");
            try
            {
                AssertAmbientTransactionPresent();
                _unit.Commit();
            }
            catch (Exception e)
            {
                Log.Error("HandleEndMessage failed rolling back", e);
                try
                {
                    _unit.Rollback();
                }catch(Exception e2)
                {
                    Log.Error("rolling back failed", e2);
                    throw;
                }
                throw;
            }
        }

        private void HandleError()
        {
            Log.Debug("HandleError called");

            try
            {
                _unit.Rollback();
            }
            catch (Exception e)
            {
                Log.Error("HandleError failed", e);
                throw;
            }
        }

        private static void AssertAmbientTransactionPresent()
        {
            if (Transaction.Current == null)
            {
                throw new NoAmbientTransactionException();
            }
        }
    }

    public class NoAmbientTransactionException : Exception
    {
        public NoAmbientTransactionException()
            : base("Ambient transaction required")
        {
        }
    }

    public class UnitOfWorkManagerReused : Exception
    {
        public UnitOfWorkManagerReused()
            : base("A unit of work manager may not be reused!")
        {

        }
    }
}