using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Kaporetto.Models;
using Kaporetto.Scheduler;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using Scheduler;
using Serilog;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using File = System.IO.File;

string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string configPath = Path.Join(homeDirectory, ".kaporetto", ".kaporettorc");
var config = new Deserializer().Deserialize<YamlConfig>(File.ReadAllText(configPath));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.LokiHttp(new NoAuthCredentials(config.lokiUrl),new LogLabelProvider("Scraper"))
    .WriteTo.Console()
    .CreateLogger();

try
{
    Globals.scraperPath = config.scraperPath;
    SchedulerWorker schedulerWorker = new SchedulerWorker();

    while (true)
    {

        var boardProcessors = config.boards.Select(b => new BoardProcessor(b)).ToImmutableList();

        var bumpedThreadsTmp = boardProcessors.Select(boardProcessor =>
        {
            return boardProcessor.GetBumpedThreads().Select(threadNo => $"{threadNo};{boardProcessor.board.alias}")
                .ToImmutableList();
        }).ToImmutableList();

        var bumpedThreadsFlat = bumpedThreadsTmp.MergeAlternately();

        schedulerWorker.ScheduleProcesses(bumpedThreadsFlat);
        boardProcessors.ForEach(boardProcessor => boardProcessor.SaveLastFetched());
    }
}
catch (Exception e)
{
    Log.Logger.Error(e.ToString());
}

