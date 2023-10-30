using System.Collections.Immutable;
using Kaporetto.Models;
using Kaporetto.Models.Adapters;
using Kaporetto.Models.Vichan;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Extensions.Http;
using Scheduler;
using File = System.IO.File;

namespace Kaporetto.Scheduler;

public class BoardProcessor
{
    public Board Board;

    private string CatalogUrl
    {
        get => new Uri(Board.BaseUrl).Combine("catalog.json").ToString();
    }

    private string LastFetchedPath
    {
        get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kaporetto",
            $"lastFetched-{Board.Alias}.tmp");
    }


    private string Json;

    //TODO cambia nome?
    public ImmutableList<long> GetBumpedThreads()
    {
        var oldThreads = File.Exists(LastFetchedPath)
            ? JsonConvert.DeserializeObject<ImmutableList<CatalogThread>>(File.ReadAllText(LastFetchedPath))
            : new List<CatalogThread>().ToImmutableList();

        Json = GetRequest(CatalogUrl);


        var newThreads = Adapt(Json);
        var diffThreads = diff(oldThreads, newThreads);
        return diffThreads;
    }


    private ImmutableList<CatalogThread> Adapt(string json)
    {
        switch (Board.ImageboardEngine)
        {
            case ImageboardEngine.Lynxchan:
            {
                var adapter = new NullAdapter();
                var catalogThread = JsonConvert.DeserializeObject<ImmutableList<CatalogThread>>(json);
                return catalogThread.Select(x => adapter.AdaptCatalogThread(x)).ToImmutableList();
            }
            case ImageboardEngine.Vichan:
            {
                var adapter = new VichanAdapter();
                var flattenedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<ImmutableList<JObject>>(json).SelectMany(x => x["threads"]));
                var vichanCatalogThread = JsonConvert.DeserializeObject<ImmutableList<VichanCatalogThread>>(flattenedJson);
                return vichanCatalogThread.Select(x => adapter.AdaptCatalogThread(x)).ToImmutableList();
            }
            default:
                throw new Exception("Engine not handled.");
        }
    }


    public void SaveLastFetched()
    {
        if (!File.Exists(LastFetchedPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LastFetchedPath));
        }

        File.WriteAllText(LastFetchedPath, Json);
    }

    private string GetRequest(string url)
    {
        //TODO fallback
        var policy = Policy<HttpResponseMessage>.Handle<HttpRequestException>().OrTransientHttpStatusCode()
            .Or<TaskCanceledException>().Or<TimeoutException>()
            .WaitAndRetry(int.MaxValue, (_) => TimeSpan.FromSeconds(5));
        var response = policy.Execute(() =>
        {
            var client = new HttpClient();
            var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
            var response = client.Send(webRequest);
            return response;
        });

        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }

    private ImmutableList<long> diff(ImmutableList<CatalogThread> oldThreads, ImmutableList<CatalogThread> newThreads)
    {
        var newlyCreatedThreads = newThreads.Where(n => !oldThreads.Select(old => old.threadId).Contains(n.threadId))
            .Select(x => x.threadId);
        var bumpedThreads = newThreads.Select(newElem =>
            {
                var oldElem = oldThreads.FirstOrDefault(old => old.threadId == newElem.threadId);
                if (oldElem == null)
                {
                    return null;
                }

                return new Tuple<CatalogThread, CatalogThread>(oldElem, newElem);
            }
        ).Where(x => x != null && x.Item2.lastBump > x.Item1.lastBump).Select(x => x.Item2).Select(x => x.threadId);
        return newlyCreatedThreads.Concat(bumpedThreads).ToImmutableList();
    }


    public BoardProcessor(Board _board)
    {
        Board = _board;
    }
}