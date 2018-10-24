using System.Collections;
using CqrsMovie.Muflone.Core;

namespace CqrsMovie.Muflone
{
  public interface IAggregate
  {
    IDomainId Id { get; }
    int Version { get; }
    void ApplyEvent(object @event);
    ICollection GetUncommittedEvents();
    void ClearUncommittedEvents();
    IMemento GetSnapshot();
  }
}