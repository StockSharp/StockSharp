#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: VolumeProfileIndicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Volume profile.
	/// </summary>
	[DisplayName("VolumeProfile")]
	[DescriptionLoc(LocalizedStrings.VolumeProfileKey, true)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorOut(typeof(VolumeProfileIndicatorValue))]
	public class VolumeProfileIndicator : BaseIndicator
	{
		private readonly Dictionary<decimal, decimal> _levels = new Dictionary<decimal, decimal>();

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeProfileIndicator"/>.
		/// </summary>
		public VolumeProfileIndicator()
		{
			Step = 1;
		}

		/// <summary>
		/// The grouping increment.
		/// </summary>
		public decimal Step { get; set; }

		/// <summary>
		/// To use aggregate volume in calculations (when candles do not contain VolumeProfile).
		/// </summary>
		public bool UseTotalVolume { get; set; }

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var result = new VolumeProfileIndicatorValue(this);

			if (!input.IsFinal)
				return result;

			IsFormed = true;

			var candle = input.GetValue<Candle>();

			if (!UseTotalVolume)
			{
				if (candle.PriceLevels != null)
				{
					foreach (var priceLevel in candle.PriceLevels)
						AddVolume(priceLevel.Price, priceLevel.TotalVolume);
				}
			}
			else
				AddVolume(candle.ClosePrice, candle.TotalVolume);

			foreach (var level in _levels)
			{
				result.Levels.Add(level.Key, level.Value);
			}

			return result;
		}

		private void AddVolume(decimal price, decimal volume)
		{
			var level = (int)(price / Step) * Step;
			var currentValue = _levels.TryGetValue(level);

			_levels[level] = currentValue + volume;
		}
	}

	/// <summary>
	/// The indicator value <see cref="VolumeProfileIndicator"/>, derived in result of calculation.
	/// </summary>
	public class VolumeProfileIndicatorValue : SingleIndicatorValue<IDictionary<decimal, decimal>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeProfileIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		public VolumeProfileIndicatorValue(IIndicator indicator)
			: base(indicator)
		{
			Levels = new Dictionary<decimal, decimal>();
		}

		/// <summary>
		/// Embedded values.
		/// </summary>
		public IDictionary<decimal, decimal> Levels { get; }

		/// <summary>
		/// Does value support data type, required for the indicator.
		/// </summary>
		/// <param name="valueType">The data type, operated by indicator.</param>
		/// <returns><see langword="true" />, if data type is supported, otherwise, <see langword="false" />.</returns>
		public override bool IsSupport(Type valueType)
		{
			return valueType == typeof(decimal);
		}

		/// <summary>
		/// To get the value by the data type.
		/// </summary>
		/// <typeparam name="T">The data type, operated by indicator.</typeparam>
		/// <returns>Value.</returns>
		public override T GetValue<T>()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// To replace the indicator input value by new one (for example it is received from another indicator).
		/// </summary>
		/// <typeparam name="T">The data type, operated by indicator.</typeparam>
		/// <param name="indicator">Indicator.</param>
		/// <param name="value">Value.</param>
		/// <returns>Replaced copy of the input value.</returns>
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Compare <see cref="VolumeProfileIndicatorValue"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns>The result of the comparison.</returns>
		public override int CompareTo(IIndicatorValue other)
		{
			throw new NotSupportedException();
		}
	}
}
