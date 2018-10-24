using System;

namespace CqrsMovie.Muflone.Persistence
{
  public interface IConstructAggregates
  {
    IAggregate Build(Type type, Guid id, IMemento snapshot);
  }
}