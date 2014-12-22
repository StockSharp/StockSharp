namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class AddStrategyCommand : BaseStudioCommand
	{
		public StrategyInfo Info { get; private set; }

		public StrategyContainer Strategy { get; set; }

		public SessionType SessionType { get; private set; }

		public AddStrategyCommand(StrategyInfo info, SessionType sessionType)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			Info = info;
			SessionType = sessionType;
		}

		public AddStrategyCommand(StrategyInfo info, StrategyContainer strategy, SessionType sessionType)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (strategy == null)
				throw new ArgumentNullException("strategy");

			Info = info;
			Strategy = strategy;
			SessionType = sessionType;
		}
	}
}