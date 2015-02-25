namespace StockSharp.Hydra.Quandl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;

	class QuandlSecurityStorage : ISecurityStorage
	{
		private class KeyComparer : IEqualityComparer<Tuple<string, string>>
		{
			public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
			{
				return StringComparer.InvariantCultureIgnoreCase.Equals(x.Item1, y.Item1)
					   && StringComparer.InvariantCultureIgnoreCase.Equals(x.Item2, y.Item2);
			}

			public int GetHashCode(Tuple<string, string> obj)
			{
				return obj.GetHashCode();
			}
		}

		private readonly IEntityRegistry _entityRegistry;

		public QuandlSecurityStorage(IEntityRegistry entityRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException("entityRegistry");

			_entityRegistry = entityRegistry;

			foreach (var security in entityRegistry.Securities)
				TryAddToCache(security);
		}

		private readonly SynchronizedDictionary<Tuple<string, string>, Security> _cacheByQuandlId = new SynchronizedDictionary<Tuple<string, string>, Security>(new KeyComparer());

		public IEnumerable<Security> Securities
		{
			get { return _cacheByQuandlId.Values; }
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			var quandlId = criteria.ExtensionInfo == null
				? null
				: Tuple.Create((string)criteria.ExtensionInfo.TryGetValue(QuandlHistorySource.SourceCodeField), (string)criteria.ExtensionInfo.TryGetValue(QuandlHistorySource.SecurityCodeField));

			if (quandlId == null || quandlId.Item1 == null)
				return _entityRegistry.Securities.Lookup(criteria);

			var security = _cacheByQuandlId.TryGetValue(quandlId);
			return security == null ? Enumerable.Empty<Security>() : new[] { security };
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return _cacheByQuandlId.SyncGet(d => d.FirstOrDefault(p => p.Value == security).Key);
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

			var sourceCode = (string)security.ExtensionInfo.TryGetValue(QuandlHistorySource.SourceCodeField);
			var secCode = (string)security.ExtensionInfo.TryGetValue(QuandlHistorySource.SecurityCodeField);

			if (sourceCode != null && secCode != null)
				_cacheByQuandlId.SafeAdd(Tuple.Create(sourceCode, secCode), key => security);
		}
	}
}