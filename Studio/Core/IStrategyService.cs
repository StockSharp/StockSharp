namespace StockSharp.Studio.Core
{
	using StockSharp.Algo.Strategies;

	public interface IStrategyService
	{
		IStudioConnector Connector { get; }
		void InitStrategy(Strategy strategy);
	}
}