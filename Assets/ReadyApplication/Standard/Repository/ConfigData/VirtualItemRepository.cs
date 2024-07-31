using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.VirtualItems;

namespace ReadyApplication.Standard
{
    public interface IVirtualItemRepository
	{
	    RepositoryEntityCache<string, VirtualItem> CachedVirtualItems { get; }
	    event System.Action<VirtualItem> CachedVirtualItemUpdated;
	    event System.Action CachedVirtualItemsUpdated;
		IFluentAction<List<VirtualItem>> GetByIds(string id, CancellationToken cancellationToken = default);
	    IFluentAction<List<VirtualItem>> GetByIds(List<string> ids, CancellationToken cancellationToken = default);
	    IFluentAction<List<VirtualItem>> GetByTags(string tag, CancellationToken cancellationToken = default);
	    IFluentAction<List<VirtualItem>> GetByTags(List<string> tags, CancellationToken cancellationToken = default);
	    IFluentAction<List<VirtualItem>> GetForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }

    public class VirtualItemRepository : IVirtualItemRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultLongTtl());
        public RepositoryEntityCache<string, VirtualItem> CachedVirtualItems { get; } = new(RepositoryHelper.GetDefaultLongTtl());
        public event System.Action<VirtualItem> CachedVirtualItemUpdated;
        public event System.Action CachedVirtualItemsUpdated;

		public void InvalidateCache(bool queryOnly = true)
        {
	        _queryCache.Invalidate();

	        if (!queryOnly)
	        {
		        CachedVirtualItems.Clear();
	        }
        }

		public IFluentAction<List<VirtualItem>> GetByIds(string id, CancellationToken cancellationToken = default)
			=> GetByIds(new List<string> { id }, cancellationToken);

		public IFluentAction<List<VirtualItem>> GetByIds(List<string> ids, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByIds), nameof(ids), ids);
            return new FluentAction<List<VirtualItem>>(() => VirtualItemsModule.I.GetVirtualItemsByIdsAsync(ids, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheVirtualItems);
		}

		public IFluentAction<List<VirtualItem>> GetByTags(string tag, CancellationToken cancellationToken = default)
			=> GetByTags(new List<string> { tag }, cancellationToken);

		public IFluentAction<List<VirtualItem>> GetByTags(List<string> tags, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByTags), nameof(tags), tags);
            return new FluentAction<List<VirtualItem>>(() => VirtualItemsModule.I.GetByTagsAsync(tags, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheVirtualItems);
		}

        public IFluentAction<List<VirtualItem>> GetForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetForThisApp), nameof(limit), limit, nameof(startAfter), startAfter);
            return new FluentAction<List<VirtualItem>>(() => VirtualItemsModule.I.GetVirtualItemsAsync(limit, startAfter, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheVirtualItems);
		}

        private void CacheVirtualItems(List<VirtualItem> virtualItems)
        {
	        bool anyUpdate = false;

	        foreach (VirtualItem virtualItem in virtualItems)
	        {
		        bool hasCachedVirtualItem = CachedVirtualItems.TryGetNotExpiredValue(virtualItem.id, out var cachedVirtualItem);

		        CachedVirtualItems[virtualItem.id] = virtualItem;

		        if (!hasCachedVirtualItem || cachedVirtualItem.updatedAt < virtualItem.updatedAt)
		        {
			        anyUpdate = true;
			        CachedVirtualItemUpdated?.Invoke(virtualItem);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedVirtualItemsUpdated?.Invoke();
	        }
        }
	}
}