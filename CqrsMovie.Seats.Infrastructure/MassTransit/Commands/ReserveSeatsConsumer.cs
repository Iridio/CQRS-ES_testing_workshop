using System.Threading.Tasks;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Muflone.Messages.Commands;
using CqrsMovie.Muflone.Persistence;
using CqrsMovie.Seats.Domain.CommandHandlers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Infrastructure.MassTransit.Commands
{
    public class ReserveSeatsConsumer : CommandConsumerBase<ReserveSeat>
    {
        public ReserveSeatsConsumer(IRepository repository, ILoggerFactory loggerFactory) : base(repository, loggerFactory)
        {
        }

        protected override ICommandHandler<ReserveSeat> Handler => new ReserveSeatCommandHandler(Repository, LoggerFactory);
        public override async Task Consume(ConsumeContext<ReserveSeat> context)
        {
            using (var handler = Handler)
                await handler.Handle(context.Message);
        }
    }
}