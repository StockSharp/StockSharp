#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: ICandleManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System.Collections.Generic;

	/// <summary>
	/// The candles manager interface.
	/// </summary>
	public interface ICandleManager : ICandleSource<Candle>
	{
		/// <summary>
		/// The data container.
		/// </summary>
		ICandleManagerContainer Container { get; }

		/// <summary>
		/// All currently active candles series started via <see cref="ICandleSource{T}.Start"/>.
		/// </summary>
		IEnumerable<CandleSeries> Series { get; }

		/// <summary>
		/// Candles sources.
		/// </summary>
		IList<ICandleSource<Candle>> Sources { get; }
	}
}