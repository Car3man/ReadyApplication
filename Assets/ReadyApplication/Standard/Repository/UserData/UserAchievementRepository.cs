using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Achievement;

namespace ReadyApplication.Standard
{
	public interface IUserAchievementRepository
	{
        RepositoryEntityCache<string, UserAchievement> CachedAchievements { get; }
        event System.Action<UserAchievement> CachedAchievementUpdated;
        event System.Action CachedAchievementsUpdated;
		IFluentAction<UserAchievement> GetById(string id, bool withHistory = false, CancellationToken cancellationToken = default);
        IFluentAction<List<UserAchievement>> GetAll(int limit, long startAfter = long.MaxValue, bool withHistory = false, CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }
    
    public class UserAchievementRepository : IUserAchievementRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultShortTtl());
        public RepositoryEntityCache<string, UserAchievement> CachedAchievements { get; } = new(RepositoryHelper.GetDefaultShortTtl());
        public event System.Action<UserAchievement> CachedAchievementUpdated;
        public event System.Action CachedAchievementsUpdated;

		public void InvalidateCache(bool queryOnly = true)
		{
			_queryCache.Invalidate();

			if (!queryOnly)
			{
				CachedAchievements.Clear();
			}
		}

		public IFluentAction<UserAchievement> GetById(string id, bool withHistory = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetById), nameof(id), id, nameof(withHistory), withHistory);
            return new FluentAction<UserAchievement>(() => AchievementsModule.I.GetUserAchievementByIdAsync(id, null, withHistory, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheUserAchievement);
        }

        public IFluentAction<List<UserAchievement>> GetAll(int limit, long startAfter = long.MaxValue, bool withHistory = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetAll), nameof(limit), limit, nameof(startAfter), startAfter, nameof(withHistory), withHistory);
            return new FluentAction<List<UserAchievement>>(() => AchievementsModule.I.GetUserAchievementsAsync(null, withHistory, startAfter, limit, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheUserAchievements);
        }

        private void CacheUserAchievement(UserAchievement achievement)
        {
	        bool hasCachedAchievement = CachedAchievements.TryGetNotExpiredValue(achievement.id, out var cachedUserAchievement);

	        CachedAchievements[achievement.id] = achievement;

	        if (!hasCachedAchievement || !Comparers.UserAchievement.Equals(cachedUserAchievement, achievement))
	        {
		        CachedAchievementUpdated?.Invoke(achievement);
		        CachedAchievementsUpdated?.Invoke();
	        }
        }

		private void CacheUserAchievements(List<UserAchievement> achievements)
        {
	        bool anyUpdate = false;

	        foreach (UserAchievement achievement in achievements)
	        {
		        bool hasCachedAchievement = CachedAchievements.TryGetNotExpiredValue(achievement.id, out var cachedUserAchievement);

		        CachedAchievements[achievement.id] = achievement;

		        if (!hasCachedAchievement || !Comparers.UserAchievement.Equals(cachedUserAchievement, achievement))
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