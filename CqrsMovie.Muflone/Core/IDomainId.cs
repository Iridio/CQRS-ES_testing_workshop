using System;

namespace CqrsMovie.Muflone.Core
{
  public interface IDomainId
  {
    Guid Value { get; }
  }
}
