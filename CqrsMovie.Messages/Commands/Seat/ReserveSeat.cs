using System.Collections.Generic;
using Muflone.Core;
using Muflone.Messages.Commands;

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