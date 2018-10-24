using System.Threading.Tasks;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Muflone.Messages.Commands;
using CqrsMovie.Muflone.Persistence;
using CqrsMovie.Seats.Domain.CommandHandlers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Infrastructure.MassTransit.Commands
{
    public class BookSeatsConsumer : CommandConsumerBase<BookSeats>
    {
        public BookSeatsConsumer(IRepository repository, ILoggerFactory loggerFactory) : base(repository, loggerFactory)
        {
        }

        protected override ICommandHandler<BookSeats> Handler => new BookSeatsCommandHandler(Repository, LoggerFactory);
        public override async Task Consume(ConsumeContext<BookSeats> context)
        {
            using (var handler = Handler)
                await handler.Handle(context.Message);
        }
    }
}