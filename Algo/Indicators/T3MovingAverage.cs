namespace StockSharp.Algo.Indicators;

/// <summary>
/// T3 Moving Average.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.T3MAKey,
	Description = LocalizedStrings.T3MovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/t3_moving_average.html")]
public class T3MovingAverage : LengthIndicator<decimal>
{
	private readonly ExponentialMovingAverage[] _emas;
	private decimal _volumeFactor;
	private int _warmUpPeriod;
	private const int _defaultWarmUpPeriod = 10;

	/// <summary>
	/// Initializes a new instance of the <see cref="T3MovingAverage"/>.
	/// </summary>
	public T3MovingAverage()
	{
		_emas = [.. Enumerable.Range(0, 6).Select(_ => new ExponentialMovingAverage())];

		Length = 5;
		VolumeFactor = 0.7m;
	}

	/// <summary>
	/// Volume factor.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.VolumeFactorKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[Range(0.000001, 0.999999)]
	public decimal VolumeFactor
	{
		get => _volumeFactor;
		set
		{
			if (value <= 0 || value >= 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_volumeFactor = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + _defaultWarmUpPeriod - 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var e1 = _emas[0].Process(input);
		var e2 = _emas[1].Process(e1);
		var e3 = _emas[2].Process(e2);
		var e4 = _emas[3].Process(e3);
		var e5 = _emas[4].Process(e4);
		var e6 = _emas[5].Process(e5);

		if (input.IsFinal)
		{
			if (_warmUpPeriod > 0)
			{
				if (_emas.All(ema => ema.IsFormed))
					_warmUpPeriod--;
			}
		}

		if (IsFormed)
		{
			var c1 = -_volumeFactor * _volumeFactor * _volumeFactor;
			var c2 = 3 * _volumeFactor * _volumeFactor + 3 * _volumeFactor * _volumeFactor * _volumeFactor;
			var c3 = -6 * _volumeFactor * _volumeFactor - 3 * _volumeFactor - 3 * _volumeFactor * _volumeFactor * _volumeFactor;
			var c4 = 1 + 3 * _volumeFactor + _volumeFactor * _volumeFactor * _volumeFactor + 3 * _volumeFactor * _volumeFactor;

			return c1 * e6.ToDecimal() + c2 * e5.ToDecimal() + c3 * e4.ToDecimal() + c4 * e3.ToDecimal();
		}

		return null;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _emas.All(ema => ema.IsFormed) && _warmUpPeriod == 0;

	/// <inheritdoc />
	public override void Reset()
	{
		foreach (var ema in _emas)
		{
			ema.Length = Length;
			ema.Reset();
		}

		_warmUpPeriod = _defaultWarmUpPeriod;

		base.Reset();
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" VF={VolumeFactor}";

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(VolumeFactor), VolumeFactor);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		VolumeFactor = storage.GetValue<decimal>(nameof(VolumeFactor));
	}
}