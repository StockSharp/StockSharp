namespace StockSharp.Studio.Core.Commands
{
	public class OpenStrategyInfoCommand : BaseStudioCommand
	{
		public OpenStrategyInfoCommand(StrategyInfo info)
		{
			Info = info;
		}

		public StrategyInfo Info { get; private set; }
	}
}