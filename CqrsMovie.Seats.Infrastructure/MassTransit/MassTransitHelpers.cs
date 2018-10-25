using System;
using CqrsMovie.Seats.Infrastructure.MassTransit.Commands;
using CqrsMovie.Seats.Infrastructure.MassTransit.Events;
using CqrsMovie.ServiceBus;
using CqrsMovie.ServiceBus.MassTransit;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CqrsMovie.Seats.Infrastructure.MassTransit
{
  public static class MassTransitHelpers
  {
    public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, ServiceBusOptions options)
    {
      services.AddMassTransit(x =>
        {
          x.AddConsumer<CreateDailyProgrammingConsumer>();
          x.AddConsumer<DailyProgrammingCreatedConsumer>();

          x.AddConsumer<BookSeatsConsumer>();
          x.AddConsumer<SeatsBookedConsumer>();

          x.AddConsumer<ReserveSeatsConsumer>();
          x.AddConsumer<SeatsReservedConsumer>();
        });

      services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
      {
        var host = cfg.Host(new Uri(options.BrokerUrl), h =>
        {
          h.Username(options.Login);
          h.Password(options.Password);
        });

        cfg.ReceiveEndpoint(host, options.QueueName, e =>
        {
          e.PrefetchCount = 16;
          e.UseMessageRetry(x => x.Interval(2, 100));
          e.LoadFrom(provider);
        });
      }));
      services.AddSingleton<IServiceBus, ServiceBus.MassTransit.ServiceBus>();
      services.AddSingleton<IEventBus, ServiceBus.MassTransit.ServiceBus>();
      services.AddSingleton<IHostedService, ServiceBus.MassTransit.ServiceBus>();

      return services;
    }
  }
}
