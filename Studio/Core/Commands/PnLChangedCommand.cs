namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class PnLChangedCommand : BaseStudioCommand
	{
		public PnLChangedCommand(DateTimeOffset time, decimal totalPnL, decimal unrealizedPnL, decimal? commission)
		{
			Time = time;
			TotalPnL = totalPnL;
			UnrealizedPnL = unrealizedPnL;
			Commission = commission;
		}

		public decimal TotalPnL { get; private set; }
		public decimal UnrealizedPnL { get; private set; }
		public decimal? Commission { get; set; }
		public DateTimeOffset Time { get; private set; }
	}
}