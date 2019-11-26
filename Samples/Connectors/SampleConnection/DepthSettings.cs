namespace SampleConnection
{
	using System;

	using StockSharp.Messages;

	public class DepthSettings
	{
		public DateTimeOffset? From { get; set; }

		public DateTimeOffset? To { get; set; }

		public int? MaxDepth { get; set; }

		public MarketDataTypes? BuildFrom { get; set; } = MarketDataTypes.OrderLog;

		public MarketDataBuildModes BuildMode { get; set; } = MarketDataBuildModes.Build;
	}
}