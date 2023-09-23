using System.Collections.Immutable;

namespace Kaporetto.Models;

public class YamlConfig
{
    public int maxDegreeOfParallelism { get; set; }
    public int maxFileSize { get; set; }
    public string scraperPath { get; set; }
    public string queueName { get; set; }
    public string postgreConnectionString { get; set; }
    public string lokiUrl { get; set; }
    public List<Board> boards { get; set; }
}

public class Board
{
    public string baseUrl { get; set; }
    public string alias { get; set; }
}