using Serilog.Sinks.Loki;
using Serilog.Sinks.Loki.Labels;

namespace Kaporetto.Models;

public class LogLabelProvider : ILogLabelProvider
{
    public IList<LokiLabel> GetLabels()
    {
        return new List<LokiLabel>
        {
            new LokiLabel("app", AppName),
        };
    }

    private string AppName;
    public LogLabelProvider(string appName)
    {
        AppName = appName;
    }
    
    public IList<string> PropertiesAsLabels { get; set; } = new List<string>
    {
        "level"
    };
    public IList<string> PropertiesToAppend { get; set; } = new List<string>
    {

    };
    public LokiFormatterStrategy FormatterStrategy { get; set; } = LokiFormatterStrategy.SpecificPropertiesAsLabelsOrAppended;
}
