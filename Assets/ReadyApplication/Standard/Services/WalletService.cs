using System.Threading;
using System.Threading.Tasks;
using ReadyApplication.Core;
using RGN.Modules.Wallets;

namespace ReadyApplication.Standard
{
    public interface IWalletService
    {
        IFluentAction<bool> CreateWallet(CancellationToken cancellationToken = default);
        IFluentAction<bool> HasUserRequiredWallets(CancellationToken cancellationToken = default);
    }

    public class WalletService : IWalletService
    {
        private CacheValue<bool> _hasUserRequiredWallets;
        
        public WalletService(IReadyApp readyApp)
        {
            readyApp.UserAuthStateChanged += _ => _hasUserRequiredWallets = new CacheValue<bool>();
        }

        public IFluentAction<bool> CreateWallet(CancellationToken cancellationToken = default)
        {
            return new FluentAction<bool>(async () =>
            {
                TaskCompletionSource<bool> createWalletCompletionTask = new TaskCompletionSource<bool>();
                try
                {
                    WalletsModule.I.CreateWallet(success => createWalletCompletionTask.SetResult(success));
                }
                catch (System.Exception exception)
                {
                    createWalletCompletionTask.SetException(exception);
                }
                bool result = await createWalletCompletionTask.Task;
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            });
        }

        public IFluentAction<bool> HasUserRequiredWallets(CancellationToken cancellationToken = default)
        {
            return new FluentAction<bool>(() => WalletsModule.I.IsUserHasBlockchainRequirementAsync(cancellationToken))
                .Cache((out bool value) =>
                    {
                        if (_hasUserRequiredWallets.HasValue)
                        {
                            value = _hasUserRequiredWallets.Value;
                            return true;
                        }
                        value = default;
                        return false;
                    },
                    (value, ttl) =>
                    {
                        if (value)
                        {
                            _hasUserRequiredWallets = new CacheValue<bool>(true, ttl);
                        }
                    }, System.TimeSpan.MaxValue
                );
        }
    }
}