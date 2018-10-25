using System.Collections.Generic;
using CqrsMovie.Muflone.Core;
using CqrsMovie.Muflone.Messages.Commands;

namespace CqrsMovie.Messages.Commands.Seat
{
    public class ReserveSeat : Command
    {
        public IEnumerable<Dtos.Seat> Seats { get; }

        public ReserveSeat(IDomainId aggregateId, IEnumerable<Dtos.Seat> seats) 
            : base(aggregateId)
        {
            Seats = seats;
        }
    }
}