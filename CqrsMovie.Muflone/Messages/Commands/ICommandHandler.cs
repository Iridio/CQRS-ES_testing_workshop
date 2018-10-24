using System;
using System.Threading.Tasks;

namespace CqrsMovie.Muflone.Messages.Commands
{
  public interface ICommandHandler : IDisposable
  {

  }

  public interface ICommandHandler<in TCommand> : ICommandHandler where TCommand : ICommand
  {
    Task Handle(TCommand command);
  }
}