﻿using System.Threading.Tasks;
using CqrsMovie.Messages.Events.Seat;
using CqrsMovie.Muflone.Messages.Events;
using CqrsMovie.Seats.ReadModel.EventHandlers;
using CqrsMovie.SharedKernel.ReadModel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Infrastructure.MassTransit.Events
{
    public class SeatsReservedConsumer : DomainEventConsumerBase<SeatsReserved>
    {
        public SeatsReservedConsumer(IPersister persister, ILoggerFactory loggerFactory) : base(persister, loggerFactory)
        {
        }

        protected override IDomainEventHandler<SeatsReserved> Handler => new SeatsReservedDomainEventHandler(Persister, LoggerFactory);
        public override async Task Consume(ConsumeContext<SeatsReserved> context)
        {
            using (var handler = Handler)
                await handler.Handle(context.Message);
        }
    }
}