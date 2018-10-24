using System.Threading.Tasks;
using CqrsMovie.Muflone.Messages.Commands;
using CqrsMovie.Muflone.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Infrastructure.MassTransit.Commands
{
  public abstract class CommandConsumerBase<TCommand> : IConsumer<TCommand> where TCommand : Command
  {
    protected readonly IRepository Repository;
    protected readonly ILoggerFactory LoggerFactory;

    protected CommandConsumerBase(IRepository repository, ILoggerFactory loggerFactory)
    {
      Repository = repository;
      LoggerFactory = loggerFactory;
    }

    protected abstract ICommandHandler<TCommand> Handler { get; }
    public abstract Task Consume(ConsumeContext<TCommand> context);
  }

}