using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Store;

namespace ReadyApplication.Standard
{
    public interface IStoreOfferRepository
	{
	    RepositoryEntityCache<string, StoreOffer> CachedStoreOffers { get; }
	    event System.Action<StoreOffer> CachedStoreOfferUpdated;
	    event System.Action CachedStoreOffersUpdated;
		IFluentAction<List<StoreOffer>> GetByIds(string id, int limit = default, long startAfter = default, bool ignoreTimestamp = true, CancellationToken cancellationToken = default);
	    IFluentAction<List<StoreOffer>> GetByIds(List<string> ids, int limit = default, long startAfter = default, bool ignoreTimestamp = true, CancellationToken cancellationToken = default);
	    IFluentAction<List<StoreOffer>> GetByTimestamp(string appId, long timestamp, CancellationToken cancellationToken = default);
	    IFluentAction<List<StoreOffer>> GetByTags(string tag, int limit = default, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<StoreOffer>> GetByTags(List<string> tags, int limit = default, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<StoreOffer>> GetByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<StoreOffer>> GetByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<StoreOffer>> GetForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }

    public class StoreOfferRepository : IStoreOfferRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultLongTtl());
        public RepositoryEntityCache<string, StoreOffer> CachedStoreOffers { get; } = new(RepositoryHelper.GetDefaultLongTtl());
        public event System.Action<StoreOffer> CachedStoreOfferUpdated;
        public event System.Action CachedStoreOffersUpdated;

		public void InvalidateCache(bool queryOnly = true)
        {
	        _queryCache.Invalidate();

	        if (!queryOnly)
	        {
		        CachedStoreOffers.Clear();
	        }
        }

		public IFluentAction<List<StoreOffer>> GetByIds(string id, int limit = default, long startAfter = default, bool ignoreTimestamp = true,
			CancellationToken cancellationToken = default)
			=> GetByIds(new List<string> { id }, limit, startAfter, ignoreTimestamp, cancellationToken);

		public IFluentAction<List<StoreOffer>> GetByIds(List<string> ids, int limit = default, long startAfter = default, bool ignoreTimestamp = true,
	        CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByIds), nameof(ids), ids, nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<StoreOffer>>(() => StoreModule.I.GetByIdsAsync(ids, limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheStoreOffers);
		}

        public IFluentAction<List<StoreOffer>> GetByTimestamp(string appId, long timestamp, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByTimestamp), nameof(appId), appId, nameof(timestamp), timestamp);
            return new FluentAction<List<StoreOffer>>(() => StoreModule.I.GetByTimestampAsync(appId, timestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheStoreOffers);
		}

        public IFluentAction<List<StoreOffer>> GetByTags(string tag, int limit = default, long startAfter = default, bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
	        => GetByTags(new List<string> { tag }, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<StoreOffer>> GetByTags(List<string> tags, int limit = default, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByTags), nameof(tags), tags, nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<StoreOffer>>(() => StoreModule.I.GetByTagsAsync(tags, limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheStoreOffers);
		}

        public IFluentAction<List<StoreOffer>> GetByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
	        => GetByAppIds(new List<string> { appId }, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<StoreOffer>> GetByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByAppIds), nameof(appIds), appIds, nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<StoreOffer>>(() => StoreModule.I.GetByAppIdsAsync(appIds, limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheStoreOffers);
		}

        public IFluentAction<List<StoreOffer>> GetForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetForThisApp), nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<StoreOffer>>(() => StoreModule.I.GetForCurrentAppAsync(limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheStoreOffers);
		}

        private void CacheStoreOffers(List<StoreOffer> storeOffers)
        {
	        bool anyUpdate = false;

	        foreach (StoreOffer storeOffer in storeOffers)
	        {
		        bool hasCachedStoreOffer = CachedStoreOffers.TryGetNotExpiredValue(storeOffer.id, out var cachedStoreOffer);

		        CachedStoreOffers[storeOffer.id] = storeOffer;

		        if (!hasCachedStoreOffer || cachedStoreOffer.updatedAt < storeOffer.updatedAt)
		        {
			        anyUpdate = true;
			        CachedStoreOfferUpdated?.Invoke(storeOffer);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedStoreOffersUpdated?.Invoke();
	        }
        }
	}
}