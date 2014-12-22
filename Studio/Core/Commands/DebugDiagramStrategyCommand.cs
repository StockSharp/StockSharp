namespace StockSharp.Studio.Core.Commands
{
	using System;

    public class DebugDiagramStrategyCommand : BaseStudioCommand
	{
		public StrategyContainer Strategy { get; private set; }

        public DebugDiagramStrategyCommand(StrategyContainer strategy)
		{
			if (strategy == null) 
				throw new ArgumentNullException("strategy");

			Strategy = strategy;
		}
	}
}
