namespace StockSharp.Studio.Core.Commands
{
	public class RemoveStrategyInfoCommand : BaseStudioCommand
	{
		public RemoveStrategyInfoCommand(StrategyInfo info)
		{
			Info = info;
		}

		public StrategyInfo Info { get; private set; }
	}
}