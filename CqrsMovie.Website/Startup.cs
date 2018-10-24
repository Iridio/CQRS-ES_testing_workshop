using CqrsMovie.ServiceBus.MassTransit;
using CqrsMovie.Website.Infrastructure.MongoDb;
using CqrsMovie.Website.Infrastructure.MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsMovie.Website
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
      services.AddMongoDb(Configuration.GetConnectionString("MongoDB"));
      services.Configure<ServiceBusOptions>(Configuration.GetSection("MassTransit:RabbitMQ"));
      var serviceBusOptions = new ServiceBusOptions();
      Configuration.GetSection("MassTransit:RabbitMQ").Bind(serviceBusOptions);
      services.AddMassTransitWithRabbitMQ(serviceBusOptions);
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
        app.UseDeveloperExceptionPage();

      else
        app.UseExceptionHandler("/Home/Error");

      app.UseStaticFiles();
      app.UseMvc(routes =>
      {
        routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
      });
    }
  }
}
