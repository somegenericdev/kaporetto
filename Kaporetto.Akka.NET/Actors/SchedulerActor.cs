using System.Collections.Immutable;
using Akka.Hosting;
using Kaporetto.Models;
using Kaporetto.Scheduler;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace Kaporetto.Akka.NET;

public class SchedulerActor : ReceiveActor, IWithTimers
{
    private readonly IActorRef ThreadScraperActor;
    private YamlConfig YamlConfig;
    private ILogger Logger;
    public SchedulerActor(IRequiredActor<ThreadScraperActor> helloActor, YamlConfig yamlConfig, ILogger logger)
    {
        YamlConfig = yamlConfig;
        Logger = logger;
        ThreadScraperActor = helloActor.ActorRef;
        Receive<string>(message =>
        {
            var act = (string x) => OnReceiveMessage(x);
            (act, message, Logger).TryOrLog();
        });
    }


    public void OnReceiveMessage(string s)
    {
        var boardProcessors = YamlConfig.Boards.Select(b => new BoardProcessor(b)).ToImmutableList();

        var bumpedThreadsTmp = boardProcessors.Select(boardProcessor =>
        {
            return boardProcessor.GetBumpedThreads().Select(threadNo => $"{threadNo};{boardProcessor.board.Alias}")
                .ToImmutableList();
        }).ToImmutableList();

        var bumpedThreadsFlat = bumpedThreadsTmp.MergeAlternately();
        bumpedThreadsFlat.ForEach(x=>ThreadScraperActor.Tell(x));
    }


    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("hello-key", "hello", TimeSpan.FromSeconds(20));
    }

    public ITimerScheduler Timers { get; set; } = null!; // gets set by Akka.NET
}