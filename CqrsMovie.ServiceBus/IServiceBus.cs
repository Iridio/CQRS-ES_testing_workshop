using System;
using System.Threading.Tasks;
using CqrsMovie.Muflone.Messages;
using CqrsMovie.Muflone.Messages.Commands;

namespace CqrsMovie.ServiceBus
{
  public interface IServiceBus
  {
    Task Send<T>(T command) where T : class, ICommand;
    Task RegisterHandler<T>(Action<T> handler) where T : IMessage;
  }
}
