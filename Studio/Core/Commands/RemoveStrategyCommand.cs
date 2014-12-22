namespace StockSharp.Studio.Core.Commands
{
    public class RemoveStrategyCommand : BaseStudioCommand
	{
		public RemoveStrategyCommand(StrategyContainer strategy)
		{
			Strategy = strategy;
		}

        public StrategyContainer Strategy { get; private set; }
	}
}