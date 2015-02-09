namespace StockSharp.Algo.Strategies
{
	using Ecng.Collections;

	/// <summary>
	/// Коллекция дочерних стратегий.
	/// </summary>
	public interface IStrategyChildStrategyList : INotifyList<Strategy>, ISynchronizedCollection
	{
	}
}