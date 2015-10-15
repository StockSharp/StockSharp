namespace StockSharp.Algo.Strategies
{
	using Ecng.Collections;

	/// <summary>
	/// The collection of subsidiary strategies.
	/// </summary>
	public interface IStrategyChildStrategyList : INotifyList<Strategy>, ISynchronizedCollection
	{
	}
}