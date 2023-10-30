using System.Collections.Immutable;
using Kaporetto.Models.Vichan;
using Scheduler;

namespace Kaporetto.Models.Adapters;

public class VichanAdapter : IAdapter<VichanCatalogThread, VichanPost, VichanPostContainer>
{
    public CatalogThread AdaptCatalogThread(VichanCatalogThread vichanCatalogThread)
    {
        var catalogThread = new CatalogThread();
        catalogThread.threadId = vichanCatalogThread.no;
        catalogThread.message = vichanCatalogThread.com ?? "";
        catalogThread.creation = DateTimeOffset.FromUnixTimeSeconds(vichanCatalogThread.time).UtcDateTime;
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

    public Post AdaptPost(VichanPost vichanPost)
    {
        var post = new Post();
        post.postId = vichanPost.no;
        post.parentId = vichanPost.resto;
        post.message = vichanPost.com ?? "";
        post.email = vichanPost.email;
        post.name = vichanPost.name;
        post.creation = DateTimeOffset.FromUnixTimeSeconds(vichanPost.time).UtcDateTime;
        post.flagCode = vichanPost.country;
        post.flagName = vichanPost.country_name;

        if (vichanPost.filename != null)
        {
            var file = new VichanFile();
            file.filename = vichanPost.filename;
            file.tim = vichanPost.tim;
            file.md5 = vichanPost.md5;
            file.ext = vichanPost.ext;
            file.fsize = vichanPost.fsize;
            file.w = vichanPost.w;
            file.h = vichanPost.h;
            file.tn_h = vichanPost.tn_h;
            file.tn_w = vichanPost.tn_w;
            vichanPost.extra_files=vichanPost.extra_files.Add(file);
        }

        post.files = vichanPost.extra_files.Select(f => AdaptFile(f)).ToImmutableList();
        
        return post;
    }

    public File AdaptFile(VichanFile vichanFile)
    {
        var file=new File();
        file.size = vichanFile.fsize;
        file.width = vichanFile.w;
        file.height = vichanFile.h;
        file.thumb = vichanFile.thumb;
        file.path = vichanFile.path;
        return file;
    }

    public PostContainer AdaptPostContainer(VichanPostContainer vichanPostContainer)
    {
        var first = vichanPostContainer.Posts.First();
        var rest = vichanPostContainer.Posts.Skip(1);

        var postContainer = new PostContainer();
        postContainer.threadId = first.no;
        postContainer.name = first.name;
        postContainer.message = first.com ?? "";
        postContainer.email = first.email;
        postContainer.creation = DateTimeOffset.FromUnixTimeSeconds(first.time).UtcDateTime;
        postContainer.flagCode = first.country;
        postContainer.flagName = first.country_name;
        
        
        
        if (first.filename != null)
        {
            var file = new VichanFile();
            file.filename = first.filename;
            file.tim = first.tim;
            file.md5 = first.md5;
            file.ext = first.ext;
            file.fsize = first.fsize;
            file.w = first.w;
            file.h = first.h;
            file.tn_h = first.tn_h;
            file.tn_w = first.tn_w;
            first.extra_files=first.extra_files.Add(file);
        }

        postContainer.files = first.extra_files.Select(f => AdaptFile(f)).ToImmutableList();
        
        postContainer.Posts = rest.Select(p => this.AdaptPost(p)).ToImmutableList();
        return postContainer;
    }
}