using Scheduler;

namespace Kaporetto.Models.Adapters;

public interface IAdapter<CT, P,PC>
{
    CatalogThread AdaptCatalogThread(CT vichanCatalogThread);
    Post AdaptPost(P vichanPost);
    PostContainer AdaptPostContainer(PC vichanPostContainer);
}