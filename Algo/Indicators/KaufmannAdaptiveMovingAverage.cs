#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: KaufmannAdaptiveMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
	[DisplayName("KAMA")]
	[DescriptionLoc(LocalizedStrings.Str792Key)]
	public class KaufmannAdaptiveMovingAverage : LengthIndicator<decimal>
	{
		private decimal _prevFinalValue;
		private bool _isInitialized;

		/// <summary>
		/// Initializes a new instance of the <see cref="KaufmannAdaptiveMovingAverage"/>.
		/// </summary>
		public KaufmannAdaptiveMovingAverage()
		{
			FastSCPeriod = 2;
			SlowSCPeriod = 30;
		}

		/// <summary>
		/// 'Rapid' EMA period. The default value is 2.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str793Key)]
		[DescriptionLoc(LocalizedStrings.Str794Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int FastSCPeriod { get; set; }

		/// <summary>
		/// 'Slow' EMA period. The default value is 30.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str795Key)]
		[DescriptionLoc(LocalizedStrings.Str796Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int SlowSCPeriod { get; set; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Buffer.Count > Length;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_prevFinalValue = 0;
			_isInitialized = false;

			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();
			var lastValue = this.GetCurrentValue();

			if (input.IsFinal)
				Buffer.Add(newValue);

			if (!IsFormed)
				return new DecimalIndicatorValue(this, lastValue);

			if (!_isInitialized && Buffer.Count == Length + 1)
			{
				_isInitialized = true;
				// Начальное значение - последнее входное значение.
				return new DecimalIndicatorValue(this, _prevFinalValue = newValue);
			}

			var buff = Buffer;

			if (input.IsFinal)
			{
				buff.RemoveAt(0);
			}
			else
			{
				buff = new List<decimal>();
				buff.AddRange(Buffer.Skip(1));
				buff.Add(newValue);
			}

			var direction = newValue - buff[0];

			decimal volatility = 0;

			for (var i = 1; i < buff.Count; i++)
			{
				volatility += Math.Abs(buff[i] - buff[i - 1]);
			}

			volatility = volatility > 0 ? volatility : 0.00001m;

			var er = Math.Abs(direction / volatility);

			var fastSC = 2m / (FastSCPeriod + 1m);
			var slowSC = 2m / (SlowSCPeriod + 1m);

			var ssc = er * (fastSC - slowSC) + slowSC;
			var smooth = (ssc * ssc);

			var curValue = (newValue - _prevFinalValue) * smooth + _prevFinalValue;
			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			FastSCPeriod = settings.GetValue<int>(nameof(FastSCPeriod));
			FastSCPeriod = settings.GetValue<int>(nameof(FastSCPeriod));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue(nameof(FastSCPeriod), FastSCPeriod);
			settings.SetValue(nameof(SlowSCPeriod), SlowSCPeriod);
		}
	}
}