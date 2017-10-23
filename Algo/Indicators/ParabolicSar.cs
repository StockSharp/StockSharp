#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: ParabolicSar.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Trend indicator implementation - Parabolic SAR.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/trends/parabolic_sar.
	/// </remarks>
	[DisplayName("Parabolic SAR")]
	[DescriptionLoc(LocalizedStrings.Str809Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class ParabolicSar : BaseIndicator
	{
		private decimal _prevValue;
		private readonly List<Candle> _candles = new List<Candle>();
		private bool _longPosition;
		private decimal _xp;		// Extreme Price
		private decimal _af;         // Acceleration factor
		private int _prevBar;
		private bool _afIncreased;
		private int _reverseBar;
		private decimal _reverseValue;
		private decimal _prevSar;
		private decimal _todaySar;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParabolicSar"/>.
		/// </summary>
		public ParabolicSar()
		{
			Acceleration = 0.02M;
			AccelerationStep = 0.02M;
			AccelerationMax = 0.2M;
		}

		/// <summary>
		/// Acceleration factor.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str810Key)]
		[DescriptionLoc(LocalizedStrings.Str811Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Acceleration { get; set; }

		/// <summary>
		/// Acceleration factor step.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str812Key)]
		[DescriptionLoc(LocalizedStrings.Str813Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal AccelerationStep { get; set; }

		/// <summary>
		/// Maximum acceleration factor.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.MaxKey)]
		[DescriptionLoc(LocalizedStrings.Str815Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal AccelerationMax { get; set; }

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			if (_candles.Count == 0)
				_candles.Add(candle);

			_prevValue = this.GetCurrentValue();

			if (candle.OpenTime != _candles[_candles.Count - 1].OpenTime)
			{
				_candles.Add(candle);
				//_prevValue = this.GetCurrentValue();
			}
			else
				_candles[_candles.Count - 1] = candle;

			if (_candles.Count < 3)
				return new DecimalIndicatorValue(this, _prevValue);

			if (_candles.Count == 3)
			{
				_longPosition = _candles[_candles.Count - 1].HighPrice > _candles[_candles.Count - 2].HighPrice;
				var max = _candles.Max(t => t.HighPrice);
				var min = _candles.Min(t => t.LowPrice);
				_xp = _longPosition ? max : min;
				_af = Acceleration;
				return new DecimalIndicatorValue(this, _xp + (_longPosition ? -1 : 1) * (max - min) * _af);
			}

			if (_afIncreased && _prevBar != _candles.Count)
				_afIncreased = false;

			if (input.IsFinal)
				IsFormed = true;

			var value = _prevValue;

			if (_reverseBar != _candles.Count)
			{
				_todaySar = TodaySar(_prevValue + _af * (_xp - _prevValue));

				for (var x = 1; x <= 2; x++)
				{
					if (_longPosition)
					{
						if (_todaySar > _candles[_candles.Count - 1 - x].LowPrice)
							_todaySar = _candles[_candles.Count - 1 - x].LowPrice;
					}
					else
					{
						if (_todaySar < _candles[_candles.Count - 1 - x].HighPrice)
							_todaySar = _candles[_candles.Count - 1 - x].HighPrice;
					}
				}

				if ((_longPosition && (_candles[_candles.Count - 1].LowPrice < _todaySar || _candles[_candles.Count - 2].LowPrice < _todaySar))
						|| (!_longPosition && (_candles[_candles.Count - 1].HighPrice > _todaySar || _candles[_candles.Count - 2].HighPrice > _todaySar)))
				{
					return new DecimalIndicatorValue(this, Reverse());
				}

				if (_longPosition)
				{
					if (_prevBar != _candles.Count || _candles[_candles.Count - 1].LowPrice < _prevSar)
					{
						value = _todaySar;
						_prevSar = _todaySar;
					}
					else
						value = _prevSar;

					if (_candles[_candles.Count - 1].HighPrice > _xp)
					{
						_xp = _candles[_candles.Count - 1].HighPrice;
						AfIncrease();
					}
				}
				else if (!_longPosition)
				{
					if (_prevBar != _candles.Count || _candles[_candles.Count - 1].HighPrice > _prevSar)
					{
						value = _todaySar;
						_prevSar = _todaySar;
					}
					else
						value = _prevSar;

					if (_candles[_candles.Count - 1].LowPrice < _xp)
					{
						_xp = _candles[_candles.Count - 1].LowPrice;
						AfIncrease();
					}
				}

			}
			else
			{
				if (_longPosition && _candles[_candles.Count - 1].HighPrice > _xp)
					_xp = _candles[_candles.Count - 1].HighPrice;
				else if (!_longPosition && _candles[_candles.Count - 1].LowPrice < _xp)
					_xp = _candles[_candles.Count - 1].LowPrice;

				value = _prevSar;

				_todaySar = TodaySar(_longPosition ? Math.Min(_reverseValue, _candles[_candles.Count - 1].LowPrice) :
					Math.Max(_reverseValue, _candles[_candles.Count - 1].HighPrice));
			}

			_prevBar = _candles.Count;

			return new DecimalIndicatorValue(this, value);
		}

		private decimal TodaySar(decimal todaySar)
		{
			if (_longPosition)
			{
				var lowestSar = Math.Min(Math.Min(todaySar, _candles[_candles.Count - 1].LowPrice), _candles[_candles.Count - 2].LowPrice);
				todaySar = _candles[_candles.Count - 1].LowPrice > lowestSar ? lowestSar : Reverse();
			}
			else
			{
				var highestSar = Math.Max(Math.Max(todaySar, _candles[_candles.Count - 1].HighPrice), _candles[_candles.Count - 2].HighPrice);
				todaySar = _candles[_candles.Count - 1].HighPrice < highestSar ? highestSar : Reverse();
			}

			return todaySar;
		}

		private decimal Reverse()
		{
			var todaySar = _xp;

			if ((_longPosition && _prevSar > _candles[_candles.Count - 1].LowPrice) ||
				(!_longPosition && _prevSar < _candles[_candles.Count - 1].HighPrice) || _prevBar != _candles.Count)
			{
				_longPosition = !_longPosition;
				_reverseBar = _candles.Count;
				_reverseValue = _xp;
				_af = Acceleration;
				_xp = _longPosition ? _candles[_candles.Count - 1].HighPrice : _candles[_candles.Count - 1].LowPrice;
				_prevSar = todaySar;
			}
			else
				todaySar = _prevSar;

			return todaySar;
		}

		private void AfIncrease()
		{
			if (_afIncreased)
				return;
			
			_af = Math.Min(AccelerationMax, _af + AccelerationStep);
			_afIncreased = true;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Acceleration = settings.GetValue(nameof(Acceleration), 0.02M);
			AccelerationMax = settings.GetValue(nameof(AccelerationMax), 0.2M);
			AccelerationStep = settings.GetValue(nameof(AccelerationStep), 0.02M);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue(nameof(Acceleration), Acceleration);
			settings.SetValue(nameof(AccelerationMax), AccelerationMax);
			settings.SetValue(nameof(AccelerationStep), AccelerationStep);
		}
	}
}