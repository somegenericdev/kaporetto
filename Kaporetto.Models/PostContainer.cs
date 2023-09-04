using System.Collections.Immutable;

namespace Kaporetto.Models;

public class PostContainer
{
    
    public string name { get; set; }
    public string signedRole { get; set; }
    public string email { get; set; }
    public string flag { get; set; }
    public string flagName { get; set; }
    public string id { get; set; }
    public string subject { get; set; }
    public string flagCode { get; set; }
    public string markdown { get; set; }
    public string message { get; set; }
    public long threadId { get; set; }
    public DateTime creation { get; set; }
    
    public ImmutableList<Post> Posts { get; set; }
    public ImmutableList<File> files { get; set; }



}
