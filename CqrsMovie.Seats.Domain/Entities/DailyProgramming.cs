using System;
using System.Collections.Generic;
using System.Linq;
using CqrsMovie.Core.Enums;
using CqrsMovie.Messages.Events.Seat;
using CqrsMovie.SharedKernel.Domain;
using CqrsMovie.SharedKernel.Domain.Ids;

namespace CqrsMovie.Seats.Domain.Entities
{
    public class DailyProgramming : AggregateRoot
    {
        private MovieId movieId;
        private ScreenId screenId;
        private DateTime date;
        private IList<Seat> seats;

        //TODO: Implement user information (due to online shopping)
        //private Guid userId;

        protected DailyProgramming()
        { }

        public DailyProgramming(DailyProgrammingId aggregateId, MovieId movieId, ScreenId screenId, DateTime date, IEnumerable<Messages.Dtos.Seat> freeSeats, string movieTitle, string screenName)
        {
            //Null checks etc. ....


            RaiseEvent(new DailyProgrammingCreated(aggregateId, movieId, screenId, date, freeSeats, movieTitle, screenName));
        }

        private void Apply(DailyProgrammingCreated @event)
        {
            Id = @event.AggregateId;
            movieId = @event.MovieId;
            screenId = @event.ScreenId;
            date = @event.Date;
            seats = @event.Seats.ToEntity(SeatState.Free);
        }

        #region BookSeats
        internal void BookSeats(DailyProgrammingId aggregateId, IEnumerable<Messages.Dtos.Seat> seatsToBook)
        {
            // Chk for seats availability
            var seatsToChk = this.seats.Where(seat =>
                seatsToBook.Any(book => book.Row.Equals(seat.Row) && book.Number.Equals(seat.Number)) 
                && seat.State.Equals(SeatState.Free));

            if (seatsToChk.Count() != seatsToBook.Count())
                throw new Exception("Seats Not Available!");

            // Raise event
            RaiseEvent(new SeatsBooked(aggregateId, seatsToBook));
        }

        private void Apply(SeatsBooked @event)
        {
            // TODO: something better???
            @event.Seats.ToList().ForEach(seatBooked =>
            {
                var seat = seats.FirstOrDefault(s => s.Row.Equals(seatBooked.Row) && s.Number.Equals(seatBooked.Number));
                if (seat != null) // should be always true...
                {
                    seats.Remove(seat);
                    seats.Add(new Seat(seat.Row, seat.Number, SeatState.Booked));
                }
            });
        }
        #endregion

    }
}
