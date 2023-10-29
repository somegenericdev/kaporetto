using System.Collections.Immutable;
using Kaporetto.Models;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using Scheduler;
using File = System.IO.File;

namespace Kaporetto.Scheduler;

public class BoardProcessor
{
    public Board board;

    private string catalogUrl
    {
        get => new Uri(board.BaseUrl).Combine("catalog.json").ToString();
    }

    private string lastFetchedPath
    {
        get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kaporetto", $"lastFetched-{board.Alias}.tmp");
    }

    private string json;

    //TODO cambia nome?
    public ImmutableList<long> GetBumpedThreads()
    {
        var oldThreads = File.Exists(lastFetchedPath)
            ? JsonConvert.DeserializeObject<ImmutableList<CatalogThread>>(File.ReadAllText(lastFetchedPath))
            : new List<CatalogThread>().ToImmutableList();

        json = GetRequest(catalogUrl);
        var newThreads = JsonConvert.DeserializeObject<ImmutableList<CatalogThread>>(this.json);
        var diffThreads = diff(oldThreads, newThreads);
        return diffThreads;
    }

    public void SaveLastFetched()
    {
        if (!File.Exists(lastFetchedPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lastFetchedPath));
        }

        File.WriteAllText(lastFetchedPath, json);
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
        board = _board;
    }
}