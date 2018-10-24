using CqrsMovie.Muflone.EventStore.Persistence;
using CqrsMovie.Muflone.Persistence;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CqrsMovie.Seats.Infrastructure.EventStore
{
  public static class EventStoreHelpers
  {
    public static IServiceCollection AddEventStore(this IServiceCollection services, string connectionString)
    {
      services.AddSingleton<IEventStoreConnection>(provider =>
      {
        var connection = EventStoreConnection.Create(connectionString);
        connection.ConnectAsync();
        return connection;
      });
      services.AddScoped<IRepository, EventStoreRepository>();
      services.AddSingleton<IHostedService, EventDispatcher>();
      return services;
    }
  }
}
