namespace StockSharp.Algo.Candles.Compression
{
	using System.Collections.Generic;

	/// <summary>
	/// The data source for <see cref="ICandleBuilder"/>.
	/// </summary>
	public interface ICandleBuilderSource : ICandleSource<IEnumerable<ICandleBuilderSourceValue>>
	{
	}
}