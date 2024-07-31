using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Leaderboard;

namespace ReadyApplication.Standard
{
    public interface ILeaderboardRepository
	{
	    RepositoryEntityCache<string, LeaderboardData> CachedLeaderboards { get; }
	    event System.Action<LeaderboardData> CachedLeaderboardUpdated;
	    event System.Action CachedLeaderboardsUpdated;
		IFluentAction<LeaderboardData> GetById(string id, CancellationToken cancellationToken = default);
	    IFluentAction<List<LeaderboardData>> GetByIds(List<string> ids, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<LeaderboardData> GetByRequestName(string requestName, CancellationToken cancellationToken = default);
	    IFluentAction<List<LeaderboardData>> GetByRequestNames(List<string> requestNames, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<LeaderboardData>> GetByTags(string tag, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<LeaderboardData>> GetByTags(List<string> tags, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<LeaderboardData>> GetByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<LeaderboardData>> GetByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
	    IFluentAction<List<LeaderboardData>> GetForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }

    public class LeaderboardRepository : ILeaderboardRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultLongTtl());
        public RepositoryEntityCache<string, LeaderboardData> CachedLeaderboards { get; } = new(RepositoryHelper.GetDefaultLongTtl());
        public event System.Action<LeaderboardData> CachedLeaderboardUpdated;
        public event System.Action CachedLeaderboardsUpdated;

		public void InvalidateCache(bool queryOnly = true)
        {
	        _queryCache.Invalidate();

	        if (!queryOnly)
	        {
		        CachedLeaderboards.Clear();
	        }
        }

		public IFluentAction<LeaderboardData> GetById(string id, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetById), nameof(id), id);
            return new FluentAction<LeaderboardData>(() => LeaderboardModule.I.GetLeaderboardByIdAsync(id, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLeaderboard);
		}

		public IFluentAction<List<LeaderboardData>> GetByIds(List<string> ids, int limit, long startAfter = default, bool ignoreTimestamp = false,
			CancellationToken cancellationToken = default)
		{
			string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByRequestNames), nameof(ids), ids, nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
			return new FluentAction<List<LeaderboardData>>(() => LeaderboardModule.I.GetLeaderboardByIdsAsync(ids, limit, startAfter, ignoreTimestamp, cancellationToken))
				.Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
				.OnComplete(CacheLeaderboards);
		}

		public IFluentAction<LeaderboardData> GetByRequestName(string requestName, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByRequestName), nameof(requestName), requestName);
            return new FluentAction<LeaderboardData>(() => LeaderboardModule.I.GetLeaderboardByRequestNameAsync(requestName, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLeaderboard);
		}

        public IFluentAction<List<LeaderboardData>> GetByRequestNames(List<string> requestNames, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByRequestNames), nameof(requestNames), requestNames, nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<LeaderboardData>>(() => LeaderboardModule.I.GetLeaderboardByRequestNamesAsync(requestNames, limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLeaderboards);
		}

        public IFluentAction<List<LeaderboardData>> GetByTags(string tag, int limit, long startAfter = default,
	        bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
	        => GetByTags(new List<string> { tag }, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<LeaderboardData>> GetByTags(List<string> tags, int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByTags), nameof(tags), tags, nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<LeaderboardData>>(() => LeaderboardModule.I.GetLeaderboardByTagsAsync(tags, limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLeaderboards);
		}

        public IFluentAction<List<LeaderboardData>> GetByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
	        => GetByAppIds(new List<string> { appId }, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<LeaderboardData>> GetByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetByAppIds), nameof(appIds), appIds, nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<LeaderboardData>>(() => LeaderboardModule.I.GetLeaderboardByAppIdsAsync(appIds, limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLeaderboards);
		}

        public IFluentAction<List<LeaderboardData>> GetForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetForThisApp), nameof(limit), limit, nameof(startAfter), startAfter, nameof(ignoreTimestamp), ignoreTimestamp);
            return new FluentAction<List<LeaderboardData>>(() => LeaderboardModule.I.GetLeaderboardForCurrentAppAsync(limit, startAfter, ignoreTimestamp, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheLeaderboards);
		}

        private void CacheLeaderboard(LeaderboardData leaderboard)
        {
	        bool hasCachedLeaderboard = CachedLeaderboards.TryGetNotExpiredValue(leaderboard.id, out var cachedLeaderboard);

	        CachedLeaderboards[leaderboard.id] = leaderboard;

	        if (!hasCachedLeaderboard || cachedLeaderboard.updatedAt < leaderboard.updatedAt)
	        {
		        CachedLeaderboardUpdated?.Invoke(leaderboard);
		        CachedLeaderboardsUpdated?.Invoke();
	        }
        }

        private void CacheLeaderboards(List<LeaderboardData> leaderboards)
        {
	        bool anyUpdate = false;

	        foreach (LeaderboardData leaderboard in leaderboards)
	        {
		        bool hasCachedLeaderboard = CachedLeaderboards.TryGetNotExpiredValue(leaderboard.id, out var cachedLeaderboard);

		        CachedLeaderboards[leaderboard.id] = leaderboard;

		        if (!hasCachedLeaderboard || cachedLeaderboard.updatedAt < leaderboard.updatedAt)
		        {
			        anyUpdate = true;
			        CachedLeaderboardUpdated?.Invoke(leaderboard);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedLeaderboardsUpdated?.Invoke();
	        }
        }
	}
}