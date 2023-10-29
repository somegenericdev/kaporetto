using System.Collections.Immutable;
using Akka.DependencyInjection;
using Akka.Hosting;
using Kaporetto.Models;
using Kaporetto.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace Kaporetto.Akka.NET;

public class SchedulerActor : ReceiveActor, IWithTimers
{
    // private readonly IActorRef ThreadScraperActor;
    private YamlConfig YamlConfig;
    private ILogger Logger;
    private IServiceProvider ServiceProvider;
    private ImmutableList<IActorRef> Children = new List<IActorRef>().ToImmutableList();
    public SchedulerActor(YamlConfig yamlConfig, ILogger logger,IServiceProvider serviceProvider)
    {
        YamlConfig = yamlConfig;
        Logger = logger;
        ServiceProvider = serviceProvider;
        // ThreadScraperActor = helloActor.ActorRef;
        Receive<string>(message =>
        {
            if (AreChildrenDead())
            {
                var act = (string x) => OnReceiveMessage(x);
                (act, message, Logger).TryOrLog();
            }
        });
    }


    private void OnReceiveMessage(string s)
    {
        
        var dbactor=ServiceProvider.GetService<IRequiredActor<DbActor>>();
        var yaml = ServiceProvider.GetService<YamlConfig>();
        var logger = ServiceProvider.GetService<ILogger>();
        
        
        
        var boardProcessors = YamlConfig.Boards.Select(b => new BoardProcessor(b)).ToImmutableList();

        var bumpedThreadsTmp = boardProcessors.Select(boardProcessor =>
        {
            return boardProcessor.GetBumpedThreads().Select(threadNo => $"{threadNo};{boardProcessor.board.Alias}")
                .ToImmutableList();
        }).ToImmutableList();

        var bumpedThreadsFlat = bumpedThreadsTmp.MergeAlternately();
        bumpedThreadsFlat.ForEach(x=>
        {
            IActorRef child=Context.ActorOf(Props.Create<ThreadScraperActor>(dbactor, yaml, logger), $"threadScraper-{x}");
            Children=Children.Add(child);
            child.Tell(x);
        });
        
        Logger.Information("Before saving LastFetched board");
        boardProcessors.ForEach(boardProcessor => boardProcessor.SaveLastFetched());
    }

    private bool AreChildrenDead()
    {
        Children=Children.Where(x => !(x as LocalActorRef).IsTerminated).ToImmutableList();
        return Children.IsEmpty;
    }

    
    

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("hello-key", "hello", TimeSpan.FromSeconds(20));
    }

    public ITimerScheduler Timers { get; set; } = null!; // gets set by Akka.NET
}

public class CurrentThreadNoMessage
{
    
}