namespace SampleConnection
{
	using System;

	using StockSharp.Messages;

	public class DepthSettings
	{
		public DateTimeOffset? From { get; set; }

		public DateTimeOffset? To { get; set; }

		public int? MaxDepth { get; set; }

		public DataType BuildFrom { get; set; } = DataType.OrderLog;

		public MarketDataBuildModes BuildMode { get; set; } = MarketDataBuildModes.Build;
	}
}