using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Inventory;

namespace ReadyApplication.Standard
{
	public interface IUserInventoryRepository
	{
        RepositoryEntityCache<string, InventoryItemData> CachedItems { get; }
        event System.Action<InventoryItemData> CachedItemUpdated;
        event System.Action CachedItemsUpdated;
		IFluentAction<InventoryItemData> GetById(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetByIds(List<string> ids, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetByVirtualItemIds(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetByVirtualItemIds(List<string> ids, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetByTags(string tag, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetByTags(List<string> tags, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetByAppIds(string appId, int limit, long startAfter = default, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetByAppIds(List<string> appIds, int limit, long startAfter = default, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetForThisApp(CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }
    
    public class UserInventoryRepository : IUserInventoryRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultShortTtl());
        public RepositoryEntityCache<string, InventoryItemData> CachedItems { get; } = new(RepositoryHelper.GetDefaultShortTtl());
        public event System.Action<InventoryItemData> CachedItemUpdated;
        public event System.Action CachedItemsUpdated;

		public void InvalidateCache(bool queryOnly = true)
		{
			_queryCache.Invalidate();

			if (!queryOnly)
			{
				CachedItems.Clear();
			}
		}

		public IFluentAction<InventoryItemData> GetById(string id, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetById), nameof(id), id);
            return new FluentAction<InventoryItemData>(() => InventoryModule.I.GetByIdAsync(id, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheGamePass);
        }

        public IFluentAction<List<InventoryItemData>> GetByIds(List<string> ids, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByIds), nameof(ids), ids);
            return new FluentAction<List<InventoryItemData>>(() => InventoryModule.I.GetByIdsAsync(ids, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheGamePasses);
        }

        public IFluentAction<List<InventoryItemData>> GetByVirtualItemIds(string id, CancellationToken cancellationToken = default)
	        => GetByVirtualItemIds(new List<string> { id }, cancellationToken);

        public IFluentAction<List<InventoryItemData>> GetByVirtualItemIds(List<string> ids, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByVirtualItemIds), nameof(ids), ids);
            return new FluentAction<List<InventoryItemData>>(() => InventoryModule.I.GetByVirtualItemIdsAsync(ids, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheGamePasses);
        }

        public IFluentAction<List<InventoryItemData>> GetByTags(string tag, CancellationToken cancellationToken = default)
	        => GetByTags(new List<string> { tag }, cancellationToken);

        public IFluentAction<List<InventoryItemData>> GetByTags(List<string> tags, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByTags), nameof(tags), tags);
            return new FluentAction<List<InventoryItemData>>(() => InventoryModule.I.GetByTagsAsync(tags, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheGamePasses);
        }

        public IFluentAction<List<InventoryItemData>> GetByAppIds(string appId, int limit, long startAfter = default,
	        CancellationToken cancellationToken = default)
	        => GetByAppIds(new List<string> { appId }, limit, startAfter, cancellationToken);

        public IFluentAction<List<InventoryItemData>> GetByAppIds(List<string> appIds, int limit, long startAfter = default,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByAppIds), nameof(appIds), appIds, nameof(limit), limit, nameof(startAfter), startAfter);
            return new FluentAction<List<InventoryItemData>>(() => InventoryModule.I.GetByAppIdsAsync(appIds, startAfter, limit, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheGamePasses);
        }

        public IFluentAction<List<InventoryItemData>> GetForThisApp(CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetForThisApp));
            return new FluentAction<List<InventoryItemData>>(() => InventoryModule.I.GetAllForCurrentAppAsync(cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheGamePasses);
        }

        private void CacheGamePass(InventoryItemData item)
        {
	        bool hasCachedItem = CachedItems.TryGetNotExpiredValue(item.id, out var cachedItem);

	        CachedItems[item.id] = item;

	        if (!hasCachedItem || cachedItem.updatedAt < item.updatedAt)
	        {
		        CachedItemUpdated?.Invoke(item);
		        CachedItemsUpdated?.Invoke();
	        }
        }

        private void CacheGamePasses(List<InventoryItemData> items)
        {
	        bool anyUpdate = false;

	        foreach (InventoryItemData item in items)
	        {
		        bool hasCachedItem = CachedItems.TryGetNotExpiredValue(item.id, out var cachedItem);

		        CachedItems[item.id] = item;

		        if (!hasCachedItem || cachedItem.updatedAt < item.updatedAt)
		        {
			        anyUpdate = true;
			        CachedItemUpdated?.Invoke(item);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedItemsUpdated?.Invoke();
	        }
        }
	}
}