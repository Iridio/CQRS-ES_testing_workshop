using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsMovie.Muflone.Messages;
using CqrsMovie.Muflone.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CqrsMovie.ServiceBus.MassTransit
{
  public class ServiceBus : IHostedService, IServiceBus, IEventBus
  {
    private readonly IBusControl busControl;
    private readonly ILogger<ServiceBus> logger;
    private readonly ServiceBusOptions options;

    public ServiceBus(IBusControl busControl, ILogger<ServiceBus> logger, IOptions<ServiceBusOptions> options)
    {
      this.busControl = busControl ?? throw new ArgumentNullException(nameof(busControl));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      return busControl.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return busControl.StopAsync(cancellationToken);
    }

    public async Task Send<T>(T command) where T : class, ICommand
    {
      var endPoint = await busControl.GetSendEndpoint(new Uri($"{options.BrokerUrl}{options.QueueNameCommand}"));
      await endPoint.Send(command);
    }

    [Obsolete("With MassTransit, handlers must be registered in the constructor")]
    public Task RegisterHandler<T>(Action<T> handler) where T : IMessage
    {
      throw new Exception("With MassTransit, handlers must be registered in the constructor");
    }

    public async Task Publish(IMessage @event)
    {
      await busControl.Publish(@event, @event.GetType());
    }
  }
}
