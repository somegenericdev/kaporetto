using Akka.Hosting;
using akka.App;
using Kaporetto.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using File = System.IO.File;

var hostBuilder = new HostBuilder();


string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string configPath = Path.Join(homeDirectory, ".kaporetto", ".kaporettorc");
var yamlConfig = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build().Deserialize<YamlConfig>(File.ReadAllText(configPath));



hostBuilder.
    
    ConfigureServices((context, services) =>
{
    services.AddAkka("MyActorSystem", (builder, sp) =>
    {
        builder
            .WithActors((system, registry, resolver) =>
            {
                var helloActorProps = resolver.Props<ThreadScraperActor>();
                var helloActor = system.ActorOf(helloActorProps, "hello-actor");
                registry.Register<ThreadScraperActor>(helloActor);
            })
            .WithActors((system, registry, resolver) =>
            {
                var timerActorProps =
                    resolver.Props<SchedulerActor>(); // uses Msft.Ext.DI to inject reference to helloActor
                var timerActor = system.ActorOf(timerActorProps, "timer-actor");
                registry.Register<SchedulerActor>(timerActor);
            })
            .WithActors((system, registry, resolver) =>
            {
                var dbActorProps = resolver.Props<DbActor>();
                var dbActor = system.ActorOf(dbActorProps, "db-actor");
                registry.Register<DbActor>(dbActor);
            })
            ;
    });

    
    services.AddSingleton<YamlConfig>(x=>yamlConfig);
    services.AddDbContext<KaporettoContext>(x=>x.UseLazyLoadingProxies().UseNpgsql(yamlConfig.PostgreConnectionString));

});

var host = hostBuilder.Build();

await host.RunAsync();


//
// void SetGlobals()
// {
//     string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
//     string configPath = Path.Join(homeDirectory, ".kaporetto", ".kaporettorc");
//     var config = new Deserializer().Deserialize<YamlConfig>(File.ReadAllText(configPath));
//     Globals.scraperPath = config.scraperPath;
//     Globals.MaxFileSize = config.maxFileSize;
//     // Globals.BaseUrl = config.boards.Single(x => x.alias == boardAlias).baseUrl;
// }