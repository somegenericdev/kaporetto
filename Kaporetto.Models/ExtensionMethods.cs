using System.Collections.Immutable;

namespace Kaporetto.Models;

public static class ExtensionMethods
{
    public static Uri Combine(this Uri uri, params string[] paths)
    {
        return new Uri(paths.Aggregate(uri.AbsoluteUri, (current, path) => string.Format("{0}/{1}", current.TrimEnd('/'), path.TrimStart('/'))));
    }
    public static ImmutableList<T> MergeAlternately<T>(this ImmutableList<ImmutableList<T>> lists, ImmutableList<T> acc=null)
    {
        if (lists.All(x => x.Count == 0))
        {
            return acc==null ? new List<T>().ToImmutableList() : acc;
        }


        var getOneFromEach = ()=>lists.Select(l => l.FirstOrDefault()).Where(x => x != null);
        
        var newAcc= (acc==null ? getOneFromEach() : acc.AddRange(getOneFromEach())).ToImmutableList();

        var newLists = lists.Select(x =>
        {
            return (x.Count > 0) ? x.RemoveAt(0) : x;
        }).ToImmutableList();

        return MergeAlternately(newLists, newAcc);

    }
}