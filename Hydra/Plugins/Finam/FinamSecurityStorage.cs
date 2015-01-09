namespace StockSharp.Hydra.Finam
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.History.Russian.Finam;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;

	class FinamSecurityStorage : ISecurityStorage
	{
		private readonly IEntityRegistry _entityRegistry;
		
		public FinamSecurityStorage(IEntityRegistry entityRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException("entityRegistry");

			_entityRegistry = entityRegistry;

			foreach (var security in entityRegistry.Securities)
				TryAddToCache(security);
		}

		private readonly SynchronizedDictionary<long, Security> _cacheByFinamId = new SynchronizedDictionary<long, Security>();

		public IEnumerable<Security> Securities
		{
			get { return _cacheByFinamId.Values; }
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			var finamId = criteria.ExtensionInfo == null
				? null
				: (long?)criteria.ExtensionInfo.TryGetValue(FinamHistorySource.SecurityIdField);

			if (finamId == null)
				return _entityRegistry.Securities.Lookup(criteria);

			var security = _cacheByFinamId.TryGetValue(finamId.Value);
			return security == null ? Enumerable.Empty<Security>() : new[] { security };
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			var finamId = _cacheByFinamId.SyncGet(d => d.FirstOrDefault(p => p.Value == security).Key);
			return finamId.IsDefault() ? null : (object)finamId;
		}

		void ISecurityStorage.Save(Security security)
		{
			_entityRegistry.Securities.Save(security);
			TryAddToCache(security);
		}

		IEnumerable<string> ISecurityStorage.GetSecurityIds()
		{
			return _entityRegistry.Securities.GetSecurityIds();
		}

		private void TryAddToCache(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var finamId = security.ExtensionInfo.TryGetValue(FinamHistorySource.SecurityIdField);

			if (finamId != null)
				_cacheByFinamId.SafeAdd((long)finamId, key => security);
		}
	}
}
