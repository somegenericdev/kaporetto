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
    public ImageboardEngine ImageboardEngine { get; set; }
    
    public string ImgBaseUrl
    {
        get => GetImgBaseUrl();
    }

    public string ThreadBaseUrl
    {
        get => GetThreadBaseUrl();
    }


    private string GetImgBaseUrl()
    {
        switch (ImageboardEngine)
        {
            case ImageboardEngine.Lynxchan:
            {
                return  new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
            }
            case ImageboardEngine.Vichan:
            {
                return BaseUrl;
            }
            default:
                throw new Exception("Engine not handled.");
        }
    }
    private string GetThreadBaseUrl()
    {
        switch (ImageboardEngine)
        {
            case ImageboardEngine.Lynxchan:
            {
                return  new Uri(BaseUrl).Combine("res").ToString();
            }
            case ImageboardEngine.Vichan:
            {
                return new Uri(BaseUrl).Combine("thread").ToString();
            }
            default:
                throw new Exception("Engine not handled.");
        }
    }
}

public enum ImageboardEngine
{
    Lynxchan, Vichan
}