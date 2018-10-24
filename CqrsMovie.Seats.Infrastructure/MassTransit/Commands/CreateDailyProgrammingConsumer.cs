using System.Threading.Tasks;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Muflone.Messages.Commands;
using CqrsMovie.Muflone.Persistence;
using CqrsMovie.Seats.Domain.CommandHandlers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Infrastructure.MassTransit.Commands
{
  public class CreateDailyProgrammingConsumer : CommandConsumerBase<CreateDailyProgramming>
  {
    public CreateDailyProgrammingConsumer(IRepository repository, ILoggerFactory loggerFactory) : base(repository, loggerFactory)
    {
    }

    protected override ICommandHandler<CreateDailyProgramming> Handler => new CreateDailyProgrammingCommandHandler(Repository, LoggerFactory);

    public override async Task Consume(ConsumeContext<CreateDailyProgramming> context)
    {
      using (var handler = Handler)
        await handler.Handle(context.Message);
    }
  }
}
