namespace StockSharp.Algo.Candles.Compression
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// The base data source for <see cref="ICandleBuilder"/>.
	/// </summary>
	public abstract class BaseCandleBuilderSource : BaseCandleSource<IEnumerable<ICandleBuilderSourceValue>>, ICandleBuilderSource
	{
		/// <summary>
		/// Initialize <see cref="BaseCandleBuilderSource"/>.
		/// </summary>
		protected BaseCandleBuilderSource()
		{
		}

		/// <summary>
		/// To call the event <see cref="BaseCandleSource{TValue}.Processing"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="values">New data.</param>
		protected override void RaiseProcessing(CandleSeries series, IEnumerable<ICandleBuilderSourceValue> values)
		{
			base.RaiseProcessing(series, values.Where(v => series.CheckTime(v.Time)));
		}
	}
}