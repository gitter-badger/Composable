// <copyright file="SimpleIndexingTest.Third.g.cs" company="Microsoft">Copyright © Microsoft 2009</copyright>
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
using System.Collections.Generic;
using Microsoft.Pex.Engine.Exceptions;
using Microsoft.Pex.Framework;
using NUnit.Framework;

namespace Void.Linq
{
    public partial class SimpleIndexingTest
    {
[PexGeneratedBy(typeof(SimpleIndexingTest))]
[PexRaisedContractException(PexExceptionState.Expected)]
public void Third01()
{
    try
    {
      if (!PexContract.HasRequiredRuntimeContracts(typeof(SimpleIndexing), (PexRuntimeContractsFlags)4223))
      {
      }
      int i;
      i = this.Third<int>((IEnumerable<int>)null);
      throw new ApplicationException();
    }
    catch(Exception ex)
    {
      if (!PexContract.IsContractException(ex))
        throw ex;
    }
}
[PexGeneratedBy(typeof(SimpleIndexingTest))]
[PexRaisedException(typeof(ArgumentOutOfRangeException), PexExceptionState.Inconclusive)]
public void Third02()
{
    try
    {
      int i;
      int[] ints = new int[0];
      i = this.Third<int>((IEnumerable<int>)ints);
      throw new ApplicationException();
    }
    catch(ArgumentOutOfRangeException )
    {
    }
}
[PexGeneratedBy(typeof(SimpleIndexingTest))]
public void Third03()
{
    int i;
    int[] ints = new int[3];
    i = this.Third<int>((IEnumerable<int>)ints);
    PexAssert.AreEqual<int>(0, i);
}
[Test]
[PexGeneratedBy(typeof(SimpleIndexingTest))]
[PexRaisedContractException(PexExceptionState.Expected)]
public void Third04()
{
    try
    {
      if (!PexContract.HasRequiredRuntimeContracts(typeof(SimpleIndexing), (PexRuntimeContractsFlags)4223))
        PexAssert.Inconclusive("assembly Void.Core is not instrumented with runtime contracts");
      int i;
      i = this.Third<int>((IEnumerable<int>)null);
      throw new AssertFailedException();
    }
    catch(Exception ex)
    {
      if (!PexContract.IsContractException(ex))
        throw ex;
    }
}
[Test]
[PexGeneratedBy(typeof(SimpleIndexingTest))]
[ExpectedException(typeof(ArgumentOutOfRangeException))]
public void Third05()
{
    int i;
    int[] ints = new int[0];
    i = this.Third<int>((IEnumerable<int>)ints);
}
[Test]
[PexGeneratedBy(typeof(SimpleIndexingTest))]
public void Third06()
{
    int i;
    int[] ints = new int[3];
    i = this.Third<int>((IEnumerable<int>)ints);
    PexAssert.AreEqual<int>(0, i);
}
    }
}
