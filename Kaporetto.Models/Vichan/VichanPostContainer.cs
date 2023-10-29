using System.Collections.Immutable;

namespace Kaporetto.Models.Vichan;

public class VichanPostContainer
{
    public ImmutableList<VichanPost> Posts { get; set; }
}