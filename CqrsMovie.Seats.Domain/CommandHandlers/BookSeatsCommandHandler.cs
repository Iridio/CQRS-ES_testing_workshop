using System;
using System.Threading.Tasks;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Muflone.Persistence;
using CqrsMovie.Seats.Domain.Entities;
using CqrsMovie.SharedKernel.Domain.Ids;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Domain.CommandHandlers
{
    public class BookSeatsCommandHandler : CommandHandler<BookSeats>
    {
        public BookSeatsCommandHandler(IRepository repository, ILoggerFactory loggerFactory) : base(repository, loggerFactory)
        {
        }

        public override async Task Handle(BookSeats command)
        {
            var entity = await Repository.GetById<DailyProgramming>(command.AggregateId.Value);
            entity.BookSeats((DailyProgrammingId)command.AggregateId, command.Seats);
            await Repository.Save(entity, Guid.NewGuid(), headers => { });
        }
    }
}