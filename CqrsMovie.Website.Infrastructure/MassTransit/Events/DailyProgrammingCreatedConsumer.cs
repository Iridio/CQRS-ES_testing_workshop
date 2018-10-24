﻿using System.Threading.Tasks;
using CqrsMovie.Messages.Events.Seat;
using CqrsMovie.Muflone.Messages.Events;
using CqrsMovie.SharedKernel.ReadModel;
using CqrsMovie.Website.ReadModel.EventHandlers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Website.Infrastructure.MassTransit.Events
{
  public class DailyProgrammingCreatedConsumer : DomainEventConsumerBase<DailyProgrammingCreated>
  {
    public DailyProgrammingCreatedConsumer(IPersister persister, ILoggerFactory loggerFactory) : base(persister, loggerFactory)
    {
    }

    protected override IDomainEventHandler<DailyProgrammingCreated> Handler => new DailyProgrammingCreatedDomainDomainEventHandler(Persister, LoggerFactory);
    public override async Task Consume(ConsumeContext<DailyProgrammingCreated> context)
    {
      using (var handler = Handler)
        await handler.Handle(context.Message);
    }
  }
}
