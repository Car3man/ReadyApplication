using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Store;

namespace ReadyApplication.Standard
{
    public interface IStoreService
    {
	    RepositoryEntityCache<string, StoreOffer> CachedStoreOffers { get; }
	    event System.Action<StoreOffer> CachedStoreOfferUpdated;
	    event System.Action CachedStoreOffersUpdated;
		IFluentAction<PurchaseResult> BuyVirtualItem(string itemId, List<string> currencies = null, string offerId = "", CancellationToken cancellationToken = default);
        IFluentAction<PurchaseResult> BuyVirtualItems(List<string> itemIds, List<string> currencies = null, string offerId = "", CancellationToken cancellationToken = default);
        IFluentAction<PurchaseResult> BuyStoreOffer(string offerId, List<string> currencies = null, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersByIds(string id, int limit = default, long startAfter = default, bool ignoreTimestamp = true, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersByIds(List<string> ids, int limit = default, long startAfter = default, bool ignoreTimestamp = true, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersByTimestamp(string appId, long timestamp, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersByTags(string tag, int limit = default, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersByTags(List<string> tags, int limit = default, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<StoreOffer>> GetStoreOffersForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
    }

    public class StoreService : IStoreService
    {
        private readonly IStoreOfferRepository _storeOfferRepository;
        private readonly IUserInventoryRepository _userInventoryRepository;
        private readonly IUserCurrencyRepository _userCurrencyRepository;

        public RepositoryEntityCache<string, StoreOffer> CachedStoreOffers => _storeOfferRepository.CachedStoreOffers;

        public event System.Action<StoreOffer> CachedStoreOfferUpdated
		{
			add => _storeOfferRepository.CachedStoreOfferUpdated += value;
			remove => _storeOfferRepository.CachedStoreOfferUpdated -= value;
		}
        public event System.Action CachedStoreOffersUpdated
        {
            add => _storeOfferRepository.CachedStoreOffersUpdated += value;
	        remove => _storeOfferRepository.CachedStoreOffersUpdated -= value;
        }

        public StoreService(IStoreOfferRepository storeOfferRepository,
            IUserInventoryRepository userInventoryRepository, IUserCurrencyRepository userCurrencyRepository)
        {
            _storeOfferRepository = storeOfferRepository;
            _userInventoryRepository = userInventoryRepository;
            _userCurrencyRepository = userCurrencyRepository;
        }
        
        public IFluentAction<PurchaseResult> BuyVirtualItem(string itemId, List<string> currencies = null, string offerId = "",
            CancellationToken cancellationToken = default)
            => BuyVirtualItems(new List<string> { itemId }, currencies, offerId, cancellationToken);

        public IFluentAction<PurchaseResult> BuyVirtualItems(List<string> itemIds, List<string> currencies = null, string offerId = "",
            CancellationToken cancellationToken = default)
        {
            return new FluentAction<PurchaseResult>(async () =>
            {
                PurchaseResult result = await StoreModule.I.BuyVirtualItemsAsync(itemIds, currencies, offerId, cancellationToken);
                _userCurrencyRepository.InvalidateCache();
                _userInventoryRepository.InvalidateCache();
                return result;
            });
        }

        public IFluentAction<PurchaseResult> BuyStoreOffer(string offerId, List<string> currencies = null, CancellationToken cancellationToken = default)
        {
            return new FluentAction<PurchaseResult>(async () =>
            {
                PurchaseResult result = await StoreModule.I.BuyStoreOfferAsync(offerId, currencies, cancellationToken);
                _userCurrencyRepository.InvalidateCache();
                _userInventoryRepository.InvalidateCache();
                return result;
            });
        }

        public IFluentAction<List<StoreOffer>> GetStoreOffersByIds(string id, int limit = default, long startAfter = default, bool ignoreTimestamp = true,
	        CancellationToken cancellationToken = default)
			=> _storeOfferRepository.GetByIds(id, limit, startAfter, ignoreTimestamp, cancellationToken);

		public IFluentAction<List<StoreOffer>> GetStoreOffersByIds(List<string> ids, int limit = default, long startAfter = default, bool ignoreTimestamp = true, CancellationToken cancellationToken = default)
            => _storeOfferRepository.GetByIds(ids, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<StoreOffer>> GetStoreOffersByTimestamp(string appId, long timestamp, CancellationToken cancellationToken = default)
            => _storeOfferRepository.GetByTimestamp(appId, timestamp, cancellationToken);

        public IFluentAction<List<StoreOffer>> GetStoreOffersByTags(string tag, int limit = default, long startAfter = default,
	        bool ignoreTimestamp = false, CancellationToken cancellationToken = default)
			=> _storeOfferRepository.GetByTags(tag, limit, startAfter, ignoreTimestamp, cancellationToken);

		public IFluentAction<List<StoreOffer>> GetStoreOffersByTags(List<string> tags, int limit = default, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
            => _storeOfferRepository.GetByTags(tags, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<StoreOffer>> GetStoreOffersByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
			=> _storeOfferRepository.GetByAppIds(appId, limit, startAfter, ignoreTimestamp, cancellationToken);

		public IFluentAction<List<StoreOffer>> GetStoreOffersByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
            => _storeOfferRepository.GetByAppIds(appIds, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<StoreOffer>> GetStoreOffersForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
            => _storeOfferRepository.GetForThisApp(limit, startAfter, ignoreTimestamp, cancellationToken);
    }
}