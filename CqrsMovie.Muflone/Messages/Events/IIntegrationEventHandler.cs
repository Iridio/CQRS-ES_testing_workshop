using System.Threading.Tasks;

namespace CqrsMovie.Muflone.Messages.Events
{
  public interface IIntegrationEventHandler
  {
  }

  public interface IIntegrationEventHandler<in T> : IIntegrationEventHandler where T : IntegrationEvent
  {
    Task Handle(T @event);
  }
}