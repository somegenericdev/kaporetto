using System.Collections.Immutable;

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
    public string tim { get; set; }
    public int fsize { get; set; }
    public bool cyclical { get; set; }
    public long last_modified { get; set; }
    public string country { get; set; }
    public string md5 { get; set; }
    public string ext { get; set; }
    public string country_name { get; set; }
    public string filename { get; set; }
    public int tn_w { get; set; }
    public int tn_h { get; set; }
    public int w { get; set; }
    public int h { get; set; }
    public ImmutableList<VichanFile> extra_files = new List<VichanFile>().ToImmutableList();
}

public class VichanFile
{
    public int tn_h { get; set; }
    public int tn_w { get; set; }
    public int h { get; set; }
    public int w { get; set; }
    public int fsize { get; set; }
    public string ext { get; set; }
    public string tim { get; set; }
    public string filename { get; set; }
    public string md5 { get; set; }

    public string path
    {
        get => $"/src/{tim}{ext}";
    }

    public string thumb
    {
        get => path.Replace("src", "thumb");
    }
}