using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Store;

namespace ReadyApplication.Standard
{
	public interface ILootBoxRepository
	{
	    RepositoryEntityCache<string, LootBox> CachedLootBoxes { get; }
	    event System.Action<LootBox> CachedLootBoxUpdated;
	    event System.Action CachedLootBoxesUpdated;
		IFluentAction<List<LootBox>> GetByIds(string id, CancellationToken cancellationToken = default);
	    IFluentAction<List<LootBox>> GetByIds(List<string> ids, CancellationToken cancellationToken = default);
	    IFluentAction<List<LootBox>> GetByAppId(string appId, int limit, string startAfter = "", CancellationToken cancellationToken = default);
	    IFluentAction<List<LootBox>> GetForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }
    
    public class LootBoxRepository : ILootBoxRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultLongTtl());
        public RepositoryEntityCache<string, LootBox> CachedLootBoxes { get; } = new(RepositoryHelper.GetDefaultLongTtl());
        public event System.Action<LootBox> CachedLootBoxUpdated;
        public event System.Action CachedLootBoxesUpdated;

		public void InvalidateCache(bool queryOnly = true)
        {
	        _queryCache.Invalidate();

	        if (!queryOnly)
	        {
		        CachedLootBoxes.Clear();
	        }
        }

		public IFluentAction<List<LootBox>> GetByIds(string id, CancellationToken cancellationToken = default)
			=> GetByIds(new List<string> { id }, cancellationToken);

		public IFluentAction<List<LootBox>> GetByIds(List<string> ids, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByIds), nameof(ids), ids);
            return new FluentAction<List<LootBox>>(() => StoreModule.I.GetLootBoxesByIdsAsync(ids, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLootBoxes);
		}

        public IFluentAction<List<LootBox>> GetByAppId(string appId, int limit, string startAfter = "",
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByAppId), nameof(appId), appId, nameof(limit), limit, nameof(startAfter), startAfter);
            return new FluentAction<List<LootBox>>(() => StoreModule.I.GetLootBoxesByAppIdAsync(appId, limit, startAfter, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLootBoxes);
        }

        public IFluentAction<List<LootBox>> GetForThisApp(int limit, string startAfter = "",
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetForThisApp), nameof(limit), limit, nameof(startAfter), startAfter);
            return new FluentAction<List<LootBox>>(() => StoreModule.I.GetLootBoxesForCurrentAppAsync(limit, startAfter, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLootBoxes);
		}

        private void CacheLootBoxes(List<LootBox> lootBoxes)
        {
	        bool anyUpdate = false;

	        foreach (LootBox lootBox in lootBoxes)
	        {
		        bool hasCachedLootBox = CachedLootBoxes.TryGetNotExpiredValue(lootBox.id, out var cachedLootBox);

		        CachedLootBoxes[lootBox.id] = lootBox;

		        if (!hasCachedLootBox || cachedLootBox.updatedAt < lootBox.updatedAt)
		        {
			        anyUpdate = true;
			        CachedLootBoxUpdated?.Invoke(lootBox);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedLootBoxesUpdated?.Invoke();
	        }
        }
	}
}