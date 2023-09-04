using Kaporetto.DbConnector;
using Kaporetto.DbConnector.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

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




await host.RunAsync();