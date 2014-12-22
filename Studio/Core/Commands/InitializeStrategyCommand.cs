namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Algo.Strategies;

	public class InitializeStrategyCommand : BaseStudioCommand
	{
		public InitializeStrategyCommand(Strategy strategy, DateTime from, DateTime to)
		{
			Strategy = strategy;
			FromDate = from;
			ToDate = to;
		}

		public Strategy Strategy { get; private set; }
		public DateTime FromDate { get; private set; }
		public DateTime ToDate { get; private set; }
	}
}