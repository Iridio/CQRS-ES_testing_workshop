using System;
using System.Collections.Generic;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Messages.Dtos;
using CqrsMovie.Messages.Events.Seat;
using CqrsMovie.Muflone.Messages.Commands;
using CqrsMovie.Muflone.Messages.Events;
using CqrsMovie.Seats.Domain.CommandHandlers;
using CqrsMovie.SharedKernel.Domain.Ids;
using Microsoft.Extensions.Logging;
using Moq;

namespace CqrsMovie.Seats.Domain.Tests.Entities
{
    public class ReserveSeat_Reserving : CommandSpecification<ReserveSeat>
    {
        private readonly DailyProgrammingId aggregateId = new DailyProgrammingId(Guid.NewGuid());
        private readonly MovieId movieId = new MovieId(Guid.NewGuid());
        private readonly ScreenId screenId = new ScreenId(Guid.NewGuid());
        private readonly DateTime dailyDate = DateTime.Today;
        private readonly string movieTitle = "rambo";
        private readonly string screenName = "screen 99";
        private readonly IEnumerable<Seat> seats;

        private readonly IEnumerable<Seat> seatsToBook;
        private readonly IEnumerable<Seat> seatsToReserve;

        private readonly Mock<ILoggerFactory> loggerFactory;

        public ReserveSeat_Reserving()
        {
            seats = new List<Seat>
            {
                new Seat { Number = 1, Row = "A" },
                new Seat { Number = 2, Row = "A" },
                new Seat { Number = 3, Row = "A" },
                new Seat { Number = 4, Row = "A" },
                new Seat { Number = 1, Row = "B" },
                new Seat { Number = 2, Row = "B" },
                new Seat { Number = 3, Row = "B" },
                new Seat { Number = 4, Row = "B" }
            };

            seatsToBook = new List<Seat>
            {
                new Seat { Number = 1, Row = "B" },
                new Seat { Number = 2, Row = "B" },
                new Seat { Number = 3, Row = "B" }
            };

            seatsToReserve = new List<Seat>
            {
                new Seat { Number = 1, Row = "B" },
                new Seat { Number = 2, Row = "B" },
                new Seat { Number = 3, Row = "B" }
            };

            loggerFactory = new Mock<ILoggerFactory>();
        }

        protected override IEnumerable<DomainEvent> Given()
        {
            yield return new DailyProgrammingCreated(aggregateId, movieId, screenId, dailyDate, seats, movieTitle, screenName);
            yield return new SeatsBooked(aggregateId, seatsToBook);
        }

        protected override ReserveSeat When()
        {
            return new ReserveSeat(aggregateId, seatsToReserve);
        }

        protected override ICommandHandler<ReserveSeat> OnHandler()
        {
            return new ReserveSeatCommandHandler(Repository, loggerFactory.Object);
        }

        protected override IEnumerable<DomainEvent> Expect()
        {
            yield return new SeatsReserved(aggregateId, seatsToReserve);
        }
    }
}