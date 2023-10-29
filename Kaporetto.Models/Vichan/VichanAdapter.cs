using System.Collections.Immutable;
using Scheduler;

namespace Kaporetto.Models.Vichan;

public class VichanAdapter
{
    public static CatalogThread AdaptCatalogThread(VichanCatalogThread vichanCatalogThread)
    {
        var catalogThread = new CatalogThread();
        catalogThread.threadId= vichanCatalogThread.no;
        catalogThread.message = vichanCatalogThread.com;
        catalogThread.creation= DateTimeOffset.FromUnixTimeSeconds( vichanCatalogThread.time).UtcDateTime;
        catalogThread.postCount = vichanCatalogThread.replies;
        catalogThread.fileCount = vichanCatalogThread.images;
        catalogThread.pinned = vichanCatalogThread.sticky;
        catalogThread.locked = vichanCatalogThread.locked;
        catalogThread.cyclic = vichanCatalogThread.cyclical;
        catalogThread.lastBump = DateTimeOffset.FromUnixTimeSeconds(vichanCatalogThread.last_modified).UtcDateTime;
        catalogThread.flagCode = vichanCatalogThread.country;
        catalogThread.flagName = vichanCatalogThread.country_name;
        return catalogThread;
    }

    public static Post AdaptPost(VichanPost vichanPost)
    {
        var post=new Post();
        post.postId = vichanPost.no;
        post.parentId = vichanPost.resto;
        post.message = vichanPost.com;
        post.email = vichanPost.email;
        post.name = vichanPost.name;
        post.creation = DateTimeOffset.FromUnixTimeSeconds(vichanPost.time).UtcDateTime;
        post.flagCode = vichanPost.country;
        post.flagName = vichanPost.country_name;
        return post;
    }

    public static PostContainer AdaptPostContainer(VichanPostContainer vichanPostContainer)
    {
        var first = vichanPostContainer.Posts.First();
        var rest = vichanPostContainer.Posts.TakeWhile((x, idx) => idx > 0);

        var postContainer=new PostContainer();
        postContainer.threadId = first.no;
        postContainer.name = first.name;
        postContainer.message = first.com;
        postContainer.email = first.email;
        postContainer.creation=DateTimeOffset.FromUnixTimeSeconds(first.time).UtcDateTime;
        postContainer.flagCode = first.country;
        postContainer.flagName = first.country_name;
        
        postContainer.Posts=rest.Select(p => VichanAdapter.AdaptPost(p)).ToImmutableList();
        return postContainer;
    }
        
}