namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class OpenStrategyCommand : BaseStudioCommand
	{
		public StrategyContainer Strategy { get; private set; }

		public string ContentTemplate { get; set; }

		public OpenStrategyCommand(StrategyContainer strategy, string contentTemplate = null)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Strategy = strategy;
			ContentTemplate = contentTemplate;
		}
	}
}