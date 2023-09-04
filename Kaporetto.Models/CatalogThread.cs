namespace Scheduler;

public class CatalogThread
{
    public string message { get; set; }
    public string markdown { get; set; }
    public long threadId { get; set; }
    public int postCount { get; set; }
    public int fileCount { get; set; }
    public int page { get; set; }
    public string subject { get; set; }
    public bool locked { get; set; }
    public bool pinned { get; set; }
    public bool cyclic { get; set; }
    public bool autoSage { get; set; }
    public DateTime lastBump { get; set; }
    public DateTime creation { get; set; }
    public string flag { get; set; }
    public string flagName { get; set; }
    public string flagCode { get; set; }
    // public File[] files { get; set; }
    public string thumb { get; set; }
}