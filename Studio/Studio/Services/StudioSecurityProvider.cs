namespace StockSharp.Studio.Services
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Configuration;

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
			if (criteria == null)
				throw new ArgumentNullException("criteria");

			if (criteria.Code == "*")
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
	}
}