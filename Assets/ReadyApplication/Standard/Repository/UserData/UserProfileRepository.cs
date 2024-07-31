using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.UserProfile;

namespace ReadyApplication.Standard
{
	public interface IUserProfileRepository
	{
        RepositoryEntityCache<string, UserData> CachedProfiles { get; }
        event System.Action<UserData> CachedProfileUpdated;
        event System.Action CachedProfilesUpdated;
		IFluentAction<UserData> Get(CancellationToken cancellationToken = default);
        IFluentAction<UserData> GetById(string id, CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }
    
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultLongTtl());
        public RepositoryEntityCache<string, UserData> CachedProfiles { get; } = new(RepositoryHelper.GetDefaultLongTtl());
        public event System.Action<UserData> CachedProfileUpdated;
        public event System.Action CachedProfilesUpdated;

		public void InvalidateCache(bool queryOnly = true)
        {
	        _queryCache.Invalidate();

	        if (!queryOnly)
	        {
		        CachedProfiles.Clear();
	        }
        }

		public IFluentAction<UserData> Get(CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(Get));
            return new FluentAction<UserData>(() => UserProfileModule.I.GetProfileAsync(cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheProfile);
        }
        
        public IFluentAction<UserData> GetById(string id, CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetById), nameof(id), id);
            return new FluentAction<UserData>(() => UserProfileModule.I.GetProfileAsync(id, cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultLongTtl())
                .OnComplete(CacheProfile);
        }

        private void CacheProfile(UserData profile)
        {
	        bool hasCachedProfile = CachedProfiles.TryGetNotExpiredValue(profile.userId, out var cachedProfile);

	        CachedProfiles[profile.userId] = profile;

	        if (!hasCachedProfile || !Comparers.UserData.Equals(cachedProfile, profile))
	        {
		        CachedProfileUpdated?.Invoke(profile);
		        CachedProfilesUpdated?.Invoke();
	        }
        }
	}
}