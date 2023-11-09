using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Akka.Hosting;
using Kaporetto.Models;
using Kaporetto.Models.Adapters;
using Kaporetto.Models.Vichan;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Loki;
using File = Kaporetto.Models.File;

namespace Kaporetto.Akka.NET;

public class ThreadScraperActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private int _helloCounter = 0;
    private YamlConfig YamlConfig;
    private string ThreadNo;
    private string BoardAlias;
    private ILogger Logger;
    private IRequiredActor<DbActor> DbActor;

    public ThreadScraperActor(IRequiredActor<DbActor> dbActor, YamlConfig yamlConfig, ILogger logger)
    {
        YamlConfig = yamlConfig;
        Logger = logger;
        DbActor = dbActor;

        Receive<string>(message =>
        {
            var act = (string x) => OnReceiveMessage(x);
            (act, message, Logger).TryOrLog();
        });
        
        Receive<CurrentThreadNoMessage>((message)=> Sender.Tell(ThreadNo)); //not used
    }

    public void OnReceiveMessage(string message)
    {
        
        
        
        ThreadNo = message.Split(";")[0];
        BoardAlias = message.Split(";")[1];
        
        Logger.Information("Scraping thread no. {ThreadNo} on board {BoardAlias}",ThreadNo, BoardAlias);

        var baseUrl = GetBaseUrl(BoardAlias);
        var lastFetchedPath = GetLastFetchedPath(ThreadNo, BoardAlias);
        Logger.Information(lastFetchedPath);
        
        long lastPostNo = System.IO.File.Exists(lastFetchedPath)
            ? long.Parse(System.IO.File.ReadAllText(lastFetchedPath))
            : 0;


        var threadUrl = new Uri(baseUrl).Combine($"{ThreadNo}.json").ToString();
        var result = GetRequest(threadUrl);
        if (result.statusCode == HttpStatusCode.NotFound)
        {
            Context.Stop(Self);
        }

        var postContainer = GetPostContainer(result.response);
        var thread = new Post(postContainer); //push the thread first


        if (lastPostNo == 0)
        {
            var normalized = NormalizePost(thread, null, BoardAlias);
            DbActor.ActorRef.Tell(normalized);
        }


        ImmutableList<Post> posts = postContainer.Posts;

        if (posts.Count > 0 && posts.Last()?.postId > lastPostNo)
        {
            List<Post> postsToBePushed = posts.Where(p => p.postId > lastPostNo).ToList();

            postsToBePushed.ForEach(p =>
            {
                var normalized = NormalizePost(p, long.Parse(ThreadNo), BoardAlias);
                DbActor.ActorRef.Tell(normalized);
            });
        }
        Logger.Information("Out of Foreach");
        lastPostNo = posts.LastOrDefault()?.postId ?? thread.postId;

        if (!System.IO.File.Exists(lastFetchedPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lastFetchedPath));
        }
        Logger.Information("Before writealltext");
        System.IO.File.WriteAllText(lastFetchedPath, lastPostNo.ToString());
        Context.Stop(Self);
    }


    public PostContainer GetPostContainer(string json)
    {
        switch (YamlConfig.GetBoard(BoardAlias).ImageboardEngine)
        {
            case ImageboardEngine.Lynxchan:
            {
                var adapter = new NullAdapter();
                var postContainer = JsonConvert.DeserializeObject<PostContainer>(json);
                return adapter.AdaptPostContainer(postContainer);
            }
            case ImageboardEngine.Vichan:
            {
                var adapter = new VichanAdapter();
                var postContainer = JsonConvert.DeserializeObject<VichanPostContainer>(json);
                return adapter.AdaptPostContainer(postContainer);
            }
            default:
                throw new Exception("Engine not handled.");
        }

        
    }

    public string GetBaseUrl(string boardAlias)
    {
        return YamlConfig.GetBoard(boardAlias).ThreadBaseUrl;
    }

    public string GetLastFetchedPath(string threadNo, string boardAlias)
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Join(homeDirectory, ".kaporetto", $"{threadNo}-{boardAlias}.tmp");
    }


    public Post NormalizePost(Post post, long? parentId, string boardAlias)
    {
        var newFiles = post.files.Select(f => NormalizeFile(f, YamlConfig.MaxFileSize)).ToImmutableList();

        return new Post(post.name, post.signedRole, post.email, post.flag, post.flagName, post.id, post.subject,
            post.flagCode, post.markdown, post.message, post.postId, post.creation, newFiles, post.isThread, parentId,
            boardAlias);
    }

    public File NormalizeFile(File file, int maxFileSize)
    {
        var path_headers =
            HeadRequest(new Uri(YamlConfig.GetBoard(BoardAlias).ImgBaseUrl).Combine(file.path).ToString(), 1000)
                .response.Content.Headers;
        var size = long.Parse(path_headers.GetValues("Content-Length").FirstOrDefault());

        byte[] thumb = new byte[] { };
        byte[] content = new byte[] { };

        if (size < maxFileSize * 1000 * 1000) //5MB
        {
            content = GetRequestBytes(new Uri(YamlConfig.GetBoard(BoardAlias).ImgBaseUrl).Combine(file.path).ToString(),
                1000).response;
        }

        thumb = GetRequestBytes(new Uri(YamlConfig.GetBoard(BoardAlias).ImgBaseUrl).Combine(file.thumb).ToString())
            .response;

        var fileContent = new FileContent(content, thumb, ComputeSha256Hash(content));

        return new File(file.size, file.width, file.height, file.mime, file.thumb, file.path, fileContent);
    }


    public static (string response, HttpStatusCode statusCode) GetRequest(string url, int secondsTimeout = 100)
    {
        var policy = HttpPolicyExtensions
            .HandleTransientHttpError().Or<TaskCanceledException>().Or<TimeoutException>()
            .WaitAndRetry(50, (_) => TimeSpan.FromSeconds(5));
        var response = policy.Execute(() =>
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(secondsTimeout);
            var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
            var response = client.Send(webRequest);
            return response;
        });

        using var reader = new StreamReader(response.Content.ReadAsStream());
        return (reader.ReadToEnd(), response.StatusCode);
    }

    public static (byte[] response, HttpStatusCode statusCode) GetRequestBytes(string url, int secondsTimeout = 100)
    {
        var policy = HttpPolicyExtensions
            .HandleTransientHttpError().Or<TaskCanceledException>().Or<TimeoutException>()
            .WaitAndRetry(50, (_) => TimeSpan.FromSeconds(5));
        var response = policy.Execute(() =>
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(secondsTimeout);
            var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
            var response = client.Send(webRequest);
            return response;
        });


        MemoryStream ms = new MemoryStream();
        response.Content.ReadAsStream().CopyTo(ms);
        return (ms.ToArray(), response.StatusCode);
    }


    public static (HttpResponseMessage response, HttpStatusCode statusCode) HeadRequest(string url,
        int secondsTimeout = 100)
    {
        var policy = HttpPolicyExtensions
            .HandleTransientHttpError().Or<TaskCanceledException>().Or<TimeoutException>()
            .WaitAndRetry(50, (_) => TimeSpan.FromSeconds(5));
        var response = policy.Execute(() =>
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(secondsTimeout);
            var webRequest = new HttpRequestMessage(HttpMethod.Head, url);
            var response = client.Send(webRequest);
            return response;
        });
        return (response, response.StatusCode);
    }

    public static string ComputeSha256Hash(byte[] data)
    {
        // Create a SHA256
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(data);

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}