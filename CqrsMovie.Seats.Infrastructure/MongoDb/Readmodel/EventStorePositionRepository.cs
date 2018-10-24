using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsMovie.Messages.Dtos;
using CqrsMovie.Muflone.EventStore;
using CqrsMovie.Muflone.EventStore.Persistence;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CqrsMovie.Seats.Infrastructure.MongoDb.Readmodel
{
  public class EventStorePositionRepository : IEventStorePositionRepository
  {
    private readonly ILogger<EventStorePositionRepository> logger;
    private readonly IMongoDatabase database;
    public EventStorePositionRepository(ILogger<EventStorePositionRepository> logger, string connectionString)
    {
      this.logger = logger;
      BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;
      var client = new MongoClient(connectionString);
      database = client.GetDatabase("CqrsMovie_EventStore_Position"); //Best to inject a class with all parameter and not being coupled like this
    }

    public async Task<Position> GetLastPosition()
    {
      try
      {
        var collection = database.GetCollection<LastEventPosition>(typeof(LastEventPosition).Name);
        var filter = Builders<LastEventPosition>.Filter.Eq("_id", Constants.LastEventPositionKey);
        var result = await collection.CountDocumentsAsync(filter) > 0 ? (await collection.FindAsync(filter)).First() : null;
        if (result == null)
        {
            result = new LastEventPosition
            {
              Id = Constants.LastEventPositionKey,
              CommitPosition = -1,
              PreparePosition = -1
            };
            await collection.InsertOneAsync(result);
        }
        return new Position(result.CommitPosition, result.PreparePosition);
      }
      catch (Exception e)
      {
        logger.LogError($"Error getting LastSavedPostion, Message: {e.Message}, StackTrace: {e.StackTrace}");
        throw;
      }
    }

    public async Task Save(long commitPosition, long preparePosition)
    {
      var retryCount = 0;
      while (retryCount < 3)
      {
        try
        {
          var collection = database.GetCollection<LastEventPosition>(typeof(LastEventPosition).Name);
          var filter = Builders<LastEventPosition>.Filter.Eq("_id", Constants.LastEventPositionKey);
          var entity = await collection.Find(filter).FirstOrDefaultAsync();
          if (entity == null)
          {
            entity = new LastEventPosition
            {
              Id = Constants.LastEventPositionKey,
              CommitPosition = commitPosition,
              PreparePosition = preparePosition
            };
            await collection.InsertOneAsync(entity);
          }
          else
          {
            if (commitPosition > entity.CommitPosition && preparePosition > entity.PreparePosition)
            {
              entity.CommitPosition = commitPosition;
              entity.PreparePosition = preparePosition;
              await collection.FindOneAndReplaceAsync(filter, entity);
            }
          }
          retryCount = 999;
        }
        catch (Exception e)
        {
          retryCount++;
          logger.LogError($"UpdateLastEventPosition: Error while updating commit position: {e.Message}, StackTrace: {e.StackTrace}");
          Thread.Sleep(100 * retryCount);
          //throw;
        }
      }
    }
  }
}
