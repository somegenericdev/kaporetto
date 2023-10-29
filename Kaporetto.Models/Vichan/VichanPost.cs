namespace Kaporetto.Models.Vichan;

public class VichanPost
{
    public long no { get; set; }
    public long resto { get; set; }
    public string com { get; set; }
    public string email { get; set; }
    public string name { get; set; }
    public long time { get; set; }
    public bool sticky { get; set; }
    public bool locked { get; set; }
    public bool cyclical { get; set; }
    public long last_modified { get; set; }
    public string country { get; set; }
    public string country_name { get; set; }
}