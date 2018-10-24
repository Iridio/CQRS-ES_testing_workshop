using CqrsMovie.Muflone.Core;

namespace CqrsMovie.Muflone.Messages.Commands
{
  public interface ICommand : IMessage
  {
    IDomainId AggregateId { get; }
  }
}
