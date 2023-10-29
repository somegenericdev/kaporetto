using System.Collections.Immutable;
using Akka.Hosting;
using Kaporetto.Models;
using Kaporetto.Scheduler;
using Serilog;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace akka.App;

public class SchedulerActor : ReceiveActor, IWithTimers
{
    private readonly IActorRef ThreadScraperActor;
    private YamlConfig YamlConfig;
    public SchedulerActor(IRequiredActor<ThreadScraperActor> helloActor, YamlConfig yamlConfig)
    {
        YamlConfig = yamlConfig;
        
        ThreadScraperActor = helloActor.ActorRef;
        Receive<string>(message =>
        {

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.LokiHttp(new NoAuthCredentials(YamlConfig.LokiUrl),new LogLabelProvider("Scraper"))
                .WriteTo.Console()
                .CreateLogger();
            
            var boardProcessors = YamlConfig.Boards.Select(b => new BoardProcessor(b)).ToImmutableList();

            var bumpedThreadsTmp = boardProcessors.Select(boardProcessor =>
            {
                return boardProcessor.GetBumpedThreads().Select(threadNo => $"{threadNo};{boardProcessor.board.Alias}")
                    .ToImmutableList();
            }).ToImmutableList();

            var bumpedThreadsFlat = bumpedThreadsTmp.MergeAlternately();
            bumpedThreadsFlat.ForEach(x=>ThreadScraperActor.Tell(x));
        });
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("hello-key", "hello", TimeSpan.FromSeconds(20));
    }

    public ITimerScheduler Timers { get; set; } = null!; // gets set by Akka.NET
}