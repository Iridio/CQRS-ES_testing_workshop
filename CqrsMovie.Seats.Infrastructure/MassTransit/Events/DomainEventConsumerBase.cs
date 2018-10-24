using System.Threading.Tasks;
using CqrsMovie.Muflone.Messages.Events;
using CqrsMovie.SharedKernel.ReadModel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Infrastructure.MassTransit.Events
{
  public abstract class DomainEventConsumerBase<TEvent> : IConsumer<TEvent> where TEvent : DomainEvent
  {
    protected readonly IPersister Persister;
    protected readonly ILoggerFactory LoggerFactory;

    protected DomainEventConsumerBase(IPersister persister,ILoggerFactory loggerFactory)
    {
      Persister = persister;
      LoggerFactory = loggerFactory;
    }

    protected abstract IDomainEventHandler<TEvent> Handler { get; }
    public abstract Task Consume(ConsumeContext<TEvent> context);
    }
}