using System;

namespace CqrsMovie.Muflone
{
  public interface IRouteEvents
  {
    void Register<T>(Action<T> handler);

    void Register(IAggregate aggregate);

    void Dispatch(object eventMessage);
  }
}