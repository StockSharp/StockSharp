namespace StockSharp.Studio.Core.Commands
{
	using StockSharp.BusinessEntities;

	public class ClearMarketDepthCommand : BaseStudioCommand
	{
		public Security Security { get; private set; }

		public ClearMarketDepthCommand(Security security)
		{
			Security = security;
		}
	}
}