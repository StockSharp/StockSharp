namespace StockSharp.Studio.Core
{
	using System;
	using System.ComponentModel;

	using StockSharp.Algo.Strategies;

	public interface IStrategyContainer : ICustomTypeDescriptor
	{
		StrategyInfo StrategyInfo { get; }
		Strategy Strategy { get; }
		event Action<Strategy> StrategyRemoved;
		event Action<Strategy> StrategyAssigned;
	}
}