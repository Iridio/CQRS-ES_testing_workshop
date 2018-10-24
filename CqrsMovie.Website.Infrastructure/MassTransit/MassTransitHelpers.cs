using System;
using CqrsMovie.ServiceBus;
using CqrsMovie.ServiceBus.MassTransit;
using CqrsMovie.Website.Infrastructure.MassTransit.Events;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CqrsMovie.Website.Infrastructure.MassTransit
{
  public static class MassTransitHelpers
  {
    public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, ServiceBusOptions options)
    {
      services.AddMassTransit(x =>
      {
        x.AddConsumer<DailyProgrammingCreatedConsumer>();
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
