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
		/// Get values.
		/// </summary>
		/// <returns>Values.</returns>
		protected override IEnumerable<DataType> GetValues() => DataType.CandleSources;
	}
}