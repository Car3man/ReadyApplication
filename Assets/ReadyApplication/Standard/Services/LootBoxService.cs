using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Inventory;
using RGN.Modules.Store;

namespace ReadyApplication.Standard
{
    public interface ILootBoxService
    {
	    RepositoryEntityCache<string, LootBox> CachedLootBoxes { get; }
	    event System.Action<LootBox> CachedLootBoxUpdated;
	    event System.Action CachedLootBoxesUpdated;
		IFluentAction<bool> IsLootBoxAvailable(string name, CancellationToken cancellationToken = default);
        IFluentAction<long> GetLootBoxAvailableCount(string name, CancellationToken cancellationToken = default);
        IFluentAction<InventoryItemData> OpenLootBox(string name, CancellationToken cancellationToken = default);
        IFluentAction<List<LootBox>> GetLootBoxesByIds(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<LootBox>> GetLootBoxesByIds(List<string> ids, CancellationToken cancellationToken = default);
        IFluentAction<List<LootBox>> GetLootBoxesByAppId(string appId, int limit, string startAfter = "", CancellationToken cancellationToken = default);
        IFluentAction<List<LootBox>> GetLootBoxesForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default);
    }

    public class LootBoxService : ILootBoxService
    {
        private readonly ICache _queryCache = new Cache(RepositoryHelper.GetDefaultShortTtl());
        private readonly ILootBoxRepository _lootBoxRepository;
        private readonly IUserInventoryRepository _userInventoryRepository;

        public RepositoryEntityCache<string, LootBox> CachedLootBoxes => _lootBoxRepository.CachedLootBoxes;

        public event System.Action<LootBox> CachedLootBoxUpdated
        {
            add => _lootBoxRepository.CachedLootBoxUpdated += value;
			remove => _lootBoxRepository.CachedLootBoxUpdated -= value;
        }
        public event System.Action CachedLootBoxesUpdated
        {
	        add => _lootBoxRepository.CachedLootBoxesUpdated += value;
	        remove => _lootBoxRepository.CachedLootBoxesUpdated -= value;
        }

        public LootBoxService(IReadyApp readyApp, ILootBoxRepository lootBoxRepository, IUserInventoryRepository userInventoryRepository)
        {
            _lootBoxRepository = lootBoxRepository;
            _userInventoryRepository = userInventoryRepository;
            readyApp.UserAuthStateChanged += _ => _userInventoryRepository.InvalidateCache();
        }
        
        public IFluentAction<bool> IsLootBoxAvailable(string name, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(IsLootBoxAvailable), nameof(name), name);
            return new FluentAction<bool>(() => StoreModule.I.LootboxIsAvailableAsync(name, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl());
        }

        public IFluentAction<long> GetLootBoxAvailableCount(string name, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetLootBoxAvailableCount), nameof(name), name);
            return new FluentAction<long>(() => StoreModule.I.GetAvailableLootBoxItemsCountAsync(name, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl());
        }
        
        public IFluentAction<InventoryItemData> OpenLootBox(string name, CancellationToken cancellationToken = default)
        {
            return new FluentAction<InventoryItemData>(async () =>
            {
                InventoryItemData openResult = await StoreModule.I.OpenLootboxAsync(name, cancellationToken);
                _userInventoryRepository.InvalidateCache();
                return openResult;
            });
        }

        public IFluentAction<List<LootBox>> GetLootBoxesByIds(string id, CancellationToken cancellationToken = default)
			=> _lootBoxRepository.GetByIds(id, cancellationToken);

		public IFluentAction<List<LootBox>> GetLootBoxesByIds(List<string> ids, CancellationToken cancellationToken = default)
            => _lootBoxRepository.GetByIds(ids, cancellationToken);

        public IFluentAction<List<LootBox>> GetLootBoxesByAppId(string appId, int limit, string startAfter = "",
            CancellationToken cancellationToken = default)
            => _lootBoxRepository.GetByAppId(appId, limit, startAfter, cancellationToken);

        public IFluentAction<List<LootBox>> GetLootBoxesForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default)
            => _lootBoxRepository.GetForThisApp(limit, startAfter, cancellationToken);
    }
}