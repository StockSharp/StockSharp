#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: HullMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Hull Moving Average.
	/// </summary>
	[DisplayName("HMA")]
	[DescriptionLoc(LocalizedStrings.Str786Key)]
	public class HullMovingAverage : LengthIndicator<decimal>
	{
		private readonly WeightedMovingAverage _wmaSlow = new WeightedMovingAverage();
		private readonly WeightedMovingAverage _wmaFast = new WeightedMovingAverage();
		private readonly WeightedMovingAverage _wmaResult = new WeightedMovingAverage();

		/// <summary>
		/// Initializes a new instance of the <see cref="HullMovingAverage"/>.
		/// </summary>
		public HullMovingAverage()
		{
			Length = 10;
			SqrtPeriod = 0;
		}

		private int _sqrtPeriod;

		/// <summary>
		/// Period of resulting average. If equal to 0, period of resulting average is equal to the square root of HMA period. By default equal to 0.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str787Key)]
		[DescriptionLoc(LocalizedStrings.Str788Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int SqrtPeriod
		{
			get => _sqrtPeriod;
			set
			{
				_sqrtPeriod = value;
				_wmaResult.Length = value == 0 ? (int)Math.Sqrt(Length) : value;
			}
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _wmaResult.IsFormed;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_wmaSlow.Length = Length;
			_wmaFast.Length = Length / 2;
			_wmaResult.Length = SqrtPeriod == 0 ? (int)Math.Sqrt(Length) : SqrtPeriod;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_wmaSlow.Process(input);
			_wmaFast.Process(input);

			if (_wmaFast.IsFormed && _wmaSlow.IsFormed)
			{
				var diff = 2 * _wmaFast.GetCurrentValue() - _wmaSlow.GetCurrentValue();
				_wmaResult.Process(diff);
			}

			return new DecimalIndicatorValue(this, _wmaResult.GetCurrentValue());
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			SqrtPeriod = settings.GetValue<int>(nameof(SqrtPeriod));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue(nameof(SqrtPeriod), SqrtPeriod);
		}
	}
}