namespace StockSharp.Algo.Candles.Compression
{
	using System.Collections.Generic;

	/// <summary>
	/// Источник данных для <see cref="ICandleBuilder"/>.
	/// </summary>
	public interface ICandleBuilderSource : ICandleSource<IEnumerable<ICandleBuilderSourceValue>>
	{
	}
}