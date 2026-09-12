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
		private int _bar;
		private int _prevBar;
		private bool _afIncreased;
		private int _reverseBar;
		private decimal _reverseValue;
		private decimal _prevSar;
		private decimal _todaySar;

		public decimal Calculate(List<ICandleMessage> candles, decimal currentValue, decimal acceleration, decimal accelerationMax, decimal accelerationStep)
		{
			// Bars are counted here rather than taken from the buffer, which holds only the candles the
			// calculation reads and so stops growing once the window is full.
			_bar++;

			_prevValue = currentValue;

			// Wilder seeds the trend from a window of three bars, so nothing is published before the
			// third candle has been seen.
			if (_bar < _windowSize)
				return _prevValue;

			if (_bar == _windowSize)
			{
				_longPosition = candles[^1].HighPrice > candles[^2].HighPrice;
				var max = candles.Max(t => t.HighPrice);
				var min = candles.Min(t => t.LowPrice);
				_xp = _longPosition ? max : min;
				_af = acceleration;
				return _xp + (_longPosition ? -1 : 1) * (max - min) * _af;
			}

			if (_afIncreased && _prevBar != _bar)
				_afIncreased = false;

			var value = _prevValue;

			if (_reverseBar != _bar)
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
					if (_prevBar != _bar || candles[^1].LowPrice < _prevSar)
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
					if (_prevBar != _bar || candles[^1].HighPrice > _prevSar)
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

				_todaySar = TodaySar(candles, _longPosition ? _reverseValue.Min(candles[^1].LowPrice) :
					_reverseValue.Max(candles[^1].HighPrice), acceleration);
			}

			_prevBar = _bar;

			return value;
		}

		private decimal TodaySar(List<ICandleMessage> candles, decimal todaySar, decimal acceleration)
		{
			if (_longPosition)
			{
				var lowestSar = todaySar.Min(candles[^1].LowPrice).Min(candles[^2].LowPrice);
				todaySar = candles[^1].LowPrice > lowestSar ? lowestSar : Reverse(candles, acceleration);
			}
			else
			{
				var highestSar = todaySar.Max(candles[^1].HighPrice).Max(candles[^2].HighPrice);
				todaySar = candles[^1].HighPrice < highestSar ? highestSar : Reverse(candles, acceleration);
			}

			return todaySar;
		}

		private decimal Reverse(List<ICandleMessage> candles, decimal acceleration)
		{
			var todaySar = _xp;

			if ((_longPosition && _prevSar > candles[^1].LowPrice) ||
				(!_longPosition && _prevSar < candles[^1].HighPrice) || _prevBar != _bar)
			{
				_longPosition = !_longPosition;
				_reverseBar = _bar;
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

			_af = accelerationMax.Min(_af + accelerationStep);
			_afIncreased = true;
		}

		public void Reset()
		{
			_prevValue = 0;
			_longPosition = false;
			_xp = 0;
			_af = 0;
			_bar = 0;
			_prevBar = 0;
			_afIncreased = false;
			_reverseBar = 0;
			_reverseValue = 0;
			_prevSar = 0;
			_todaySar = 0;
		}
	}

	// The calculation reads the last three candles and nothing before them.
	private const int _windowSize = 3;

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

		List<ICandleMessage> window;

		if (input.IsFinal)
		{
			if (_candles.Count == _windowSize)
				_candles.RemoveAt(0);

			_candles.Add(candle);
			window = _candles;
		}
		else
		{
			// A preview asks what the bar would be worth if it closed here, so it reads the confirmed
			// candles before it and leaves both the buffer and the state untouched.
			window = [.. _candles.Skip(_candles.Count == _windowSize ? 1 : 0), candle];
		}

		var val = b.Calculate(window, this.GetCurrentValue(), Acceleration, AccelerationMax, AccelerationStep);

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

	/// <inheritdoc />
	public override string ToString()
		=> $"{base.ToString()} A=({Acceleration}) S=({AccelerationStep}) M=({AccelerationMax})";
}
