using System.Threading;
using ReadyApplication.Core;
using RGN.Model;
using RGN.Modules.UserProfile;

namespace ReadyApplication.Standard
{
    public interface IUserProfileService
    {
        RepositoryEntityCache<string, UserData> CachedProfiles { get; }
        event System.Action<UserData> CachedProfileUpdated;
        event System.Action CachedProfilesUpdated;
		IFluentAction UpdateDisplayNameAsync(string displayName, CancellationToken cancellationToken = default);
        IFluentAction UpdateBioAsync(string bio, CancellationToken cancellationToken = default);
        IFluentAction UpdateAvatarAsync(byte[] avatarBytes, CancellationToken cancellationToken = default);
        IFluentAction<byte[]> DownloadAvatarAsync(string userId, ImageSize size, CancellationToken cancellationToken = default);
        IFluentAction<UserData> GetUserProfile(CancellationToken cancellationToken = default);
        IFluentAction<UserData> GetUserProfileById(string id, CancellationToken cancellationToken = default);
    }

    public class UserProfileService : IUserProfileService
    {
        private readonly IReadyApp _readyApp;
        private readonly IUserProfileRepository _userProfileRepository;
        
        public RepositoryEntityCache<string, UserData> CachedProfiles => _userProfileRepository.CachedProfiles;

        public event System.Action<UserData> CachedProfileUpdated
		{
			add => _userProfileRepository.CachedProfileUpdated += value;
			remove => _userProfileRepository.CachedProfileUpdated -= value;
		}
        public event System.Action CachedProfilesUpdated
		{
			add => _userProfileRepository.CachedProfilesUpdated += value;
			remove => _userProfileRepository.CachedProfilesUpdated -= value;
		}

        public UserProfileService(IReadyApp readyApp, IUserProfileRepository userProfileRepository)
        {
            _readyApp = readyApp;
            _readyApp.UserAuthStateChanged += _ => userProfileRepository.InvalidateCache();
            _userProfileRepository = userProfileRepository;
        }
        
        public IFluentAction UpdateDisplayNameAsync(string displayName, CancellationToken cancellationToken = default)
        {
            return new FluentAction(async () =>
            {
                await UserProfileModule.I.SetDisplayNameAsync(displayName, cancellationToken);

                if (_userProfileRepository.CachedProfiles.TryGetValue(_readyApp.User.UserId, out UserData userProfile))
                {
                    userProfile.displayName = displayName;
                    _userProfileRepository.CachedProfiles[_readyApp.User.UserId] = userProfile;
                }
            });
        }

        public IFluentAction UpdateBioAsync(string bio, CancellationToken cancellationToken = default)
        {
            return new FluentAction(async () =>
            {
                await UserProfileModule.I.SetBioAsync(bio, cancellationToken);
            
                if (_userProfileRepository.CachedProfiles.TryGetValue(_readyApp.User.UserId, out UserData userProfile))
                {
                    userProfile.bio = bio;
                    _userProfileRepository.CachedProfiles[_readyApp.User.UserId] = userProfile;
                }
            });
        }

        public IFluentAction UpdateAvatarAsync(byte[] avatarBytes, CancellationToken cancellationToken = default)
        {
            return new FluentAction(() => UserProfileModule.I.UploadAvatarImageAsync(avatarBytes, cancellationToken));
        }

        public IFluentAction<byte[]> DownloadAvatarAsync(string userId, ImageSize size, CancellationToken cancellationToken = default)
        {
            return new FluentAction<byte[]>(() => UserProfileModule.I.DownloadAvatarImageAsync(userId, size, cancellationToken));
        }
        
        public IFluentAction<UserData> GetUserProfile(CancellationToken cancellationToken = default)
            => _userProfileRepository.Get(cancellationToken);
        
        public IFluentAction<UserData> GetUserProfileById(string id, CancellationToken cancellationToken = default)
            => _userProfileRepository.GetById(id, cancellationToken);
    }
}