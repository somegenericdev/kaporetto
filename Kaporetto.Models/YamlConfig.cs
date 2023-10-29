using System.Collections.Immutable;

namespace Kaporetto.Models;

public class YamlConfig
{
    public int MaxDegreeOfParallelism { get; set; }
    public int MaxFileSize { get; set; }
    public string ScraperPath { get; set; }
    public string QueueName { get; set; }
    public string PostgreConnectionString { get; set; }
    public string LokiUrl { get; set; }
    public List<Board> Boards { get; set; }

    public Board GetBoard(string alias)
    {
        return Boards.Single(x => x.Alias == alias);
    }
}

public class Board
{
    public string BaseUrl { get; set; }
    public string Alias { get; set; }
    
    public string ImgBaseUrl
    {
        get => new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
    }

    public string ThreadBaseUrl
    {
        get => new Uri(BaseUrl).Combine("res").ToString();
    }
}