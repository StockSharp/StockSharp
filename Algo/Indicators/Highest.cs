#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Highest.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using System;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Maximum value for a period.
	/// </summary>
	[DisplayName("Highest")]
	[DescriptionLoc(LocalizedStrings.Str733Key)]
	public class Highest : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Highest"/>.
		/// </summary>
		public Highest()
		{
			Length = 5;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.IsSupport(typeof(Candle)) ? input.GetValue<Candle>().HighPrice : input.GetValue<decimal>();

			var lastValue = Buffer.Count == 0 ? newValue : this.GetCurrentValue();

			// добавляем новое начало
			if (input.IsFinal)
				Buffer.Add(newValue);

			if (newValue > lastValue)
			{
				// Новое значение и есть экстремум 
				lastValue = newValue;
			}

			if (Buffer.Count > Length)
			{
				var first = Buffer[0];

				// удаляем хвостовое значение
				if (input.IsFinal)
					Buffer.RemoveAt(0);

				// удаляется экстремум, для поиска нового значения необходим проход по всему буфферу
				if (first == lastValue && lastValue != newValue)
				{
					// ищем новый экстремум
					lastValue = Buffer.Aggregate(newValue, (current, t) => Math.Max(t, current));
				}
			}

			return new DecimalIndicatorValue(this, lastValue);
		}
	}
}