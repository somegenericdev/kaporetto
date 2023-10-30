using Scheduler;

namespace Kaporetto.Models.Adapters;

public class NullAdapter : IAdapter<CatalogThread, Post, PostContainer>
{
    public CatalogThread AdaptCatalogThread(CatalogThread vichanCatalogThread) => vichanCatalogThread;
    public Post AdaptPost(Post vichanPost) => vichanPost;
    public PostContainer AdaptPostContainer(PostContainer vichanPostContainer) => vichanPostContainer;
}