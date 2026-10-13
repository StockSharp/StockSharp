namespace StockSharp.Algo.Statistics;

/// <summary>
/// The share of the observed time a position was open, in percent.
/// </summary>
/// <remarks>
/// Measured between the first and the last position reported, so time before the strategy held anything
/// and after it stopped reporting is not counted against it.
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TimeInMarketKey,
	Description = LocalizedStrings.TimeInMarketDescKey,
	GroupName = LocalizedStrings.PositionsKey,
	Order = 202
)]
public class TimeInMarketParameter : BaseStatisticParameter<decimal>, IPositionStatisticParameter
{
	private DateTime _first;
	private DateTime _last;
	private decimal _position;
	private TimeSpan _held;

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeInMarketParameter"/>.
	/// </summary>
	public TimeInMarketParameter()
		: base(StatisticParameterTypes.TimeInMarket)
	{
	}

	/// <inheritdoc />
	public void Add(DateTime marketTime, decimal position)
	{
		if (_first == default)
		{
			_first = marketTime;
			_last = marketTime;
			_position = position;
			return;
		}

		if (marketTime > _last)
		{
			if (_position != 0)
				_held += marketTime - _last;

			_last = marketTime;
		}

		_position = position;

		var observed = _last - _first;

		Value = observed <= TimeSpan.Zero ? 0m : (decimal)_held.Ticks / observed.Ticks * 100m;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_first = default;
		_last = default;
		_position = default;
		_held = default;

		base.Reset();
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage
			.Set("First", _first)
			.Set("Last", _last)
			.Set("Position", _position)
			.Set("Held", _held);

		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_first = storage.GetValue<DateTime>("First");
		_last = storage.GetValue<DateTime>("Last");
		_position = storage.GetValue<decimal>("Position");
		_held = storage.GetValue<TimeSpan>("Held");

		await base.LoadAsync(storage, cancellationToken);
	}
}
