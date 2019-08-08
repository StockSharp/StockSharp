#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: SmoothedMovingAverage.cs
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
	/// Smoothed Moving Average.
	/// </summary>
	[DisplayName("SMMA")]
	[DescriptionLoc(LocalizedStrings.Str819Key)]
	public class SmoothedMovingAverage : LengthIndicator<decimal>
	{
		private decimal _prevFinalValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="SmoothedMovingAverage"/>.
		/// </summary>
		public SmoothedMovingAverage()
		{
			Length = 32;
		}

		/// <inheritdoc />
		public override void Reset()
		{
			_prevFinalValue = 0;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (!IsFormed)
			{
				if (input.IsFinal)
				{
					Buffer.Add(newValue);

					_prevFinalValue = Buffer.Sum() / Length;

					return new DecimalIndicatorValue(this, _prevFinalValue);
				}

				return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);
			}

			var curValue = (_prevFinalValue * (Length - 1) + newValue) / Length;

			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}
	}
}