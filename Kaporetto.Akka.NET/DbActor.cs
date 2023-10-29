using System.Collections.Immutable;
using System.Text;
using Akka.Hosting;
using Kaporetto.Models;
using Kaporetto.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace akka.App;

public class DbActor : ReceiveActor
{
    private readonly IActorRef ThreadScraperActor;
    private YamlConfig YamlConfig;
    public DbActor(IRequiredActor<ThreadScraperActor> helloActor, IServiceProvider serviceProvided, IServiceScopeFactory serviceScopeFactory, YamlConfig yamlConfig)
    {
        YamlConfig = yamlConfig;
        
        ThreadScraperActor = helloActor.ActorRef;
        Receive<Post>(post =>
        {

            using (var serviceScope = serviceScopeFactory.CreateScope())
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

        });
    }

}