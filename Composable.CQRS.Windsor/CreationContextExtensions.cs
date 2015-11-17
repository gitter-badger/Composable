using System;
using System.Reflection;
using Castle.MicroKernel.Context;

namespace Composable.CQRS.Windsor
{
    public static  class CreationContextExtensions
    {
        [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
        public static Type RequestingType(this CreationContext me)
        {
            return me.Handler.ComponentModel.Implementation;
        }

        [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
        public static Assembly RequestingAssembly(this CreationContext me)
        {
            return RequestingType(me).Assembly;
        }
    }
}