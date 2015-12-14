#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: BaseCandleBuilderSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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