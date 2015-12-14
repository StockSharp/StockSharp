#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: DiMinus.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// DIMinus is a component of the Directional Movement System developed by Welles Wilder.
	/// </summary>
	public class DiMinus : DiPart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiMinus"/>.
		/// </summary>
		public DiMinus()
		{
		}

		/// <summary>
		/// To get the part value.
		/// </summary>
		/// <param name="current">The current candle.</param>
		/// <param name="prev">The previous candle.</param>
		/// <returns>Value.</returns>
		protected override decimal GetValue(Candle current, Candle prev)
		{
			if (current.LowPrice < prev.LowPrice && current.HighPrice - prev.HighPrice < prev.LowPrice - current.LowPrice)
				return prev.LowPrice - current.LowPrice;
			else
				return 0;
		}
	}
}