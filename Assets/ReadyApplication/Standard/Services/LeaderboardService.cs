using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Leaderboard;

namespace ReadyApplication.Standard
{
    public interface ILeaderboardService
    {
	    RepositoryEntityCache<string, LeaderboardData> CachedLeaderboards { get; }
		event System.Action<LeaderboardData> CachedLeaderboardUpdated;
	    event System.Action CachedLeaderboardsUpdated;
		IFluentAction<IsLeaderboardAvailableResponseData> IsLeaderboardAvailable(string id, CancellationToken cancellationToken = default);
        IFluentAction<int> AddScoreToLeaderboard(string id, int score, string extraData = "", CancellationToken cancellationToken = default);
        IFluentAction<int> SetScoreToLeaderboard(string id, int score, string extraData = "", CancellationToken cancellationToken = default);
        IFluentAction<LeaderboardEntry> GetLeaderboardUserEntry(string leaderboardId, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardEntry>> GetLeaderboardEntries(string leaderboardId, int quantityTop, bool includeUser, int quantityAroundUser, CancellationToken cancellationToken = default);
        IFluentAction<LeaderboardData> GetLeaderboardById(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardData>> GetLeaderboardsByIds(List<string> ids, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<LeaderboardData> GetLeaderboardByRequestName(string requestName, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardData>> GetLeaderboardsByRequestNames(List<string> requestNames, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardData>> GetLeaderboardsByTags(string tag, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardData>> GetLeaderboardsByTags(List<string> tags, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardData>> GetLeaderboardsByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardData>> GetLeaderboardsByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
        IFluentAction<List<LeaderboardData>> GetLeaderboardsForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default);
    }

    public class LeaderboardService : ILeaderboardService
    {
        private readonly ICache _queryCache = new Cache(RepositoryHelper.GetDefaultLongTtl());
        private readonly ILeaderboardRepository _leaderboardRepository;

        public RepositoryEntityCache<string, LeaderboardData> CachedLeaderboards => _leaderboardRepository.CachedLeaderboards;

        public event System.Action<LeaderboardData> CachedLeaderboardUpdated
		{
			add => _leaderboardRepository.CachedLeaderboardUpdated += value;
			remove => _leaderboardRepository.CachedLeaderboardUpdated -= value;
		}
        public event System.Action CachedLeaderboardsUpdated
        {
            add => _leaderboardRepository.CachedLeaderboardsUpdated += value;
            remove => _leaderboardRepository.CachedLeaderboardsUpdated -= value;
        }

        public LeaderboardService(IReadyApp readyApp, ILeaderboardRepository leaderboardRepository)
        {
            _leaderboardRepository = leaderboardRepository;
            readyApp.UserAuthStateChanged += _ => _queryCache.Invalidate();
        }
        
        public IFluentAction<IsLeaderboardAvailableResponseData> IsLeaderboardAvailable(string id, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(IsLeaderboardAvailable), nameof(id), id);
            return new FluentAction<IsLeaderboardAvailableResponseData>(() => LeaderboardModule.I.IsLeaderboardAvailableAsync(id, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl());
        }

        public IFluentAction<int> AddScoreToLeaderboard(string id, int score, string extraData = "",
            CancellationToken cancellationToken = default)
        {
            return new FluentAction<int>(async () =>
            {
                int place = await LeaderboardModule.I.AddScoreAsync(id, score, extraData, cancellationToken);
                _queryCache.Invalidate();
                return place;
            });
        }

        public IFluentAction<int> SetScoreToLeaderboard(string id, int score, string extraData = "",
            CancellationToken cancellationToken = default)
        {
            return new FluentAction<int>(async () =>
            {
                int place = await LeaderboardModule.I.SetScoreAsync(id, score, extraData, cancellationToken);
                _queryCache.Invalidate();
                return place;
            });
        }

        public IFluentAction<LeaderboardEntry> GetLeaderboardUserEntry(string leaderboardId, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetLeaderboardUserEntry), nameof(leaderboardId), leaderboardId);
            return new FluentAction<LeaderboardEntry>(() => LeaderboardModule.I.GetUserEntryAsync(leaderboardId, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl());
        }

        public IFluentAction<List<LeaderboardEntry>> GetLeaderboardEntries(string leaderboardId, int quantityTop, bool includeUser, int quantityAroundUser,
            CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetLeaderboardEntries), nameof(leaderboardId), leaderboardId, 
                nameof(quantityTop), quantityTop, 
                nameof(includeUser), includeUser,
                nameof(quantityAroundUser), quantityAroundUser
            );
            return new FluentAction<List<LeaderboardEntry>>(() => LeaderboardModule.I.GetEntriesAsync(leaderboardId, quantityTop, includeUser, quantityAroundUser, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl());
        }

        public IFluentAction<LeaderboardData> GetLeaderboardById(string id, CancellationToken cancellationToken = default)
            => _leaderboardRepository.GetById(id, cancellationToken);

        public IFluentAction<List<LeaderboardData>> GetLeaderboardsByIds(List<string> ids, int limit, long startAfter = default, bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
	        => _leaderboardRepository.GetByIds(ids, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<LeaderboardData> GetLeaderboardByRequestName(string requestName, CancellationToken cancellationToken = default)
            => _leaderboardRepository.GetByRequestName(requestName, cancellationToken);

        public IFluentAction<List<LeaderboardData>> GetLeaderboardsByRequestNames(List<string> requestNames, int limit, long startAfter = default, bool ignoreTimestamp = false, CancellationToken cancellationToken = default)
            => _leaderboardRepository.GetByRequestNames(requestNames, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<LeaderboardData>> GetLeaderboardsByTags(string tag, int limit, long startAfter = default, bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
			=> _leaderboardRepository.GetByTags(tag, limit, startAfter, ignoreTimestamp, cancellationToken);

		public IFluentAction<List<LeaderboardData>> GetLeaderboardsByTags(List<string> tags, int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
            => _leaderboardRepository.GetByTags(tags, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<LeaderboardData>> GetLeaderboardsByAppIds(string appId, int limit, long startAfter = default, bool ignoreTimestamp = false,
	        CancellationToken cancellationToken = default)
			=> _leaderboardRepository.GetByAppIds(appId, limit, startAfter, ignoreTimestamp, cancellationToken);

		public IFluentAction<List<LeaderboardData>> GetLeaderboardsByAppIds(List<string> appIds, int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
            => _leaderboardRepository.GetByAppIds(appIds, limit, startAfter, ignoreTimestamp, cancellationToken);

        public IFluentAction<List<LeaderboardData>> GetLeaderboardsForThisApp(int limit, long startAfter = default, bool ignoreTimestamp = false,
            CancellationToken cancellationToken = default)
            => _leaderboardRepository.GetForThisApp(limit, startAfter, ignoreTimestamp, cancellationToken);
    }
}