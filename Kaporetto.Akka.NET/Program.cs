using Akka.DependencyInjection;
using Akka.Hosting;
using Kaporetto.Akka.NET;
using Kaporetto.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using File = System.IO.File;

var hostBuilder = new HostBuilder();


string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string configPath = Path.Join(homeDirectory, ".kaporetto", ".kaporettorc");
var yamlConfig = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build().Deserialize<YamlConfig>(File.ReadAllText(configPath));
var log=Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.LokiHttp(new NoAuthCredentials(yamlConfig.LokiUrl),new LogLabelProvider("Scraper"))
    .WriteTo.Console()
    .CreateLogger();


hostBuilder.ConfigureServices((context, services) =>
{
    services.AddAkka("MyActorSystem", (builder, sp) =>
    {
        builder
            .WithActors((system, registry, resolver) =>
            {
                var threadScraperProps = resolver.Props<ThreadScraperActor>();
                var threadScraperActor = system.ActorOf(threadScraperProps, "thread-scraper-actor");
                registry.Register<ThreadScraperActor>(threadScraperActor);
            })
            .WithActors((system, registry, resolver) =>
            {
                var schedulerActorProps =
                    resolver.Props<SchedulerActor>(); // uses Msft.Ext.DI to inject reference to helloActor
                var schedulerActor = system.ActorOf(schedulerActorProps, "scheduler-actor");
                registry.Register<SchedulerActor>(schedulerActor);
            })
            .WithActors((system, registry, resolver) =>
            {
                var dbActorProps = resolver.Props<DbActor>();
                var dbActor = system.ActorOf(dbActorProps, "db-actor");
                registry.Register<DbActor>(dbActor);
            });
    });

    services.AddSingleton<ILogger>(x=>log);
    services.AddSingleton<YamlConfig>(x=>yamlConfig);
    services.AddDbContext<KaporettoContext>(x=>x.UseLazyLoadingProxies().UseNpgsql(yamlConfig.PostgreConnectionString));

});

var host = hostBuilder.Build();

await host.RunAsync();
