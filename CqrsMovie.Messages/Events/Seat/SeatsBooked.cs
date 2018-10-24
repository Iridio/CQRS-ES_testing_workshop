using System;
using System.Collections.Generic;
using CqrsMovie.Muflone.Core;
using CqrsMovie.Muflone.Messages.Events;
using CqrsMovie.SharedKernel.Domain.Ids;

namespace CqrsMovie.Messages.Events.Seat
{
    public class SeatsBooked : DomainEvent
    {
        public IEnumerable<Dtos.Seat> Seats { get; }

        public SeatsBooked(DailyProgrammingId aggregateId, IEnumerable<Dtos.Seat> seats, string who = "anonymous") 
            : base(aggregateId, who)
        {
            Seats = seats;
        }
    }
}