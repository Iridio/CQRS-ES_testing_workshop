using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Messages.Dtos;
using CqrsMovie.Messages.Events.Seat;
using CqrsMovie.Muflone.Messages.Events;
using CqrsMovie.Seats.Domain.CommandHandlers;
using CqrsMovie.SharedKernel.Domain.Ids;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CqrsMovie.Seats.Domain.Tests.Entities
{
    public class ReserveSeats_Fail
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
        private readonly InMemoryEventRepository repository;

        public ReserveSeats_Fail()
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
                new Seat { Number = 1, Row = "C" },
                new Seat { Number = 2, Row = "C" },
                new Seat { Number = 3, Row = "C" }
            };

            loggerFactory = new Mock<ILoggerFactory>();
            repository = new InMemoryEventRepository();
        }

        [Fact]
        public async Task Cannot_ReserveSeats_NotBooked()
        {
            var eventsSucceded = new List<DomainEvent>
            {
                new DailyProgrammingCreated(aggregateId, movieId, screenId, dailyDate, seats, movieTitle, screenName),
                new SeatsBooked(aggregateId, seatsToBook)
            };

            repository.ApplyGivenEvents(eventsSucceded);

            var reserveSeatsCommand = new ReserveSeat(aggregateId, seatsToReserve);
            var reserveSeatsCommandHandler = new ReserveSeatCommandHandler(repository, loggerFactory.Object);

            Exception ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                reserveSeatsCommandHandler.Handle(reserveSeatsCommand));

            Assert.Equal("Timeout expired!", ex.Message);
        }
    }
}