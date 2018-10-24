using System;
using System.Threading.Tasks;

namespace CqrsMovie.Muflone.Messages.Events
{
  public interface IDomainEventHandler : IDisposable
  {
  }

  public interface IDomainEventHandler<in TEvent> : IDomainEventHandler where TEvent : IDomainEvent
  {
    Task Handle(TEvent @event);
  }
}