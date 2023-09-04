using Kaporetto.Models;

namespace Kaporetto.Scraper;

public static class Globals
{
    public static int MaxFileSize { get; set; }
    public static string BaseUrl { get; set; }
    public static string ImgBaseUrl
    {
        get => new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
    }

    public static string ThreadBaseUrl
    {
        get => new Uri(BaseUrl).Combine("res").ToString();
    }
}