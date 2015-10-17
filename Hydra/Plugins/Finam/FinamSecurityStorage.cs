namespace StockSharp.Hydra.Finam
{
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Algo.History.Russian.Finam;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;

	class FinamSecurityStorage : NativeIdSecurityStorage<long>
	{
		public FinamSecurityStorage(IEntityRegistry entityRegistry)
			: base(entityRegistry, EqualityComparer<long>.Default)
		{
		}

		protected override long CreateNativeId(Security security)
		{
			return (long?)security.ExtensionInfo.TryGetValue(FinamHistorySource.SecurityIdField) ?? 0;
		}
	}
}