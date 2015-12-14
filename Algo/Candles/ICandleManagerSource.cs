#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: ICandleManagerSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	/// <summary>
	/// The candles source for <see cref="ICandleManager"/>.
	/// </summary>
	public interface ICandleManagerSource : ICandleSource<Candle>
	{
		/// <summary>
		/// The candles manager which owns this source.
		/// </summary>
		ICandleManager CandleManager { get; set; }
	}
}