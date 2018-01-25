#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: TroughBar.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// TroughBar.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/TroughBar.ashx.
	/// </remarks>
	[DisplayName("TroughBar")]
	[DescriptionLoc(LocalizedStrings.Str822Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class TroughBar : BaseIndicator
	{
		private decimal _currentMinimum = decimal.MaxValue;
		private int _currentBarCount;
		private int _valueBarCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="TroughBar"/>.
		/// </summary>
		public TroughBar()
		{
		}

		private Unit _reversalAmount = new Unit();

		/// <summary>
		/// Indicator changes threshold.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str783Key)]
		[DescriptionLoc(LocalizedStrings.Str784Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Unit ReversalAmount
		{
			get => _reversalAmount;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_reversalAmount = value;

				Reset();
			}
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			try
			{
				if (candle.LowPrice < _currentMinimum)
				{
					_currentMinimum = candle.LowPrice;
					_valueBarCount = _currentBarCount;
				}
				else if (candle.HighPrice >= _currentMinimum + ReversalAmount.Value)
				{
					if (input.IsFinal)
						IsFormed = true;

					return new DecimalIndicatorValue(this, _valueBarCount);
				}

				return new DecimalIndicatorValue(this, this.GetCurrentValue());
			}
			finally
			{
				if(input.IsFinal)
					_currentBarCount++;
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			ReversalAmount.Load(settings.GetValue<SettingsStorage>(nameof(ReversalAmount)));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue(nameof(ReversalAmount), ReversalAmount.Save());
		}
	}
}