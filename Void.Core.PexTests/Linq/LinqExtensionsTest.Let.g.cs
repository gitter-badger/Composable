// <auto-generated>
// This file contains automatically generated unit tests.
// Do NOT modify this file manually.
// 
// When Pex is invoked again,
// it might remove or update any previously generated unit tests.
// 
// If the contents of this file becomes outdated, e.g. if it does not
// compile anymore, you may delete this file and invoke Pex again.
// </auto-generated>
using System;
using Microsoft.Pex.Framework.Generated;
using Microsoft.Pex.Framework;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.Pex.Engine.Exceptions;

namespace Void.Linq
{
    public partial class LinqExtensionsTest
    {
[Test]
[PexGeneratedBy(typeof(LinqExtensionsTest))]
public void Let02()
{
    IEnumerable<int> iEnumerable;
    iEnumerable = this.Let<int, int>((IEnumerable<int>)null, PexChoose.CreateDelegate<Func<IEnumerable<int>, IEnumerable<int>>>());
    PexAssert.IsNull((object)iEnumerable);
}
[Test]
[PexGeneratedBy(typeof(LinqExtensionsTest))]
[PexRaisedContractException(PexExceptionState.Expected)]
public void Let01()
{
    try
    {
      if (!PexContract.HasRequiredRuntimeContracts(typeof(LinqExtensions), (PexRuntimeContractsFlags)4223))
        PexAssert.Inconclusive("assembly Void.Core is not instrumented with runtime contracts");
      IEnumerable<int> iEnumerable;
      iEnumerable = this.Let<int, int>((IEnumerable<int>)null, (Func<IEnumerable<int>, IEnumerable<int>>)null);
      throw new AssertFailedException();
    }
    catch(Exception ex)
    {
      if (!PexContract.IsContractException(ex))
        throw ex;
    }
}
    }
}
