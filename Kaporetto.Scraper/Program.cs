
using Kaporetto.Scraper;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using Serilog;
using Kaporetto.Models;
using System.Linq;
using System.Net;
using Serilog.Sinks.Loki;
using YamlDotNet.Serialization;
using File = System.IO.File;

#region SIGNAL handling, logger init and other boring stuff





bool wasSigTermReceived = false;

PosixSignalRegistration.Create(PosixSignal.SIGTERM, (ctx) =>
{
    ctx.Cancel = true;
    wasSigTermReceived = true;
});

#endregion


// string threadNo = "21017308";
// string boardAlias = "int";
string threadNo = args[0].Split(";")[0];
string boardAlias = args[0].Split(";")[1];

string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string lastFetchedPath = Path.Join(homeDirectory, ".kaporetto", $"{threadNo}-{boardAlias}.tmp");

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
var channel = connection.CreateModel();

string configPath = Path.Join(homeDirectory, ".kaporetto", ".kaporettorc");

var yamlConfig = new Deserializer().Deserialize<YamlConfig>(File.ReadAllText(configPath));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.LokiHttp(new NoAuthCredentials(yamlConfig.LokiUrl),new LogLabelProvider("Scraper"))
    .WriteTo.Console()
    .CreateLogger();

Globals.MaxFileSize = yamlConfig.MaxFileSize;
Globals.BaseUrl = yamlConfig.Boards.Single(x => x.Alias == boardAlias).BaseUrl;

long lastPostNo = File.Exists(lastFetchedPath) ? long.Parse(File.ReadAllText(lastFetchedPath)) : 0;


var threadUrl = new Uri(Globals.ThreadBaseUrl).Combine($"{threadNo}.json").ToString();
var result = Utils.GetRequest(threadUrl);
if (result.statusCode == HttpStatusCode.NotFound)
{
    Environment.Exit(0);
}

var postContainer = JsonConvert.DeserializeObject<PostContainer>(result.response);
var thread = new Post(postContainer); //push the thread first


if (lastPostNo == 0)
{
    RabbitMqPush(Utils.NormalizePost(thread, null, boardAlias));
}


try
{
    ImmutableList<Post> posts = postContainer.Posts;

    if (posts.Count > 0 && posts.Last()?.postId > lastPostNo)
    {
        List<Post> postsToBePushed = posts.Where(p => p.postId > lastPostNo).ToList();

        postsToBePushed.ForEach(p => { RabbitMqPush(Utils.NormalizePost(p, long.Parse(threadNo),boardAlias)); });
    }

    lastPostNo = posts.LastOrDefault()?.postId ?? thread.postId;

    if (!File.Exists(lastFetchedPath))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(lastFetchedPath));
    }

    File.WriteAllText(lastFetchedPath, lastPostNo.ToString());
    channel.Dispose();

    if (wasSigTermReceived)
    {
        Environment.Exit(0);
    }
}
catch (Exception e)
{

    Log.Logger.Error($"[{DateTime.Now.ToString()}] {e.Message} {e.Source} {e.TargetSite} {e.StackTrace}");
}


void RabbitMqPush(Post p)
{
    try
    {
        var json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(p));
        Log.Logger.Information(
            $"[{DateTime.Now.ToString()}] [{boardAlias}] Pushing post no. {p.postId} in thread no. {p.parentId} to the queue. Approx. size: {json.Length / 1000 / 1000}MB");
        
        IBasicProperties basicProperties = channel.CreateBasicProperties();
        basicProperties.Persistent = true;
        channel.BasicPublish(exchange: string.Empty,
            routingKey: "Posts", //queue name
            basicProperties: basicProperties,
            body: json);
    }
    catch (Exception e)
    {
        Log.Logger.Error(
            $"[{boardAlias}] Error while pushing post no. {p.postId} in thread no. {p.parentId} to the queue.\n${e.Message}");
        channel.Dispose();
        channel = connection.CreateModel();
    }
}