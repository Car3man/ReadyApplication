using System;
using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Achievement;

namespace ReadyApplication.Standard
{
    public interface IAchievementService
    {
        RepositoryEntityCache<string, AchievementData> CachedAchievements { get; }
        RepositoryEntityCache<string, UserAchievement> CachedUserAchievements { get; }
        event System.Action<AchievementData> CachedAchievementUpdated;
        event System.Action<UserAchievement> CachedUserAchievementUpdated;
        event System.Action CachedAchievementsUpdated;
        event System.Action CachedUserAchievementsUpdated;
		IFluentAction<TriggerAndClaimResponse> TriggerAchievementById(string id, int progress = 1, CancellationToken cancellationToken = default);
        IFluentAction<TriggerAndClaimResponse> TriggerAchievementByRequestName(string requestName, int progress = 1, CancellationToken cancellationToken = default);
        IFluentAction<TriggerAndClaimResponse> ClaimAchievementById(string id, CancellationToken cancellationToken = default);
        IFluentAction<TriggerAndClaimResponse> ClaimAchievementByRequestName(string requestName, CancellationToken cancellationToken = default);
        IFluentAction<UserAchievement> GetUserAchievementById(string id, bool withHistory = false, CancellationToken cancellationToken = default);
        IFluentAction<List<UserAchievement>> GetUserAchievements(int limit, long startAfter = long.MaxValue, bool withHistory = false, CancellationToken cancellationToken = default);
        IFluentAction<List<AchievementData>> GetAchievementsByIds(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<AchievementData>> GetAchievementsByIds(List<string> ids, CancellationToken cancellationToken = default);
        IFluentAction<List<AchievementData>> GetAchievementsByTags(string tag, int limit, string startAfter = "", CancellationToken cancellationToken = default);
        IFluentAction<List<AchievementData>> GetAchievementsByTags(List<string> tags, int limit, string startAfter = "", CancellationToken cancellationToken = default);
        IFluentAction<List<AchievementData>> GetAchievementsByAppIds(string appId, int limit, string startAfter = "", CancellationToken cancellationToken = default);
        IFluentAction<List<AchievementData>> GetAchievementsByAppIds(List<string> appIds, int limit, string startAfter = "", CancellationToken cancellationToken = default);
        IFluentAction<List<AchievementData>> GetAchievementsForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default);
    }

    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _achievementRepository;
        private readonly IUserAchievementRepository _userAchievementRepository;

        public RepositoryEntityCache<string, AchievementData> CachedAchievements => _achievementRepository.CachedAchievements;
        public RepositoryEntityCache<string, UserAchievement> CachedUserAchievements => _userAchievementRepository.CachedAchievements;

        public event Action<AchievementData> CachedAchievementUpdated
        {
            add => _achievementRepository.CachedAchievementUpdated += value;
            remove => _achievementRepository.CachedAchievementUpdated -= value;
        }
        public event Action<UserAchievement> CachedUserAchievementUpdated
        {
            add => _userAchievementRepository.CachedAchievementUpdated += value;
			remove => _userAchievementRepository.CachedAchievementUpdated -= value;
        }
        public event Action CachedAchievementsUpdated
        { 
	        add => _achievementRepository.CachedAchievementsUpdated += value;
			remove => _achievementRepository.CachedAchievementsUpdated -= value;
        }
        public event Action CachedUserAchievementsUpdated
        {
            add => _userAchievementRepository.CachedAchievementsUpdated += value;
			remove => _userAchievementRepository.CachedAchievementsUpdated -= value;
        }

        public AchievementService(IReadyApp readyApp, IAchievementRepository achievementRepository, IUserAchievementRepository userAchievementRepository)
        {
            _achievementRepository = achievementRepository;
            _userAchievementRepository = userAchievementRepository;
            readyApp.UserAuthStateChanged += _ => userAchievementRepository.InvalidateCache();
        }

        public IFluentAction<TriggerAndClaimResponse> TriggerAchievementById(string id, int progress = 1, CancellationToken cancellationToken = default)
        {
            return new FluentAction<TriggerAndClaimResponse>(async () =>
            {
                var triggerResult = await AchievementsModule.I.TriggerByIdAsync(id, progress, cancellationToken);
                _userAchievementRepository.InvalidateCache();
                return triggerResult;
            });
        }

        public IFluentAction<TriggerAndClaimResponse> TriggerAchievementByRequestName(string requestName, int progress = 1, CancellationToken cancellationToken = default)
        {
            return new FluentAction<TriggerAndClaimResponse>(async () =>
            {
                var triggerResult = await AchievementsModule.I.TriggerByRequestNameAsync(requestName, progress, cancellationToken);
                _userAchievementRepository.InvalidateCache();
                return triggerResult;
            });
        }

        public IFluentAction<TriggerAndClaimResponse> ClaimAchievementById(string id, CancellationToken cancellationToken = default)
        {
            return new FluentAction<TriggerAndClaimResponse>(async () =>
            {
                var claimResult = await AchievementsModule.I.ClaimByIdAsync(id, cancellationToken);
                _userAchievementRepository.InvalidateCache();
                return claimResult;
            });
        }

        public IFluentAction<TriggerAndClaimResponse> ClaimAchievementByRequestName(string requestName, CancellationToken cancellationToken = default)
        {
            return new FluentAction<TriggerAndClaimResponse>(async () =>
            {
                var claimResult = await AchievementsModule.I.ClaimByRequestNameAsync(requestName, cancellationToken);
                _userAchievementRepository.InvalidateCache();
                return claimResult;
            });
        }
        
        public IFluentAction<UserAchievement> GetUserAchievementById(string id, bool withHistory = false, CancellationToken cancellationToken = default)
            => _userAchievementRepository.GetById(id, withHistory, cancellationToken);

        public IFluentAction<List<UserAchievement>> GetUserAchievements(int limit, long startAfter = long.MaxValue, bool withHistory = false, CancellationToken cancellationToken = default)
            => _userAchievementRepository.GetAll(limit, startAfter, withHistory, cancellationToken);

        public IFluentAction<List<AchievementData>> GetAchievementsByIds(string id, CancellationToken cancellationToken = default)
	        => _achievementRepository.GetByIds(id, cancellationToken);

		public IFluentAction<List<AchievementData>> GetAchievementsByIds(List<string> ids, CancellationToken cancellationToken = default)
            => _achievementRepository.GetByIds(ids, cancellationToken);

		public IFluentAction<List<AchievementData>> GetAchievementsByTags(string tag, int limit, string startAfter = "",
			CancellationToken cancellationToken = default)
			=> _achievementRepository.GetByTags(tag, limit, startAfter, cancellationToken);

        public IFluentAction<List<AchievementData>> GetAchievementsByTags(List<string> tags, int limit, string startAfter = "", CancellationToken cancellationToken = default)
            => _achievementRepository.GetByTags(tags, limit, startAfter, cancellationToken);

        public IFluentAction<List<AchievementData>> GetAchievementsByAppIds(string appId, int limit, string startAfter = "",
	        CancellationToken cancellationToken = default)
	        => _achievementRepository.GetByAppIds(appId, limit, startAfter, cancellationToken);

		public IFluentAction<List<AchievementData>> GetAchievementsByAppIds(List<string> appIds, int limit, string startAfter = "", CancellationToken cancellationToken = default)
            => _achievementRepository.GetByAppIds(appIds, limit, startAfter, cancellationToken);

        public IFluentAction<List<AchievementData>> GetAchievementsForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default)
            => _achievementRepository.GetForThisApp(limit, startAfter, cancellationToken);
    }
}