namespace StockSharp.Algo.Indicators;

/// <summary>
/// Relative Momentum Index (RMI).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RMIKey,
	Description = LocalizedStrings.RelativeMomentumIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/relative_momentum_index.html")]
public class RelativeMomentumIndex : LengthIndicator<decimal>
{
	private readonly CircularBuffer<decimal> _prices = new(1);
	private readonly SimpleMovingAverage _upMomentumSma = new();
	private readonly SimpleMovingAverage _downMomentumSma = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeMomentumIndex"/>.
	/// </summary>
	public RelativeMomentumIndex()
	{
		Length = 14;
		MomentumPeriod = 3;
	}

	private int _momentumPeriod;

	/// <summary>
	/// Momentum period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MomentumKey,
		Description = LocalizedStrings.PeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int MomentumPeriod
	{
		get => _momentumPeriod;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_momentumPeriod = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _upMomentumSma.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + MomentumPeriod;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
		{
			_prices.PushBack(price);
		}

		if (_prices.Count > MomentumPeriod)
		{
			var previousPrice = _prices[_prices.Count - 1 - MomentumPeriod];
			var upMoment = Math.Max(price - previousPrice, 0);
			var downMoment = Math.Max(previousPrice - price, 0);

			var upValue = _upMomentumSma.Process(input, upMoment);
			var downValue = _downMomentumSma.Process(input, downMoment);

			if (IsFormed)
			{
				var up = upValue.ToDecimal();
				var down = downValue.ToDecimal();

				var den = up + down;

				if (den != 0)
				{
					var rmi = 100m * up / den;
					return rmi;
				}
			}
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prices.Clear();
		_downMomentumSma.Length = _upMomentumSma.Length = Length;
		_prices.Capacity = Length + MomentumPeriod;

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(MomentumPeriod), MomentumPeriod);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		MomentumPeriod = storage.GetValue<int>(nameof(MomentumPeriod));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()} M={MomentumPeriod}";
}