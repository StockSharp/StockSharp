namespace StockSharp.Algo.Indicators;

/// <summary>
/// NickRypockTrailingReverse (Nick Rypock Trailing reverse).
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/nrtr.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.NRTRKey,
	Description = LocalizedStrings.NickRypockTrailingReverseKey)]
[Doc("topics/api/indicators/list_of_indicators/nrtr.html")]
public class NickRypockTrailingReverse : LengthIndicator<decimal>
{
	private struct CalcBuffer
	{
		private bool _isInitialized;

		private decimal _k;
		private decimal _reverse;
		private decimal _price;
		private decimal _highPrice;
		private decimal _lowPrice;
		private int _newTrend;
		private int _trend;

		public decimal Calculate(NickRypockTrailingReverse ind, IIndicatorValue input)
		{
			if (_isInitialized == false)
			{
				_k = input.ToDecimal();
				_highPrice = input.ToDecimal();
				_lowPrice = input.ToDecimal();

				_isInitialized = true;
			}

			_price = input.ToDecimal();

			_k = (_k + (_price - _k) / ind.Length) * ind._multiple;

			_newTrend = 0;

			if (_trend >= 0)
			{
				if (_price > _highPrice)
					_highPrice = _price;

				_reverse = _highPrice - _k;

				if (_price <= _reverse)
				{
					_newTrend = -1;
					_lowPrice = _price;
					_reverse = _lowPrice + _k;
				}
				else
				{
					_newTrend = +1;
				}
			}

			if (_trend <= 0)
			{
				if (_price < _lowPrice)
					_lowPrice = _price;

				_reverse = _lowPrice + _k;

				if (_price >= _reverse)
				{
					_newTrend = +1;
					_highPrice = _price;
					_reverse = _highPrice - _k;
				}
				else
				{
					_newTrend = -1;
				}
			}

			if (_newTrend != 0)
				_trend = _newTrend;

			return _reverse;
		}
	}

	private CalcBuffer _buf;

	private decimal _multiple;

	/// <summary>
	/// Multiplication factor.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MultiplicationFactorKey,
		Description = LocalizedStrings.MultiplicationFactorDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Multiple
	{
		get => _multiple * 1000;
		set
		{
			var tmpValue = value;

			if (tmpValue <= 1)
				tmpValue = 1;

			_multiple = tmpValue / 1000;

			Reset();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NickRypockTrailingReverse"/>.
	/// </summary>
	public NickRypockTrailingReverse()
	{
		Multiple = 100;
		Length = 50;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var b = _buf;

		var newValue = b.Calculate(this, input);

		if (input.IsFinal)
		{
			_buf = b;
			Buffer.PushBack(newValue);
		}

		return newValue;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_buf = default;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Multiple = storage.GetValue<decimal>(nameof(Multiple));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Multiple), Multiple);
	}
}