using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CqrsMovie.Muflone.Core;
using CqrsMovie.Muflone.Persistence;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CqrsMovie.Muflone.EventStore.Persistence
{
  public class EventStoreRepository : IRepository
  {
    private const string EventClrTypeHeader = "EventClrTypeName";
    private const string AggregateClrTypeHeader = "AggregateClrTypeName";
    private const string CommitIdHeader = "CommitId";
    private const string CommitDateHeader = "CommitDate";
    private const int WritePageSize = 500;
    private const int ReadPageSize = 500;

    private readonly Func<Type, Guid, string> aggregateIdToStreamName;

    private readonly IEventStoreConnection eventStoreConnection;
    private static readonly JsonSerializerSettings SerializerSettings;

    static EventStoreRepository()
    {
      SerializerSettings = new JsonSerializerSettings
      {
        TypeNameHandling = TypeNameHandling.None,
        ContractResolver = new PrivateContractResolver()
      };
    }

    //This rename is to be consistent with naming convention of EventStore javascript
    public EventStoreRepository(IEventStoreConnection eventStoreConnection)
      : this(eventStoreConnection, (type, aggregateId) => $"{char.ToLower(type.Name[0]) + type.Name.Substring(1)}-{aggregateId}")
    {

    }

    public EventStoreRepository(IEventStoreConnection eventStoreConnection, Func<Type, Guid, string> aggregateIdToStreamName)
    {
      this.eventStoreConnection = eventStoreConnection;
      this.aggregateIdToStreamName = aggregateIdToStreamName;
    }

    public async Task<TAggregate> GetById<TAggregate>(Guid id) where TAggregate : class, IAggregate
    {
      return await GetById<TAggregate>(id, int.MaxValue);
    }

    public Task<TAggregate> GetById<TAggregate>(Guid id, int version) where TAggregate : class, IAggregate
    {
      if (version <= 0)
        throw new InvalidOperationException("Cannot get version <= 0");

      var streamName = aggregateIdToStreamName(typeof(TAggregate), id);
      var aggregate = ConstructAggregate<TAggregate>();

      long sliceStart = 0;
      StreamEventsSlice currentSlice;
      do
      {
        var sliceCount = (int)(sliceStart + ReadPageSize <= version ? ReadPageSize : version - sliceStart + 1);
        currentSlice = eventStoreConnection.ReadStreamEventsForwardAsync(streamName, sliceStart, sliceCount, false).Result;

        if (currentSlice.Status == SliceReadStatus.StreamNotFound)
          throw new AggregateNotFoundException(id, typeof(TAggregate));

        if (currentSlice.Status == SliceReadStatus.StreamDeleted)
          throw new AggregateDeletedException(id, typeof(TAggregate));

        sliceStart = currentSlice.NextEventNumber;

        foreach (var @event in currentSlice.Events)
          aggregate.ApplyEvent(DeserializeEvent(@event.OriginalEvent.Metadata, @event.OriginalEvent.Data));
      } while (version >= currentSlice.NextEventNumber && !currentSlice.IsEndOfStream);

      if (aggregate.Version != version && version < int.MaxValue)
        throw new AggregateVersionException(id, typeof(TAggregate), aggregate.Version, version);

      return Task.FromResult(aggregate);
    }

    private static TAggregate ConstructAggregate<TAggregate>()
    {
      return (TAggregate)Activator.CreateInstance(typeof(TAggregate), true);
    }

    private static object DeserializeEvent(byte[] metadata, byte[] data)
    {
      var eventClrTypeName = JObject.Parse(Encoding.UTF8.GetString(metadata)).Property(EventClrTypeHeader).Value;
      return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), Type.GetType((string)eventClrTypeName));
    }

    public async Task Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
    {
      var commitHeaders = new Dictionary<string, object>
      {
        { CommitIdHeader, commitId },
        { CommitDateHeader, DateTime.UtcNow},
        { AggregateClrTypeHeader, aggregate.GetType().AssemblyQualifiedName }
      };
      updateHeaders(commitHeaders);

      var streamName = aggregateIdToStreamName(aggregate.GetType(), aggregate.Id.Value);
      var newEvents = aggregate.GetUncommittedEvents().Cast<object>().ToList();
      var originalVersion = aggregate.Version - newEvents.Count;
      var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion - 1;
      var eventsToSave = newEvents.Select(e => ToEventData(Guid.NewGuid(), e, commitHeaders)).ToList();

      if (eventsToSave.Count < WritePageSize)
      {
        eventStoreConnection.AppendToStreamAsync(streamName, expectedVersion, eventsToSave).Wait();
      }
      else
      {
        var transaction = eventStoreConnection.StartTransactionAsync(streamName, expectedVersion).Result;

        var position = 0;
        while (position < eventsToSave.Count)
        {
          var pageEvents = eventsToSave.Skip(position).Take(WritePageSize);
          await transaction.WriteAsync(pageEvents);
          position += WritePageSize;
        }
        await transaction.CommitAsync();
        transaction.Dispose();
      }
      aggregate.ClearUncommittedEvents();
    }

    private static EventData ToEventData(Guid eventId, object @event, IDictionary<string, object> headers)
    {
      var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, SerializerSettings));
      var eventHeaders = new Dictionary<string, object>(headers) { { EventClrTypeHeader, @event.GetType().AssemblyQualifiedName } };
      var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeaders, SerializerSettings));
      var typeName = @event.GetType().Name;
      return new EventData(eventId, typeName, true, data, metadata);
    }

    #region IDisposable Support
    private bool disposedValue; // To detect redundant calls
    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          // TODO: dispose managed state (managed objects).
        }
        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.
        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~EventStoreRepository() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }

  internal class PrivateContractResolver : DefaultContractResolver
  {
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
      var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Select(p => base.CreateProperty(p, memberSerialization))
        .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Select(f => base.CreateProperty(f, memberSerialization)))
        .ToList();
      props.ForEach(p => { p.Writable = true; p.Readable = true; });
      return props;
    }
  }
}