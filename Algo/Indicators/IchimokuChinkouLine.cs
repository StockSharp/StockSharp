#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IchimokuChinkouLine.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// Chinkou line.
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class IchimokuChinkouLine : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuChinkouLine"/>.
		/// </summary>
		public IchimokuChinkouLine()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<Candle>().ClosePrice;

			if (Buffer.Count > Length)
				Buffer.RemoveAt(0);

			if (input.IsFinal)
				Buffer.Add(price);

			return new DecimalIndicatorValue(this, price);
		}
	}
}