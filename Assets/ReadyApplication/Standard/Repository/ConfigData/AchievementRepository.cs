using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Achievement;

namespace ReadyApplication.Standard
{
	public interface IAchievementRepository
	{
	    RepositoryEntityCache<string, AchievementData> CachedAchievements { get; }
	    event System.Action<AchievementData> CachedAchievementUpdated;
		event System.Action CachedAchievementsUpdated;
		IFluentAction<List<AchievementData>> GetByIds(string id, CancellationToken cancellationToken = default);
	    IFluentAction<List<AchievementData>> GetByIds(List<string> ids, CancellationToken cancellationToken = default);
	    IFluentAction<List<AchievementData>> GetByTags(string tag, int limit, long startAfter = default, CancellationToken cancellationToken = default);
	    IFluentAction<List<AchievementData>> GetByTags(List<string> tags, int limit, long startAfter = default, CancellationToken cancellationToken = default);
	    IFluentAction<List<AchievementData>> GetByAppIds(string appId, int limit, string startAfter = "", CancellationToken cancellationToken = default);
	    IFluentAction<List<AchievementData>> GetByAppIds(List<string> appIds, int limit, string startAfter = "", CancellationToken cancellationToken = default);
	    IFluentAction<List<AchievementData>> GetForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }
    
    public class AchievementRepository : IAchievementRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultLongTtl());
        public RepositoryEntityCache<string, AchievementData> CachedAchievements { get; } = new(RepositoryHelper.GetDefaultLongTtl());
        public event System.Action<AchievementData> CachedAchievementUpdated;
        public event System.Action CachedAchievementsUpdated;

		public void InvalidateCache(bool queryOnly = true)
		{
			_queryCache.Invalidate();

			if (!queryOnly)
			{
				CachedAchievements.Clear();
			}
		}

		public IFluentAction<List<AchievementData>> GetByIds(string id, CancellationToken cancellationToken = default)
			=> GetByIds(new List<string> { id }, cancellationToken);

		public IFluentAction<List<AchievementData>> GetByIds(List<string> ids, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByIds), nameof(ids), ids);
            return new FluentAction<List<AchievementData>>(() => AchievementsModule.I.GetByIdsAsync(ids, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheAchievements);
		}

		public IFluentAction<List<AchievementData>> GetByTags(string tag, int limit, long startAfter = default,
			CancellationToken cancellationToken = default)
			=> GetByTags(new List<string> { tag }, limit, startAfter, cancellationToken);

		public IFluentAction<List<AchievementData>> GetByTags(List<string> tags, int limit, long startAfter = default,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByTags), nameof(tags), tags, nameof(limit), limit, nameof(startAfter), startAfter);
            return new FluentAction<List<AchievementData>>(() => AchievementsModule.I.GetByTagsAsync(tags, limit, startAfter, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheAchievements);
		}

		public IFluentAction<List<AchievementData>> GetByAppIds(string appId, int limit, string startAfter = "",
			CancellationToken cancellationToken = default)
			=> GetByAppIds(new List<string> { appId }, limit, startAfter, cancellationToken);

		public IFluentAction<List<AchievementData>> GetByAppIds(List<string> appIds, int limit, string startAfter = "",
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByAppIds), nameof(appIds), appIds, nameof(limit), limit, nameof(startAfter), startAfter);
            return new FluentAction<List<AchievementData>>(() => AchievementsModule.I.GetByAppIdsAsync(appIds, limit, startAfter, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
				.OnComplete(CacheAchievements);
        }

        public IFluentAction<List<AchievementData>> GetForThisApp(int limit, string startAfter = "",
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetForThisApp), nameof(limit), limit, nameof(startAfter), startAfter);
            return new FluentAction<List<AchievementData>>(() => AchievementsModule.I.GetForCurrentAppAsync(limit, startAfter, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
				.OnComplete(CacheAchievements);
        }

        private void CacheAchievements(List<AchievementData> achievements)
        {
	        bool anyUpdate = false;

	        foreach (AchievementData achievement in achievements)
	        {
				bool hasCachedAchievement = CachedAchievements.TryGetNotExpiredValue(achievement.id, out var cachedAchievement);

				CachedAchievements[achievement.id] = achievement;

		        if (!hasCachedAchievement || cachedAchievement.updatedAt < achievement.updatedAt)
		        {
			        anyUpdate = true;
					CachedAchievementUpdated?.Invoke(achievement);
		        }
	        }

			if (anyUpdate)
			{
				CachedAchievementsUpdated?.Invoke();
			}
        }
    }
}