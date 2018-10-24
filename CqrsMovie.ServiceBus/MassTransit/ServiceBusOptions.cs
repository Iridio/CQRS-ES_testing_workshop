namespace CqrsMovie.ServiceBus.MassTransit
{
  public class ServiceBusOptions
  {
    public string QueueName { get; set; }
    public string BrokerUrl { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string QueueNameCommand { get; set; }
  }
}