namespace StockSharp.Messages
{
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	/// <summary>
	/// Source for <see cref="MarketDataMessage.BuildFrom"/>.
	/// </summary>
	public class BuildCandlesFromSource : ItemsSourceBase<DataType>
	{
		/// <summary>
		/// Possible data types that can be used as candles source.
		/// </summary>
		public static IEnumerable<DataType> CandleDataSources { get; } = new[] { DataType.Level1, DataType.Ticks, DataType.MarketDepth, DataType.OrderLog };

		/// <summary>
		/// Get values.
		/// </summary>
		/// <returns>Values.</returns>
		protected override IEnumerable<DataType> GetValues() => CandleDataSources;
	}
}