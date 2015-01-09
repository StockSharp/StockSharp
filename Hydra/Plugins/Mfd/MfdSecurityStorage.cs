namespace StockSharp.Hydra.Mfd
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.History.Russian;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;

	class MfdSecurityStorage : ISecurityStorage
	{
		private readonly IEntityRegistry _entityRegistry;

		public MfdSecurityStorage(IEntityRegistry entityRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException("entityRegistry");

			_entityRegistry = entityRegistry;

			foreach (var security in entityRegistry.Securities)
				TryAddToCache(security);
		}

		private readonly SynchronizedDictionary<string, Security> _cacheByMfdId = new SynchronizedDictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);

		public IEnumerable<Security> Securities
		{
			get { return _cacheByMfdId.Values; }
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			var mfdId = criteria.ExtensionInfo == null
				? null
				: (string)criteria.ExtensionInfo.TryGetValue(MfdHistorySource.SecurityIdField);

			if (mfdId == null)
				return _entityRegistry.Securities.Lookup(criteria);

			var security = _cacheByMfdId.TryGetValue(mfdId);
			return security == null ? Enumerable.Empty<Security>() : new[] { security };
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return _cacheByMfdId.SyncGet(d => d.FirstOrDefault(p => p.Value == security).Key);
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

			var mfdId = (string)security.ExtensionInfo.TryGetValue(MfdHistorySource.SecurityIdField);

			if (mfdId != null)
				_cacheByMfdId.SafeAdd(mfdId, key => security);
		}
	}
}
