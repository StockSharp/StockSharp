namespace StockSharp.Studio.Core
{
	using System.Collections.Generic;

	using Ecng.Serialization;

	using StockSharp.Algo.Storages;

	public interface IStudioEntityRegistry : IEntityRegistry
	{
		IStorage Storage { get; }

		IStrategyInfoList Strategies { get; }
		SessionList Sessions { get; }

		IStrategyInfoList GetStrategyInfoList(Session session);
	}

	public interface IStrategyInfoList : IStorageEntityList<StrategyInfo>
	{
		IEnumerable<StrategyInfo> ReadByType(StrategyInfoTypes type);
	}
}