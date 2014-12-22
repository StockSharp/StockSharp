namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class StartStrategyCommand : BaseStudioCommand
	{
		public StrategyContainer Strategy { get; private set; }

		public DateTime? StartDate { get; private set; }

		public DateTime? StopDate { get; private set; }

		public TimeSpan? CandlesTimeFrame { get; private set; }

		public bool OnlyInitialize { get; private set; }

		public bool Step { get; private set; }

        public StartStrategyCommand(StrategyContainer strategy, bool step = false)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			Strategy = strategy;
			Step = step;
		}

		public StartStrategyCommand(StrategyContainer strategy, DateTime startDate, DateTime stopDate, TimeSpan? candlesTimeFrame, bool onlyInitialize)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			Strategy = strategy;
			StartDate = startDate;
			StopDate = stopDate;
			CandlesTimeFrame = candlesTimeFrame;
			OnlyInitialize = onlyInitialize;
		}
	}

	public class StopStrategyCommand : BaseStudioCommand
	{
        public StrategyContainer Strategy { get; private set; }

        public StopStrategyCommand(StrategyContainer strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			Strategy = strategy;
		}
	}
}
