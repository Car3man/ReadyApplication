using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.GamePass;

namespace ReadyApplication.Standard
{
	public interface IGamePassRepository
	{
	    RepositoryEntityCache<string, GamePassData> CachedGamePasses { get; }
	    event System.Action<GamePassData> CachedGamePassUpdated;
	    event System.Action CachedGamePassesUpdated;
		IFluentAction<GamePassData> GetById(string id = "", string requestName = "", CancellationToken cancellationToken = default);
	    IFluentAction<List<GamePassData>> GetForThisApp(CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }

    public class GamePassRepository : IGamePassRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultLongTtl());
        public RepositoryEntityCache<string, GamePassData> CachedGamePasses { get; } = new(RepositoryHelper.GetDefaultLongTtl());
        public event System.Action<GamePassData> CachedGamePassUpdated;
        public event System.Action CachedGamePassesUpdated;

		public void InvalidateCache(bool queryOnly = true)
        {
	        _queryCache.Invalidate();

	        if (!queryOnly)
	        {
		        CachedGamePasses.Clear();
	        }
        }

		public IFluentAction<GamePassData> GetById(string id = "", string requestName = "", CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetById), nameof(id), id, nameof(requestName), requestName);
            return new FluentAction<GamePassData>(() => GamePassModule.I.GetAsync(id, requestName, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheGamePass);
		}

        public IFluentAction<List<GamePassData>> GetForThisApp(CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetForThisApp));
            return new FluentAction<List<GamePassData>>(() => GamePassModule.I.GetForCurrentAppAsync(cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheGamePasses);
		}

        private void CacheGamePass(GamePassData gamePass)
        {
	        bool hasCachedGamePass = CachedGamePasses.TryGetNotExpiredValue(gamePass.id, out var cachedGamePass);

	        CachedGamePasses[gamePass.id] = gamePass;

	        if (!hasCachedGamePass || cachedGamePass.updatedAt < gamePass.updatedAt)
	        {
		        CachedGamePassUpdated?.Invoke(gamePass);
		        CachedGamePassesUpdated?.Invoke();
	        }
        }

        private void CacheGamePasses(List<GamePassData> gamePasses)
        {
	        bool anyUpdate = false;

	        foreach (GamePassData gamePassData in gamePasses)
	        {
		        bool hasCachedGamePass = CachedGamePasses.TryGetNotExpiredValue(gamePassData.id, out var cachedGamePass);

		        CachedGamePasses[gamePassData.id] = gamePassData;

		        if (!hasCachedGamePass || cachedGamePass.updatedAt < gamePassData.updatedAt)
		        {
			        anyUpdate = true;
			        CachedGamePassUpdated?.Invoke(gamePassData);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedGamePassesUpdated?.Invoke();
	        }
        }
	}
}