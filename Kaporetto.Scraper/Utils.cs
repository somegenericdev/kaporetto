using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Polly;
using Polly.Extensions.Http;
using Kaporetto.Models;
using File = Kaporetto.Models.File;
using IOFile = System.IO.File;

namespace Kaporetto.Scraper;

public class Utils
{
    public static Post NormalizePost(Post post, long? parentId, string boardAlias)
    {
        var newFiles = post.files.Select(f => NormalizeFile(f, Globals.MaxFileSize)).ToImmutableList();

        return new Post(post.name, post.signedRole, post.email, post.flag, post.flagName, post.id, post.subject,
            post.flagCode, post.markdown, post.message, post.postId, post.creation, newFiles, post.isThread, parentId,boardAlias);
    }
    
    public static File NormalizeFile(File file, int maxFileSize)
    {
        var path_headers = HeadRequest(new Uri(Globals.ImgBaseUrl).Combine(file.path).ToString(), 1000).response.Content.Headers;
        var size = long.Parse(path_headers.GetValues("Content-Length").FirstOrDefault());

        byte[] thumb=new byte[]{};
        byte[] content=new byte[]{};

        if (size < maxFileSize * 1000 * 1000) //5MB
        {
            content = GetRequestBytes(new Uri(Globals.ImgBaseUrl).Combine(file.path).ToString(), 1000).response;
        }

        thumb = GetRequestBytes(new Uri(Globals.ImgBaseUrl).Combine(file.thumb).ToString()).response;

        var fileContent=new FileContent(content, thumb, Utils.ComputeSha256Hash(content));

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
        return (ms.ToArray(),response.StatusCode);
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