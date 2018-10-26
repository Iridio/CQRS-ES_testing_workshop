using System;
using System.Threading.Tasks;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Muflone.Persistence;
using CqrsMovie.Seats.Domain.Entities;
using CqrsMovie.SharedKernel.Domain.Ids;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Domain.CommandHandlers
{
    public class ReserveSeatCommandHandler : CommandHandler<ReserveSeat>
    {
        public ReserveSeatCommandHandler(IRepository repository, ILoggerFactory loggerFactory) : base(repository, loggerFactory)
        {
        }

        public override async Task Handle(ReserveSeat command)
        {
            try
            {
                var entity = await Repository.GetById<DailyProgramming>(command.AggregateId.Value);
                entity.ReserveSeat((DailyProgrammingId)entity.Id, command.Seats);
                await Repository.Save(entity, Guid.NewGuid(), headers => { });
            }
            catch (Exception e)
            {
                Logger?.LogError($"ReserveSeatsCommand: Error processing the command: {e.Message} - StackTrace: {e.StackTrace}");
                throw new Exception(e.Message);
            }
        }
    }
}