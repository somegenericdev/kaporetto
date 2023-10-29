namespace Kaporetto.Models.Vichan;

public class VichanCatalogThread
{
    public long no { get; set; }
    public string com { get; set; }
    public string name { get; set; }
    public long time { get; set; }
    public int omitted_posts { get; set; }
    public int omitted_images { get; set; }
    public int replies { get; set; }
    public int images { get; set; }
    public bool sticky { get; set; }
    public bool locked { get; set; }
    public bool cyclical { get; set; }
    public long last_modified { get; set; }
    public string country { get; set; }
    public string country_name { get; set; }
    public int tn_h { get; set; }
    public int tn_w { get; set; }
    public int h { get; set; }
    public int w { get; set; }
    public int fsize { get; set; }
    public string ext { get; set; }
    public string tim { get; set; }
    public string filename { get; set; }
    public string md5 { get; set; }
    public long resto { get; set; }
}