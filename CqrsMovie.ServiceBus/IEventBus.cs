using System.Threading.Tasks;
using CqrsMovie.Muflone.Messages;

namespace CqrsMovie.ServiceBus
{
  public interface IEventBus
  {
    Task Publish(IMessage @event);
  }
}
