namespace StockSharp.Algo.Indicators;

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
	private struct CalcBuffer
	{
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

		public decimal Calculate(List<ICandleMessage> candles, decimal currentValue, decimal acceleration, decimal accelerationMax, decimal accelerationStep, bool isFinal, ICandleMessage candle)
		{
			if (candles.Count == 0)
				candles.Add(candle);

			if (isFinal)
				candles.Add(candle);
			else
				candles[^1] = candle;

			_prevValue = currentValue;

			if (candles.Count < 3)
				return _prevValue;

			if (candles.Count == 3)
			{
				_longPosition = candles[^1].HighPrice > candles[^2].HighPrice;
				var max = candles.Max(t => t.HighPrice);
				var min = candles.Min(t => t.LowPrice);
				_xp = _longPosition ? max : min;
				_af = acceleration;
				return _xp + (_longPosition ? -1 : 1) * (max - min) * _af;
			}

			if (_afIncreased && _prevBar != candles.Count)
				_afIncreased = false;

			var value = _prevValue;

			if (_reverseBar != candles.Count)
			{
				_todaySar = TodaySar(candles, _prevValue + _af * (_xp - _prevValue), acceleration);

				for (var x = 1; x <= 2; x++)
				{
					var t = candles[candles.Count - 1 - x];

					if (_longPosition)
					{
						if (_todaySar > t.LowPrice)
							_todaySar = t.LowPrice;
					}
					else
					{
						if (_todaySar < t.HighPrice)
							_todaySar = t.HighPrice;
					}
				}

				if ((_longPosition && (candles[^1].LowPrice < _todaySar || candles[^2].LowPrice < _todaySar))
						|| (!_longPosition && (candles[^1].HighPrice > _todaySar || candles[^2].HighPrice > _todaySar)))
				{
					return Reverse(candles, acceleration);
				}

				if (_longPosition)
				{
					if (_prevBar != candles.Count || candles[^1].LowPrice < _prevSar)
					{
						value = _todaySar;
						_prevSar = _todaySar;
					}
					else
						value = _prevSar;

					if (candles[^1].HighPrice > _xp)
					{
						_xp = candles[^1].HighPrice;
						AfIncrease(accelerationMax, accelerationStep);
					}
				}
				else if (!_longPosition)
				{
					if (_prevBar != candles.Count || candles[^1].HighPrice > _prevSar)
					{
						value = _todaySar;
						_prevSar = _todaySar;
					}
					else
						value = _prevSar;

					if (candles[^1].LowPrice < _xp)
					{
						_xp = candles[^1].LowPrice;
						AfIncrease(accelerationMax, accelerationStep);
					}
				}

			}
			else
			{
				if (_longPosition && candles[^1].HighPrice > _xp)
					_xp = candles[^1].HighPrice;
				else if (!_longPosition && candles[^1].LowPrice < _xp)
					_xp = candles[^1].LowPrice;

				value = _prevSar;

				_todaySar = TodaySar(candles, _longPosition ? Math.Min(_reverseValue, candles[^1].LowPrice) :
					Math.Max(_reverseValue, candles[^1].HighPrice), acceleration);
			}

			_prevBar = candles.Count;

			return value;
		}

		private decimal TodaySar(List<ICandleMessage> candles, decimal todaySar, decimal acceleration)
		{
			if (_longPosition)
			{
				var lowestSar = Math.Min(Math.Min(todaySar, candles[^1].LowPrice), candles[^2].LowPrice);
				todaySar = candles[^1].LowPrice > lowestSar ? lowestSar : Reverse(candles, acceleration);
			}
			else
			{
				var highestSar = Math.Max(Math.Max(todaySar, candles[^1].HighPrice), candles[^2].HighPrice);
				todaySar = candles[^1].HighPrice < highestSar ? highestSar : Reverse(candles, acceleration);
			}

			return todaySar;
		}

		private decimal Reverse(List<ICandleMessage> candles, decimal acceleration)
		{
			var todaySar = _xp;

			if ((_longPosition && _prevSar > candles[^1].LowPrice) ||
				(!_longPosition && _prevSar < candles[^1].HighPrice) || _prevBar != candles.Count)
			{
				_longPosition = !_longPosition;
				_reverseBar = candles.Count;
				_reverseValue = _xp;
				_af = acceleration;
				_xp = _longPosition ? candles[^1].HighPrice : candles[^1].LowPrice;
				_prevSar = todaySar;
			}
			else
				todaySar = _prevSar;

			return todaySar;
		}

		private void AfIncrease(decimal accelerationMax, decimal accelerationStep)
		{
			if (_afIncreased)
				return;

			_af = Math.Min(accelerationMax, _af + accelerationStep);
			_afIncreased = true;
		}

		public void Reset()
		{
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

	private CalcBuffer _buf;
	private readonly List<ICandleMessage> _candles = [];
	private decimal _acceleration;
	private decimal _accelerationStep;
	private decimal _accelerationMax;

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

		var candle = input.ToCandle();
		var b = _buf;
		var val = b.Calculate(input.IsFinal ? _candles : [.. _candles.Skip(1), candle], this.GetCurrentValue(), Acceleration, AccelerationMax, AccelerationStep, input.IsFinal, candle);

		if (input.IsFinal)
			_buf = b;

		return val == 0 ? new DecimalIndicatorValue(this, input.Time) : new DecimalIndicatorValue(this, val, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_candles.Clear();
		_buf = default;
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
