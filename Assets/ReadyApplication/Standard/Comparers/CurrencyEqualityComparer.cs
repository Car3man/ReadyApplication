using System.Collections.Generic;
using RGN.Modules.Currency;

namespace ReadyApplication.Standard
{
	public sealed class CurrencyEqualityComparer : IEqualityComparer<Currency>
	{
		public bool Equals(Currency x, Currency y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (ReferenceEquals(x, null))
			{
				return false;
			}

			if (ReferenceEquals(y, null))
			{
				return false;
			}

			if (x.GetType() != y.GetType())
			{
				return false;
			}

			return Equals(x.appIds, y.appIds) && x.name == y.name && x.quantity == y.quantity;
		}

		public int GetHashCode(Currency obj)
		{
			unchecked
			{
				int hashCode = (obj.appIds != null ? obj.appIds.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (obj.name != null ? obj.name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ obj.quantity;
				return hashCode;
			}
		}
	}
}