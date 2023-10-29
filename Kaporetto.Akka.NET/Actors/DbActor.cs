using System.Collections.Immutable;
using System.Text;
using Akka.Hosting;
using Kaporetto.Models;
using Kaporetto.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace Kaporetto.Akka.NET;

public class DbActor : ReceiveActor
{
    private readonly IActorRef ThreadScraperActor;
    private YamlConfig YamlConfig;
    private ILogger Logger;
    private IServiceScopeFactory ServiceScopeFactory;
    
    public DbActor(IRequiredActor<ThreadScraperActor> helloActor, IServiceProvider serviceProvided, IServiceScopeFactory serviceScopeFactory, YamlConfig yamlConfig, ILogger logger)
    {
        YamlConfig = yamlConfig;
        ThreadScraperActor = helloActor.ActorRef;
        ServiceScopeFactory = serviceScopeFactory;
        Logger = logger;
        
        
        Receive<Post>(post =>
        {
            var act = (Post x) => OnReceiveMessage(x);
            (act, post, Logger).Try();
        });
    }

    public void OnReceiveMessage(Post post)
    {
        using (var serviceScope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetService<KaporettoContext>();




            post.files.ForEach(file =>
            {
                var fileContent = dbContext.FileContents.FirstOrDefault(f => f.sha256 == file.FileContent.sha256);
                if (fileContent != null)
                {
                    file.FileContent = fileContent;
                }
            });
            dbContext.Posts.Add(post);
            dbContext.SaveChanges();
            Log.Logger.Information($"[{DateTime.Now.ToString()}] Saved and acked post no. {post.postId}");
            // dbContext.ChangeTracker.Clear();
        }
    }

 
    
}
