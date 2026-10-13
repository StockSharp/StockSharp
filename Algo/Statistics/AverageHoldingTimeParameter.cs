namespace StockSharp.Algo.Statistics;

/// <summary>
/// How long a position is held on average, from leaving zero to returning there.
/// </summary>
/// <remarks>
/// A reversal through zero ends one holding and starts the next, because the position before it and the
/// one after it are two different bets.
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AverageHoldingTimeKey,
	Description = LocalizedStrings.AverageHoldingTimeDescKey,
	GroupName = LocalizedStrings.PositionsKey,
	Order = 203
)]
public class AverageHoldingTimeParameter : BaseStatisticParameter<TimeSpan>, IPositionStatisticParameter
{
	private decimal _position;
	private DateTime _openedAt;
	private TimeSpan _total;
	private int _count;

	/// <summary>
	/// Initializes a new instance of the <see cref="AverageHoldingTimeParameter"/>.
	/// </summary>
	public AverageHoldingTimeParameter()
		: base(StatisticParameterTypes.AverageHoldingTime)
	{
	}

	/// <inheritdoc />
	public void Add(DateTime marketTime, decimal position)
	{
		var wasOpen = _position != 0;
		var reversed = wasOpen && position != 0 && Math.Sign(position) != Math.Sign(_position);

		if (wasOpen && (position == 0 || reversed))
		{
			_total += marketTime - _openedAt;
			_count++;

			Value = TimeSpan.FromTicks(_total.Ticks / _count);
		}

		if (position != 0 && (!wasOpen || reversed))
			_openedAt = marketTime;

		_position = position;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_position = default;
		_openedAt = default;
		_total = default;
		_count = default;

		base.Reset();
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage
			.Set("Position", _position)
			.Set("OpenedAt", _openedAt)
			.Set("Total", _total)
			.Set("Count", _count);

		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_position = storage.GetValue<decimal>("Position");
		_openedAt = storage.GetValue<DateTime>("OpenedAt");
		_total = storage.GetValue<TimeSpan>("Total");
		_count = storage.GetValue<int>("Count");

		await base.LoadAsync(storage, cancellationToken);
	}
}
