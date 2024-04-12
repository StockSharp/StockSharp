#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: PeakBar.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// PeakBar.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/peakbar.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeakBarKey,
		Description = LocalizedStrings.PeakBarKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/peakbar.html")]
	public class PeakBar : BaseIndicator
	{
		private decimal _currentMaximum = decimal.MinValue;

		private int _currentBarCount;

		private int _valueBarCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="PeakBar"/>.
		/// </summary>
		public PeakBar()
		{
		}

		private Unit _reversalAmount = new();

		/// <summary>
		/// Indicator changes threshold.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ThresholdKey,
			Description = LocalizedStrings.ThresholdDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public Unit ReversalAmount
		{
			get => _reversalAmount;
			set
			{
				_reversalAmount = value ?? throw new ArgumentNullException(nameof(value));

				Reset();
			}
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, high, low, _) = input.GetOhlc();

			var cm = _currentMaximum;
			var vbc = _valueBarCount;

			try
			{
				if (high > cm)
				{
					cm = high;
					vbc = _currentBarCount;
				}
				else if (low <= (cm - ReversalAmount))
				{
					if (input.IsFinal)
						IsFormed = true;

					return new DecimalIndicatorValue(this, vbc);
				}

				return new DecimalIndicatorValue(this, this.GetCurrentValue());
			}
			finally
			{
				if (input.IsFinal)
				{
					_currentBarCount++;
					_currentMaximum = cm;
					_valueBarCount = vbc;
				}
			}
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			ReversalAmount.Load(storage, nameof(ReversalAmount));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(ReversalAmount), ReversalAmount.Save());
		}
	}
}