#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Vidya.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using System;

	using StockSharp.Localization;

	/// <summary>
	/// The dynamic average of variable index  (Variable Index Dynamic Average).
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/Vidya.ashx http://www.mql5.com/en/code/75.
	/// </remarks>
	[DisplayName("Vidya")]
	[DescriptionLoc(LocalizedStrings.Str755Key)]
	public class Vidya : LengthIndicator<decimal>
	{
		private decimal _multiplier = 1;
		private decimal _prevFinalValue;

		private readonly ChandeMomentumOscillator _cmo;

		/// <summary>
		/// To create the indicator <see cref="Vidya"/>.
		/// </summary>
		public Vidya()
		{
			_cmo = new ChandeMomentumOscillator();
			Length = 15;
		}

		/// <inheritdoc />
		public override void Reset()
		{
			_cmo.Length = Length;
			_multiplier = 2m / (Length + 1);
			_prevFinalValue = 0;

			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			// calc СMO
			var cmoValue = _cmo.Process(input);

			if (cmoValue.IsEmpty)
				return new DecimalIndicatorValue(this);

			// calc Vidya
			if (!IsFormed)
			{
				if (!input.IsFinal)
					return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);

				Buffer.Add(newValue);

				_prevFinalValue = Buffer.Sum() / Length;

				return new DecimalIndicatorValue(this, _prevFinalValue);
			}

			var curValue = (newValue - _prevFinalValue) * _multiplier * Math.Abs(cmoValue.GetValue<decimal>() / 100m) + _prevFinalValue;
				
			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}
	}
}