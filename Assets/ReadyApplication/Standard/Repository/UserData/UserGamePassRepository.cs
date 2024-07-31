using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.GamePass;

namespace ReadyApplication.Standard
{
	public interface IUserGamePassRepository
	{
        RepositoryEntityCache<string, GamePassUserData> CachedGamePasses { get; }
        event System.Action<GamePassUserData> CachedGamePassUpdated;
        event System.Action CachedGamePassesUpdated;
		IFluentAction<List<GamePassUserData>> GetById(string id = "", string requestName = "", CancellationToken cancellationToken = default);
        IFluentAction<List<GamePassUserData>> GetAll(CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }
    
    public class UserGamePassRepository : IUserGamePassRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultShortTtl());
        public RepositoryEntityCache<string, GamePassUserData> CachedGamePasses { get; } = new(RepositoryHelper.GetDefaultShortTtl());
        public event System.Action<GamePassUserData> CachedGamePassUpdated;
        public event System.Action CachedGamePassesUpdated;

		public void InvalidateCache(bool queryOnly = true)
		{
			_queryCache.Invalidate();

			if (!queryOnly)
			{
				CachedGamePasses.Clear();
			}
		}

		public IFluentAction<List<GamePassUserData>> GetById(string id = "", string requestName = "", CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetById), nameof(id), id, nameof(requestName), requestName);
            return new FluentAction<List<GamePassUserData>>(() => GamePassModule.I.GetForUserAsync(id, requestName, "", cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheUserGamePasses);
        }

        public IFluentAction<List<GamePassUserData>> GetAll(CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetAll));
            return new FluentAction<List<GamePassUserData>>(() => GamePassModule.I.GetAllForUserAsync("", cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheUserGamePasses);
        }

        private void CacheUserGamePasses(List<GamePassUserData> gamePasses)
        {
	        bool anyUpdate = false;

	        foreach (GamePassUserData gamePass in gamePasses)
	        {
		        bool hasCachedGamePass = CachedGamePasses.TryGetNotExpiredValue(gamePass.id, out var cachedGamePass);

		        CachedGamePasses[gamePass.id] = gamePass;

		        if (!hasCachedGamePass || cachedGamePass.updatedAt < gamePass.updatedAt)
		        {
			        anyUpdate = true;
			        CachedGamePassUpdated?.Invoke(gamePass);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedGamePassesUpdated?.Invoke();
	        }
        }
	}
}