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
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Trend indicator implementation - Parabolic SAR.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/parabolic_sar.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ParabolicSARKey,
		Description = LocalizedStrings.ParabolicSARDescKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/parabolic_sar.html")]
	public class ParabolicSar : BaseIndicator
	{
		private class CalcBuffer
		{
			private readonly ParabolicSar _ind;

			private decimal _prevValue;
			private bool _longPosition;
			private decimal _xp; // Extreme Price
			private decimal _af; // Acceleration factor
			private int _prevBar;
			private bool _afIncreased;
			private int _reverseBar;
			private decimal _reverseValue;
			private decimal _prevSar;
			private decimal _todaySar;

			private IList<ICandleMessage> Candles => _ind._candles;

			public CalcBuffer(ParabolicSar parent) => _ind = parent;

			public CalcBuffer Clone() => (CalcBuffer) MemberwiseClone();

			public decimal Calculate(ICandleMessage candle)
			{
				if (Candles.Count == 0)
					Candles.Add(candle);

				if (candle.OpenTime != Candles[Candles.Count - 1].OpenTime)
					Candles.Add(candle);
				else
					Candles[Candles.Count - 1] = candle;

				_prevValue = _ind.GetCurrentValue();

				if (Candles.Count < 3)
					return _prevValue;

				if (Candles.Count == 3)
				{
					_longPosition = Candles[Candles.Count - 1].HighPrice > Candles[Candles.Count - 2].HighPrice;
					var max = Candles.Max(t => t.HighPrice);
					var min = Candles.Min(t => t.LowPrice);
					_xp = _longPosition ? max : min;
					_af = _ind.Acceleration;
					return _xp + (_longPosition ? -1 : 1) * (max - min) * _af;
				}

				if (_afIncreased && _prevBar != Candles.Count)
					_afIncreased = false;

				var value = _prevValue;

				if (_reverseBar != Candles.Count)
				{
					_todaySar = TodaySar(_prevValue + _af * (_xp - _prevValue));

					for (var x = 1; x <= 2; x++)
					{
						if (_longPosition)
						{
							if (_todaySar > Candles[Candles.Count - 1 - x].LowPrice)
								_todaySar = Candles[Candles.Count - 1 - x].LowPrice;
						}
						else
						{
							if (_todaySar < Candles[Candles.Count - 1 - x].HighPrice)
								_todaySar = Candles[Candles.Count - 1 - x].HighPrice;
						}
					}

					if ((_longPosition && (Candles[Candles.Count - 1].LowPrice < _todaySar || Candles[Candles.Count - 2].LowPrice < _todaySar))
							|| (!_longPosition && (Candles[Candles.Count - 1].HighPrice > _todaySar || Candles[Candles.Count - 2].HighPrice > _todaySar)))
					{
						return Reverse();
					}

					if (_longPosition)
					{
						if (_prevBar != Candles.Count || Candles[Candles.Count - 1].LowPrice < _prevSar)
						{
							value = _todaySar;
							_prevSar = _todaySar;
						}
						else
							value = _prevSar;

						if (Candles[Candles.Count - 1].HighPrice > _xp)
						{
							_xp = Candles[Candles.Count - 1].HighPrice;
							AfIncrease();
						}
					}
					else if (!_longPosition)
					{
						if (_prevBar != Candles.Count || Candles[Candles.Count - 1].HighPrice > _prevSar)
						{
							value = _todaySar;
							_prevSar = _todaySar;
						}
						else
							value = _prevSar;

						if (Candles[Candles.Count - 1].LowPrice < _xp)
						{
							_xp = Candles[Candles.Count - 1].LowPrice;
							AfIncrease();
						}
					}

				}
				else
				{
					if (_longPosition && Candles[Candles.Count - 1].HighPrice > _xp)
						_xp = Candles[Candles.Count - 1].HighPrice;
					else if (!_longPosition && Candles[Candles.Count - 1].LowPrice < _xp)
						_xp = Candles[Candles.Count - 1].LowPrice;

					value = _prevSar;

					_todaySar = TodaySar(_longPosition ? Math.Min(_reverseValue, Candles[Candles.Count - 1].LowPrice) :
						Math.Max(_reverseValue, Candles[Candles.Count - 1].HighPrice));
				}

				_prevBar = Candles.Count;

				return value;
			}

			private decimal TodaySar(decimal todaySar)
			{
				if (_longPosition)
				{
					var lowestSar = Math.Min(Math.Min(todaySar, Candles[Candles.Count - 1].LowPrice), Candles[Candles.Count - 2].LowPrice);
					todaySar = Candles[Candles.Count - 1].LowPrice > lowestSar ? lowestSar : Reverse();
				}
				else
				{
					var highestSar = Math.Max(Math.Max(todaySar, Candles[Candles.Count - 1].HighPrice), Candles[Candles.Count - 2].HighPrice);
					todaySar = Candles[Candles.Count - 1].HighPrice < highestSar ? highestSar : Reverse();
				}

				return todaySar;
			}

			private decimal Reverse()
			{
				var todaySar = _xp;

				if ((_longPosition && _prevSar > Candles[Candles.Count - 1].LowPrice) ||
					(!_longPosition && _prevSar < Candles[Candles.Count - 1].HighPrice) || _prevBar != Candles.Count)
				{
					_longPosition = !_longPosition;
					_reverseBar = Candles.Count;
					_reverseValue = _xp;
					_af = _ind.Acceleration;
					_xp = _longPosition ? Candles[Candles.Count - 1].HighPrice : Candles[Candles.Count - 1].LowPrice;
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

				_af = Math.Min(_ind.AccelerationMax, _af + _ind.AccelerationStep);
				_afIncreased = true;
			}

			public void Reset()
			{
				Candles.Clear();
				_prevValue = 0;
				_longPosition = false;
				_xp = 0;
				_af = 0;
				_prevBar = 0;
				_afIncreased = false;
				_reverseBar = 0;
				_reverseValue = 0;
				_prevSar = 0;
				_todaySar = 0;
			}
		}

		private readonly CalcBuffer _buf;
		private readonly List<ICandleMessage> _candles = new();
		private decimal _acceleration;
		private decimal _accelerationStep;
		private decimal _accelerationMax;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParabolicSar"/>.
		/// </summary>
		public ParabolicSar()
		{
			_buf = new CalcBuffer(this);
			Acceleration = 0.02M;
			AccelerationStep = 0.02M;
			AccelerationMax = 0.2M;
		}

		/// <summary>
		/// Acceleration factor.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AccelerationKey,
			Description = LocalizedStrings.AccelerationFactorKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public decimal Acceleration
		{
			get => _acceleration;
			set
			{
				_acceleration = value;
				Reset();
			}
		}

		/// <summary>
		/// Acceleration factor step.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.StepKey,
			Description = LocalizedStrings.AccelerationFactorStepKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public decimal AccelerationStep
		{
			get => _accelerationStep;
			set
			{
				_accelerationStep = value;
				Reset();
			}
		}

		/// <summary>
		/// Maximum acceleration factor.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.MaxKey,
			Description = LocalizedStrings.MaxAccelerationFactorKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public decimal AccelerationMax
		{
			get => _accelerationMax;
			set
			{
				_accelerationMax = value;
				Reset();
			}
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			var b = input.IsFinal ? _buf : _buf.Clone();
			var val = b.Calculate(input.GetValue<ICandleMessage>());

			return val == 0 ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, val);
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();
			_buf.Reset();
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Acceleration = storage.GetValue(nameof(Acceleration), 0.02M);
			AccelerationMax = storage.GetValue(nameof(AccelerationMax), 0.2M);
			AccelerationStep = storage.GetValue(nameof(AccelerationStep), 0.02M);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Acceleration), Acceleration);
			storage.SetValue(nameof(AccelerationMax), AccelerationMax);
			storage.SetValue(nameof(AccelerationStep), AccelerationStep);
		}
	}
}
