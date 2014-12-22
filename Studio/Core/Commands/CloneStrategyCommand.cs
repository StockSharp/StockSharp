namespace StockSharp.Studio.Core.Commands
{
    public class CloneStrategyCommand : BaseStudioCommand
	{
		public CloneStrategyCommand(StrategyContainer strategy)
		{
			Strategy = strategy;
		}

        public StrategyContainer Strategy { get; private set; }
	}
}