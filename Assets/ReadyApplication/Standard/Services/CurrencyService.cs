using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Currency;

namespace ReadyApplication.Standard
{
    public interface ICurrencyService
    {
        RepositoryEntityCache<string, Currency> CachedUserCurrencies { get; }
        event System.Action<Currency> CachedUserCurrencyUpdated;
        event System.Action CachedUserCurrenciesUpdate;
		IFluentAction<int> GetUserCurrency(string currencyName, CancellationToken cancellationToken = default);
        IFluentAction<List<Currency>> GetUserCurrencies(CancellationToken cancellationToken = default);
        IFluentAction<List<Currency>> AddUserCurrency(string currencyName, int amount, CancellationToken cancellationToken = default);
        IFluentAction<List<Currency>> AddUserCurrencies(List<Currency> currencies, CancellationToken cancellationToken = default);
        IFluentAction<List<Currency>> BuyREADYggCoin(string iapUuid, string iapTransactionId, string iapReceipt, CancellationToken cancellationToken = default);
	}

    public class CurrencyService : ICurrencyService
    {
        private readonly IUserCurrencyRepository _userCurrencyRepository;
        
        public RepositoryEntityCache<string, Currency> CachedUserCurrencies => _userCurrencyRepository.CachedCurrencies;

        public event System.Action<Currency> CachedUserCurrencyUpdated
		{
			add => _userCurrencyRepository.CachedCurrencyUpdated += value;
			remove => _userCurrencyRepository.CachedCurrencyUpdated -= value;
		}
        public event System.Action CachedUserCurrenciesUpdate
        {
            add => _userCurrencyRepository.CachedCurrenciesUpdate += value;
            remove => _userCurrencyRepository.CachedCurrenciesUpdate -= value;
        }

        public CurrencyService(IReadyApp readyApp, IUserCurrencyRepository userCurrencyRepository)
        {
            _userCurrencyRepository = userCurrencyRepository;
            readyApp.UserAuthStateChanged += _ => _userCurrencyRepository.InvalidateCache();
        }
        
        public IFluentAction<int> GetUserCurrency(string currencyName, CancellationToken cancellationToken = default)
        {
            return new FluentAction<int>(async () =>
            {
                if (CachedUserCurrencies.ContainsKey(currencyName) && !CachedUserCurrencies.HasMissedOrExpired(currencyName))
                {
                    return CachedUserCurrencies[currencyName].quantity;
                }
                await _userCurrencyRepository.GetAll(cancellationToken);
                return CachedUserCurrencies.ContainsKey(currencyName) ? CachedUserCurrencies[currencyName].quantity : 0;
            });
        }

        public IFluentAction<List<Currency>> GetUserCurrencies(CancellationToken cancellationToken = default)
            => _userCurrencyRepository.GetAll(cancellationToken);

        public IFluentAction<List<Currency>> AddUserCurrency(string currencyName, int amount, CancellationToken cancellationToken = default)
        {
            return AddUserCurrencies(new List<Currency>
            {
                new()
                {
                    name = currencyName,
                    quantity = amount
                }
            }, cancellationToken);
        }

        public IFluentAction<List<Currency>> AddUserCurrencies(List<Currency> currencies, CancellationToken cancellationToken = default)
        {
            return new FluentAction<List<Currency>>(async () =>
            {
                List<Currency> updatedCurrencies = await CurrencyModule.I.AddUserCurrenciesAsync(currencies, cancellationToken);
                _userCurrencyRepository.InvalidateCache();
                return updatedCurrencies;
            });
        }

        public IFluentAction<List<Currency>> BuyREADYggCoin(string iapUuid, string iapTransactionId, string iapReceipt,
	        CancellationToken cancellationToken = default)
        {
	        return new FluentAction<List<Currency>>(async () =>
	        {
		        List<Currency> updatedCurrencies = await CurrencyModule.I.PurchaseRGNCoinAsync(iapUuid, iapTransactionId, iapReceipt, cancellationToken);
		        _userCurrencyRepository.InvalidateCache();
		        return updatedCurrencies;
	        });
        }
	}
}