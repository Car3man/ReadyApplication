using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.Currency;

namespace ReadyApplication.Standard
{
	public interface IUserCurrencyRepository
	{
        RepositoryEntityCache<string, Currency> CachedCurrencies { get; }
        event System.Action<Currency> CachedCurrencyUpdated;
        event System.Action CachedCurrenciesUpdate;
		IFluentAction<List<Currency>> GetAll(CancellationToken cancellationToken = default);
		void InvalidateCache(bool queryOnly = true);
    }
    
    public class UserCurrencyRepository : IUserCurrencyRepository
    {
        private readonly Cache _queryCache = new(RepositoryHelper.GetDefaultShortTtl());
        public RepositoryEntityCache<string, Currency> CachedCurrencies { get; } = new(RepositoryHelper.GetDefaultShortTtl());
        public event System.Action<Currency> CachedCurrencyUpdated;
        public event System.Action CachedCurrenciesUpdate;

		public void InvalidateCache(bool queryOnly = true)
		{
			_queryCache.Invalidate();

			if (!queryOnly)
			{
				CachedCurrencies.Clear();
			}
		}

		public IFluentAction<List<Currency>> GetAll(CancellationToken cancellationToken = default)
        {
            string queryHash = RepositoryHelper.GetQueryHash(nameof(GetAll));
            return new FluentAction<List<Currency>>(() => CurrencyModule.I.GetUserCurrenciesAsync(cancellationToken))
                .Cache(queryHash, _queryCache, RepositoryHelper.GetDefaultShortTtl())
                .OnComplete(CacheUserCurrencies);
        }

        private void CacheUserCurrencies(List<Currency> currencies)
        {
	        bool anyUpdate = false;

	        foreach (Currency currency in currencies)
	        {
				string currencyId = GetCurrencyId(currency);
				bool hasCachedCurrency = CachedCurrencies.TryGetNotExpiredValue(currencyId, out var cachedUserCurrency);

		        CachedCurrencies[currencyId] = currency;

		        if (!hasCachedCurrency || !Comparers.Currency.Equals(cachedUserCurrency, currency))
		        {
			        anyUpdate = true;
			        CachedCurrencyUpdated?.Invoke(currency);
		        }
	        }

	        if (anyUpdate)
	        {
		        CachedCurrenciesUpdate?.Invoke();
	        }
        }

        private string GetCurrencyId(Currency currency)
        {
	        return "[" + string.Join(",", currency.appIds) + "] - " + currency.name;
        }
	}
}