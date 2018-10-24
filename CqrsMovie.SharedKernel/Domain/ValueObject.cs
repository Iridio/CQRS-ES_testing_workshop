﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CqrsMovie.SharedKernel.Domain
{
  //Taken from here: https://enterprisecraftsmanship.com/2017/08/28/value-object-a-better-implementation/
  public abstract class ValueObject
  {
    /// <summary>
    /// The class overriding this just need to return:
    /// yield return Field1;
    /// yield return Field2;
    /// ...
    /// yield return FieldN;
    /// In case of lists, it is enough to do this:
    /// foreach (Tenant tenant in Tenants)
    /// {
    ///   yield return tenant;
    /// }
    /// </summary>
    /// <returns>List of objects to compare</returns>
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object obj)
    {
      if (obj == null)
        return false;
      if (GetType() != obj.GetType())
        throw new ArgumentException($"Invalid comparison of Value Objects of different types: {GetType()} and {obj.GetType()}");
      var valueObject = (ValueObject)obj;
      return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
      return GetEqualityComponents().Aggregate(1, (current, obj) => { unchecked { return current * 23 + (obj?.GetHashCode() ?? 0); } });
    }

    public static bool operator ==(ValueObject a, ValueObject b)
    {
      if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
        return true;
      if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        return false;
      return a.Equals(b);
    }

    public static bool operator !=(ValueObject a, ValueObject b)
    {
      return !(a == b);
    }
  }
}