namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class ResetStrategyCommand : BaseStudioCommand
	{
		public StrategyContainer Strategy { get; private set; }

		public ResetStrategyCommand(StrategyContainer strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));
			Strategy = strategy;
		}
	}
}