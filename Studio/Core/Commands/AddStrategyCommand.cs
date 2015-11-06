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
				throw new ArgumentNullException(nameof(info));

			Info = info;
			SessionType = sessionType;
		}

		public AddStrategyCommand(StrategyInfo info, StrategyContainer strategy, SessionType sessionType)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Info = info;
			Strategy = strategy;
			SessionType = sessionType;
		}
	}
}