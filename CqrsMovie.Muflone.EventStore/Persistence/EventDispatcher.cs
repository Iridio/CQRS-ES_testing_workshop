using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CqrsMovie.Muflone.Messages.Events;
using CqrsMovie.ServiceBus;
using EventStore.ClientAPI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CqrsMovie.Muflone.EventStore.Persistence
{
  public class EventDispatcher : IHostedService
  {
    private const int ReconnectTimeoutMillisec = 5000;
    private const int ThreadKillTimeoutMillisec = 5000;
    private const int ReadPageSize = 500;
    private const int LiveQueueSizeLimit = 10000;

    private readonly IEventBus eventBus;
    private readonly IEventStorePositionRepository eventStorePositionRepository;
    private readonly IEventStoreConnection eventStoreConnection;
    private readonly ConcurrentQueue<ResolvedEvent> liveQueue = new ConcurrentQueue<ResolvedEvent>();
    private readonly ManualResetEventSlim liveDone = new ManualResetEventSlim(true);
    private readonly ConcurrentQueue<ResolvedEvent> historicalQueue = new ConcurrentQueue<ResolvedEvent>();
    private readonly ManualResetEventSlim historicalDone = new ManualResetEventSlim(true);

    private volatile bool stop;
    private volatile bool livePublishingAllowed;
    private int isPublishing;
    private Position lastProcessed;
    private EventStoreSubscription eventStoreSubscription;
    private readonly ILogger log;

    public EventDispatcher(ILoggerFactory loggerFactory, IEventStoreConnection store, IEventBus eventBus, IEventStorePositionRepository eventStorePositionRepository)
    {
      log = loggerFactory?.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
      eventStoreConnection = store ?? throw new ArgumentNullException(nameof(store));
      this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
      this.eventStorePositionRepository = eventStorePositionRepository;
      lastProcessed = eventStorePositionRepository.GetLastPosition().GetAwaiter().GetResult();
    }

    // Credit algorithm to Szymon Pobiega
    // http://simon-says-architecture.com/2013/02/02/mechanics-of-durable-subscription/#comments
    // 1. The subscriber always starts with pull assuming there were some messages generated while it was offline
    // 2. The subscriber pulls messages until thereâ€™s nothing left to pull (it is up to date with the stream)
    // 3. Push subscription is started  but arriving messages are not processed immediately but temporarily redirected to a buffer
    // 4. One last pull is done to ensure nothing happened between step 2 and 3
    // 5. Messages from this last pull are processed
    // 6. Processing messages from push buffer is started. While messages are processed, they are checked against IDs of messages processed in step 5 to ensure thereâ€™s no duplicates.
    // 7. System works in push model until subscriber is killed or subscription is dropped by publisher drops push subscription.

    //Credit to Andrii Nakryiko
    //If data is written to storage at such a speed, that between the moment you did your last 
    //pull read and the moment you subscribed to push notifications more data (events) were 
    //generated, than you request in one pull request, you would need to repeat steps 4-5 few 
    //times until you get a pull message which position is >= subscription position 
    //(EventStore provides you with those positions).
    public Task StartDispatching()
    {
      return RecoverSubscription();
    }

    private async Task RecoverSubscription()
    {
      livePublishingAllowed = false;
      liveDone.Wait(); // wait until all live processing is finished (queue is empty, _lastProcessed updated)

      //AN: if _lastProcessed == (-1, -1) then we haven't processed anything yet, so we start from Position.Start
      var startPos = lastProcessed == new Position(-1, -1) ? Position.Start : lastProcessed;
      var nextPos = await ReadHistoricalEventsFrom(startPos);

      eventStoreSubscription = await SubscribeToAll();

      await ReadHistoricalEventsFrom(nextPos);
      historicalDone.Wait(); // wait until historical queue is empty and _lastProcessed updated

      livePublishingAllowed = true;
      EnsurePublishEvents(liveQueue, liveDone);

    }

    public Task StopDispatching()
    {
      stop = true;
      eventStoreSubscription?.Unsubscribe();

      // hopefully additional check in PublishEvents (additional check for _stop after setting event) prevents race conditions
      if (!historicalDone.Wait(ThreadKillTimeoutMillisec))
        throw new TimeoutException("DispatchStoppingException");

      if (!liveDone.Wait(ThreadKillTimeoutMillisec))
        throw new TimeoutException("DispatchStoppingException");
      return Task.CompletedTask;
    }

    private async Task<Position> ReadHistoricalEventsFrom(Position from)
    {
      var position = from;
      AllEventsSlice slice;
      while (!stop && (slice = await eventStoreConnection.ReadAllEventsForwardAsync(position, ReadPageSize, false)).Events.Length > 0)
      {
        foreach (var rawEvent in slice.Events)
        {
          historicalQueue.Enqueue(rawEvent);
        }
        EnsurePublishEvents(historicalQueue, historicalDone);

        position = slice.NextPosition;
      }
      return position;
    }

    private Task<EventStoreSubscription> SubscribeToAll()
    {
      //TODO: Before trying to resubscribe - how to ensure that store is active and ready to accept.
      //AN: EventStoreConnection automatically tries to connect (if not already connected) to EventStore,
      //so you don't have to do something manually
      //Though in case of errors, you need to do some actions (if EventStore server is down or not yet up, etc)
      var task = eventStoreConnection.SubscribeToAllAsync(false, EventAppeared, SubscriptionDropped);
      if (!task.Wait(ReconnectTimeoutMillisec))
        throw new TimeoutException("ReconnectedAfterSubscriptionException");
      return Task.FromResult(task.GetAwaiter().GetResult());

      //return await eventStoreConnection.SubscribeToAllAsync(false, EventAppeared, SubscriptionDropped);
    }

    private void SubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason dropReason, Exception exception)
    {
      if (stop)
        return;

      RecoverSubscription().GetAwaiter().GetResult();
    }

    private Task EventAppeared(EventStoreSubscription eventStoreSubscription, ResolvedEvent resolvedEvent)
    {
      if (stop)
        return Task.CompletedTask;

      liveQueue.Enqueue(resolvedEvent);

      //Prevent live queue memory explosion.
      if (!livePublishingAllowed && liveQueue.Count > LiveQueueSizeLimit)
      {
        liveQueue.TryDequeue(out var throwAwayEvent);
      }

      if (livePublishingAllowed)
        EnsurePublishEvents(liveQueue, liveDone);
      return Task.CompletedTask;
    }

    private void EnsurePublishEvents(ConcurrentQueue<ResolvedEvent> queue, ManualResetEventSlim doneEvent)
    {
      if (stop) return;

      if (Interlocked.CompareExchange(ref isPublishing, 1, 0) == 0)
        ThreadPool.QueueUserWorkItem(_ => PublishEvents(queue, doneEvent));
    }

    private void PublishEvents(ConcurrentQueue<ResolvedEvent> queue, ManualResetEventSlim doneEvent)
    {
      var keepGoing = true;
      while (keepGoing)
      {
        doneEvent.Reset(); // signal we start processing this queue
        if (stop) // this is to avoid race condition in StopDispatching, though it is 1AM here, so I could be wrong :)
        {
          doneEvent.Set();
          Interlocked.CompareExchange(ref isPublishing, 0, 1);
          return;
        }
        ResolvedEvent @event;
        while (!stop && queue.TryDequeue(out @event))
        {
          if (!(@event.OriginalPosition > lastProcessed)) continue;

          var processedEvent = ProcessRawEvent(@event);
          if (processedEvent != null)
          {
            processedEvent.Headers.Set(Constants.CommitPosition, @event.OriginalPosition.Value.CommitPosition.ToString());
            processedEvent.Headers.Set(Constants.PreparePosition, @event.OriginalPosition.Value.PreparePosition.ToString());
            eventBus.Publish(processedEvent);
          }
          lastProcessed = @event.OriginalPosition.Value;
          //TODO: Should be moved in the event handlers to be sure the event is persisted and then the counter updated?
          eventStorePositionRepository.Save(lastProcessed.CommitPosition, lastProcessed.PreparePosition);
        }
        doneEvent.Set(); // signal end of processing particular queue
        Interlocked.CompareExchange(ref isPublishing, 0, 1);
        // try to reacquire lock if needed
        keepGoing = !stop && queue.Count > 0 && Interlocked.CompareExchange(ref isPublishing, 1, 0) == 0;
      }
    }

    private DomainEvent ProcessRawEvent(ResolvedEvent rawEvent)
    {
      if (rawEvent.OriginalEvent.Metadata.Length > 0 && rawEvent.OriginalEvent.Data.Length > 0)
        return DeserializeEvent(rawEvent.OriginalEvent.Metadata, rawEvent.OriginalEvent.Data);

      return null;
    }

    /// <summary>
    /// Deserializes the event from the raw GetEventStore event to my event.
    /// Took this from a gist that James Nugent posted on the GetEventStore forums.
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    private DomainEvent DeserializeEvent(byte[] metadata, byte[] data)
    {
      if (Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString(metadata)).Property("EventClrTypeName") == null)
        return null;

      var eventClrTypeName = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString(metadata)).Property("EventClrTypeName").Value;

      try
      {
        return (DomainEvent)Newtonsoft.Json.JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), Type.GetType((string)eventClrTypeName));
      }
      catch (Exception ex)
      {
        log.LogError(ex.Message);
        return null;
      }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      return StartDispatching();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return StopDispatching();
    }
  }
}
