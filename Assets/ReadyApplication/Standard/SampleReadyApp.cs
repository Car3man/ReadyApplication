using ReadyApplication.Core;
using UnityEngine;

namespace ReadyApplication.Standard
{
    public class SampleReadyApp : BaseReadyApp
    {
        #region Singleton

        private static SampleReadyApp _instance;
        public static SampleReadyApp Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var gameObj = new GameObject(nameof(SampleReadyApp));
                    _instance = gameObj.AddComponent<SampleReadyApp>();
                    DontDestroyOnLoad(gameObj);
                }
                return _instance;
            }
        }
        public static SampleReadyApp I => Instance;
        public static bool HasInstance => _instance != null;

        private void Awake()
        {
	        if (_instance != null)
	        {
		        if (this != _instance)
		        {
			        DestroyImmediate(gameObject);
			        return;
		        }
		        OnAwake();
	        }
	        else
	        {
		        _instance = this;
		        DontDestroyOnLoad(gameObject);
		        OnAwake();
	        }
		}

        #endregion

        private readonly ServiceLocator _repositories = new();
        private readonly ServiceLocator _services = new();

        private void OnAwake()
        {
            CreateRepositories();
            CreateServices();
        }

        private void CreateRepositories()
        {
            /* Config data repositories */
            _repositories.Register<IAchievementRepository>(new AchievementRepository());
            _repositories.Register<IGamePassRepository>(new GamePassRepository());
            _repositories.Register<ILeaderboardRepository>(new LeaderboardRepository());
            _repositories.Register<ILootBoxRepository>(new LootBoxRepository());
            _repositories.Register<IStoreOfferRepository>(new StoreOfferRepository());
            _repositories.Register<IVirtualItemRepository>(new VirtualItemRepository());
            /* User data repositories */
            _repositories.Register<IUserAchievementRepository>(new UserAchievementRepository());
            _repositories.Register<IUserCurrencyRepository>(new UserCurrencyRepository());
            _repositories.Register<IUserGamePassRepository>(new UserGamePassRepository());
            _repositories.Register<IUserInventoryRepository>(new UserInventoryRepository());
            _repositories.Register<IUserProfileRepository>(new UserProfileRepository());
        }

        private void CreateServices()
        {
            _services.Register<IAchievementService>(new AchievementService(this, _repositories.Get<IAchievementRepository>(), _repositories.Get<IUserAchievementRepository>()));
            _services.Register<ICurrencyService>(new CurrencyService(this, _repositories.Get<IUserCurrencyRepository>()));
            _services.Register<IGameProgressService>(new GameProgressService(this, _repositories.Get<IUserCurrencyRepository>()));
            _services.Register<IInventoryService>(new InventoryService(this, _repositories.Get<IUserInventoryRepository>()));
            _services.Register<ILeaderboardService>(new LeaderboardService(this, _repositories.Get<ILeaderboardRepository>()));
            _services.Register<ILootBoxService>(new LootBoxService(this, _repositories.Get<ILootBoxRepository>(), _repositories.Get<IUserInventoryRepository>()));
            _services.Register<IStoreService>(new StoreService(_repositories.Get<IStoreOfferRepository>(), _repositories.Get<IUserInventoryRepository>(), _repositories.Get<IUserCurrencyRepository>()));
            _services.Register<IUserProfileService>(new UserProfileService(this, _repositories.Get<IUserProfileRepository>()));
            _services.Register<IVirtualItemService>(new VirtualItemService(_repositories.Get<IVirtualItemRepository>()));
            _services.Register<IWalletService>(new WalletService(this));
        }
    
        public T GetService<T>() where T : class
        {
            return _services.Get<T>();
        }
    }
}
