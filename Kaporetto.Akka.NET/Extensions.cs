using Kaporetto.Models;
using Serilog;
using Serilog.Core;

namespace Kaporetto.Akka.NET;

public static class Extensions
{
    
    public static void TryOrLog<T>(this (Action<T> act, T post, ILogger Logger) tuple )
    {
        try
        {
            tuple.Item1(tuple.Item2);
        }
        catch (Exception e)
        {
            tuple.Item3.Error(e.ToString());
        }
    }
    public static void Try<T>(this (Action<T> act, T post, ILogger Logger) tuple )
    {
         tuple.Item1(tuple.Item2);
    }

    
}