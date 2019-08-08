#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: ExponentialMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Localization;

	/// <summary>
	/// Exponential Moving Average.
	/// </summary>
	[DisplayName("EMA")]
	[DescriptionLoc(LocalizedStrings.Str785Key)]
	public class ExponentialMovingAverage : LengthIndicator<decimal>
	{
		private decimal _prevFinalValue;
		private decimal _multiplier = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExponentialMovingAverage"/>.
		/// </summary>
		public ExponentialMovingAverage()
		{
			Length = 32;
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();
			_multiplier = 2m / (Length + 1);
			_prevFinalValue = 0;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			// буфер нужен только для формирования начального значение - SMA
			if (!IsFormed)
			{
				// пока sma не сформирована, возвращаем или "недоделанную" sma из финальных значенией
				// или "недоделанную" sma c пропущенным первым значением из буфера + промежуточное значение
				if (input.IsFinal)
				{
					Buffer.Add(newValue);

					_prevFinalValue = Buffer.Sum() / Length;

					return new DecimalIndicatorValue(this, _prevFinalValue);
				}
				else
				{
					return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);
				}
			}
			else
			{
				// если sma сформирована 
				// если IsFinal = true рассчитываем ema и сохраняем для последующих расчетов с промежуточными значениями
				var curValue = (newValue - _prevFinalValue) * _multiplier + _prevFinalValue;

				if (input.IsFinal)
					_prevFinalValue = curValue;

				return new DecimalIndicatorValue(this, curValue);
			}
		}
	}
}