#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: ChandeMomentumOscillator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Chande Momentum Oscillator.
	/// </summary>
	[DisplayName("CMO")]
	[DescriptionLoc(LocalizedStrings.Str759Key)]
	public class ChandeMomentumOscillator : LengthIndicator<decimal>
	{
		private readonly Sum _cmoUp = new Sum();
		private readonly Sum _cmoDn = new Sum();
		private bool _isInitialized;
		private decimal _last;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChandeMomentumOscillator"/>.
		/// </summary>
		public ChandeMomentumOscillator()
		{
			Length = 15;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_cmoDn.Length = _cmoUp.Length = Length;
			_isInitialized = false;
			_last = 0;

			base.Reset();
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _cmoUp.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (!_isInitialized)
			{
				if (input.IsFinal)
				{
					_last = newValue;
					_isInitialized = true;
				}

				return new DecimalIndicatorValue(this);
			}

			var delta = newValue - _last;

			var upValue = _cmoUp.Process(input.SetValue(this, delta > 0 ? delta : 0m)).GetValue<decimal>();
			var downValue = _cmoDn.Process(input.SetValue(this, delta > 0 ? 0m : -delta)).GetValue<decimal>();

			if(input.IsFinal)
				_last = newValue;

			var value = (upValue + downValue) == 0 ? 0 : 100m * (upValue - downValue) / (upValue + downValue);

			return IsFormed ? new DecimalIndicatorValue(this, value) : new DecimalIndicatorValue(this);
		}
	}
}