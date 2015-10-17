namespace StockSharp.Studio.Services
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Configuration;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core;

	sealed class StudioSecurityProvider : ISecurityProvider
	{
		private readonly CachedSynchronizedDictionary<string, Security> _securities = new CachedSynchronizedDictionary<string, Security>();

		private readonly IStudioEntityRegistry _registry;

		public StudioSecurityProvider()
		{
			_registry = ConfigManager.GetService<IStudioEntityRegistry>();
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			if (criteria.IsLookupAll())
				return _securities.CachedValues;

			return _registry
				.Securities
				.Lookup(criteria)
				.Select(security => _securities.SafeAdd(security.Id, id => security))
				.ToArray();
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		int ISecurityProvider.Count
		{
			get { return _securities.Count; }
		}

		event Action<Security> ISecurityProvider.Added
		{
			add { }
			remove { }
		}

		event Action<Security> ISecurityProvider.Removed
		{
			add { }
			remove { }
		}

		event Action ISecurityProvider.Cleared
		{
			add { }
			remove { }
		}

		void IDisposable.Dispose()
		{
		}
	}
}