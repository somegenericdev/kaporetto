using Kaporetto.DbConnector;
using Kaporetto.DbConnector.Models;
using Kaporetto.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Serilog.Sinks.Loki;
using File = System.IO.File;





IHost host = Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((hostContext, configBuilder) =>
    {
        
        configBuilder
            .AddYamlStream(new MemoryStream(File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".kaporetto",".kaporettorc"))))
            .Build();
    })
    .ConfigureServices((hostContext,services) =>
    {
   
    
        services.AddHostedService<Worker>();
   


        services.AddDbContext<KaporettoContext>(x=>x.UseNpgsql(hostContext.Configuration["postgreConnectionString"]),ServiceLifetime.Singleton);

    }) 
    .Build();


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.LokiHttp(new NoAuthCredentials(host.Services.GetService<IConfiguration>()["lokiUrl"]),new LogLabelProvider("DbConnector"))
    .WriteTo.Console()
    .CreateLogger();

await host.RunAsync();