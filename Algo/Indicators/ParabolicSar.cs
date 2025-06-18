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
	private class CalcBuffer(ParabolicSar parent)
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

		private List<ICandleMessage> Candles => parent._candles;

		public CalcBuffer Clone() => (CalcBuffer)MemberwiseClone();

		public decimal Calculate(bool isFinal, ICandleMessage candle)
		{
			if (Candles.Count == 0)
				Candles.Add(candle);

			if (isFinal)
				Candles.Add(candle);
			else
				Candles[^1] = candle;

			_prevValue = parent.GetCurrentValue();

			if (Candles.Count < 3)
				return _prevValue;

			if (Candles.Count == 3)
			{
				_longPosition = Candles[^1].HighPrice > Candles[^2].HighPrice;
				var max = Candles.Max(t => t.HighPrice);
				var min = Candles.Min(t => t.LowPrice);
				_xp = _longPosition ? max : min;
				_af = parent.Acceleration;
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
					var t = Candles[Candles.Count - 1 - x];

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

				if ((_longPosition && (Candles[^1].LowPrice < _todaySar || Candles[^2].LowPrice < _todaySar))
						|| (!_longPosition && (Candles[^1].HighPrice > _todaySar || Candles[^2].HighPrice > _todaySar)))
				{
					return Reverse();
				}

				if (_longPosition)
				{
					if (_prevBar != Candles.Count || Candles[^1].LowPrice < _prevSar)
					{
						value = _todaySar;
						_prevSar = _todaySar;
					}
					else
						value = _prevSar;

					if (Candles[^1].HighPrice > _xp)
					{
						_xp = Candles[^1].HighPrice;
						AfIncrease();
					}
				}
				else if (!_longPosition)
				{
					if (_prevBar != Candles.Count || Candles[^1].HighPrice > _prevSar)
					{
						value = _todaySar;
						_prevSar = _todaySar;
					}
					else
						value = _prevSar;

					if (Candles[^1].LowPrice < _xp)
					{
						_xp = Candles[^1].LowPrice;
						AfIncrease();
					}
				}

			}
			else
			{
				if (_longPosition && Candles[^1].HighPrice > _xp)
					_xp = Candles[^1].HighPrice;
				else if (!_longPosition && Candles[^1].LowPrice < _xp)
					_xp = Candles[^1].LowPrice;

				value = _prevSar;

				_todaySar = TodaySar(_longPosition ? Math.Min(_reverseValue, Candles[^1].LowPrice) :
					Math.Max(_reverseValue, Candles[^1].HighPrice));
			}

			_prevBar = Candles.Count;

			return value;
		}

		private decimal TodaySar(decimal todaySar)
		{
			if (_longPosition)
			{
				var lowestSar = Math.Min(Math.Min(todaySar, Candles[^1].LowPrice), Candles[^2].LowPrice);
				todaySar = Candles[^1].LowPrice > lowestSar ? lowestSar : Reverse();
			}
			else
			{
				var highestSar = Math.Max(Math.Max(todaySar, Candles[^1].HighPrice), Candles[^2].HighPrice);
				todaySar = Candles[^1].HighPrice < highestSar ? highestSar : Reverse();
			}

			return todaySar;
		}

		private decimal Reverse()
		{
			var todaySar = _xp;

			if ((_longPosition && _prevSar > Candles[^1].LowPrice) ||
				(!_longPosition && _prevSar < Candles[^1].HighPrice) || _prevBar != Candles.Count)
			{
				_longPosition = !_longPosition;
				_reverseBar = Candles.Count;
				_reverseValue = _xp;
				_af = parent.Acceleration;
				_xp = _longPosition ? Candles[^1].HighPrice : Candles[^1].LowPrice;
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

			_af = Math.Min(parent.AccelerationMax, _af + parent.AccelerationStep);
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
	private readonly List<ICandleMessage> _candles = [];
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
		var val = b.Calculate(input.IsFinal, input.ToCandle());

		return val == 0 ? new DecimalIndicatorValue(this, input.Time) : new DecimalIndicatorValue(this, val, input.Time);
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
