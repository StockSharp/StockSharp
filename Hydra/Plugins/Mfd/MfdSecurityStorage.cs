namespace StockSharp.Hydra.Mfd
{
	using System;

	using Ecng.Collections;

	using StockSharp.Algo.History.Russian;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;

	class MfdSecurityStorage : NativeIdSecurityStorage<string>
	{
		public MfdSecurityStorage(IEntityRegistry entityRegistry)
			: base(entityRegistry, StringComparer.InvariantCultureIgnoreCase)
		{
		}

		protected override string CreateNativeId(Security security)
		{
			return (string)security.ExtensionInfo.TryGetValue(MfdHistorySource.SecurityIdField);
		}
	}
}