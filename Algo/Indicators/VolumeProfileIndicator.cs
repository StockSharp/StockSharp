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
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Volume profile.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/volume_profile.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeProfileKey,
		Description = LocalizedStrings.VolumeProfileKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorOut(typeof(VolumeProfileIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/volume_profile.html")]
	public class VolumeProfileIndicator : BaseIndicator
	{
		/// <summary>
		/// The indicator value <see cref="VolumeProfileIndicator"/>, derived in result of calculation.
		/// </summary>
		private class VolumeProfileIndicatorValue : SingleIndicatorValue<IDictionary<decimal, decimal>>
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

			/// <inheritdoc />
			public override bool IsSupport(Type valueType) => valueType == typeof(decimal);

			/// <inheritdoc />
			public override T GetValue<T>(Level1Fields? field) => throw new NotSupportedException();

			/// <inheritdoc />
			public override IIndicatorValue SetValue<T>(IIndicator indicator, T value) => throw new NotSupportedException();

			/// <inheritdoc />
			public override IEnumerable<object> ToValues()
			{
				if (IsEmpty)
					yield break;

				foreach (var level in Levels)
				{
					yield return level.Key;
					yield return level.Value;
				}
			}

			/// <inheritdoc />
			public override void FromValues(object[] values)
			{
				if (values.Length == 0)
				{
					IsEmpty = true;
					return;
				}

				IsEmpty = false;

				Levels.Clear();

				for (var i = 0; i < values.Length; i += 2)
					Levels.Add(values[i].To<decimal>(), values[i + 1].To<decimal>());
			}
		}

		private readonly Dictionary<decimal, decimal> _levels = new();

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

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var result = new VolumeProfileIndicatorValue(this);

			if (!input.IsFinal)
				return result;

			IsFormed = true;

			var candle = input.GetValue<ICandleMessage>();

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
}
