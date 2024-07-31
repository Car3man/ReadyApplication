using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Currency;
using RGN.Modules.Inventory;

namespace ReadyApplication.Standard
{
    public interface IInventoryService
    {
        RepositoryEntityCache<string, InventoryItemData> CachedUserItems { get; }
        event System.Action<InventoryItemData> CachedUserItemUpdated;
        event System.Action CachedUserItemsUpdated;
		IFluentAction<List<UpgradesResponseData>> GetItemUpgrades(string itemId, CancellationToken cancellationToken = default);
        IFluentAction<List<VirtualItemUpgrade>> UpgradeItem(string itemId, int upgradeLevel, List<Currency> upgradePrice = null, string upgradeId = null, CancellationToken cancellationToken = default);
        IFluentAction<InventoryItemData> GetUserItemById(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsByIds(List<string> ids, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsByVirtualItemIds(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsByVirtualItemIds(List<string> ids, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsByTags(string tag, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsByTags(List<string> tags, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsByAppIds(string appId, int limit, long startAfter = default, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsByAppIds(List<string> appIds, int limit, long startAfter = default, CancellationToken cancellationToken = default);
        IFluentAction<List<InventoryItemData>> GetUserItemsForThisApp(CancellationToken cancellationToken = default);
    }

    public class InventoryService : IInventoryService
    {
        private readonly ICache _queryCache = new Cache(RepositoryHelper.GetDefaultShortTtl());
        private readonly IUserInventoryRepository _userInventoryRepository;
        
        public RepositoryEntityCache<string, InventoryItemData> CachedUserItems => _userInventoryRepository.CachedItems;

        public event System.Action<InventoryItemData> CachedUserItemUpdated
		{
			add => _userInventoryRepository.CachedItemUpdated += value;
			remove => _userInventoryRepository.CachedItemUpdated -= value;
		}
        public event System.Action CachedUserItemsUpdated
		{
			add => _userInventoryRepository.CachedItemsUpdated += value;
			remove => _userInventoryRepository.CachedItemsUpdated -= value;
		}

        public InventoryService(IReadyApp readyApp, IUserInventoryRepository userInventoryRepository)
        {
            _userInventoryRepository = userInventoryRepository;
            readyApp.UserAuthStateChanged += _ => _userInventoryRepository.InvalidateCache();
        }

        public IFluentAction<List<UpgradesResponseData>> GetItemUpgrades(string itemId, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetItemUpgrades), nameof(itemId), itemId);
            return new FluentAction<List<UpgradesResponseData>>(() => InventoryModule.I.GetUpgradesAsync(itemId, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl());
        }

        public IFluentAction<List<VirtualItemUpgrade>> UpgradeItem(string itemId, int upgradeLevel, List<Currency> upgradePrice = null, string upgradeId = null,
            CancellationToken cancellationToken = default)
        {
            return new FluentAction<List<VirtualItemUpgrade>>(() =>
                InventoryModule.I.UpgradeAsync(itemId, upgradeLevel, upgradePrice, upgradeId, cancellationToken));
        }

        public IFluentAction<InventoryItemData> GetUserItemById(string id, CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetById(id, cancellationToken);

		public IFluentAction<List<InventoryItemData>> GetUserItemsByIds(List<string> ids, CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetByIds(ids, cancellationToken);

        public IFluentAction<List<InventoryItemData>> GetUserItemsByVirtualItemIds(string id, CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetByVirtualItemIds(id, cancellationToken);

		public IFluentAction<List<InventoryItemData>> GetUserItemsByVirtualItemIds(List<string> ids, CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetByVirtualItemIds(ids, cancellationToken);

        public IFluentAction<List<InventoryItemData>> GetUserItemsByTags(string tag, CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetByTags(tag, cancellationToken);

		public IFluentAction<List<InventoryItemData>> GetUserItemsByTags(List<string> tags, CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetByTags(tags, cancellationToken);

        public IFluentAction<List<InventoryItemData>> GetUserItemsByAppIds(string appId, int limit, long startAfter = default,
	        CancellationToken cancellationToken = default)
			=> _userInventoryRepository.GetByAppIds(appId, limit, startAfter, cancellationToken);

        public IFluentAction<List<InventoryItemData>> GetUserItemsByAppIds(List<string> appIds, int limit, long startAfter = default,
            CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetByAppIds(appIds, limit, startAfter, cancellationToken);
        
        public IFluentAction<List<InventoryItemData>> GetUserItemsForThisApp(CancellationToken cancellationToken = default)
            => _userInventoryRepository.GetForThisApp(cancellationToken);
    }
}