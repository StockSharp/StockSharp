namespace StockSharp.Algo.Commissions;

/// <summary>
/// Number of trades commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradesCountKey,
	Description = LocalizedStrings.TradesCountCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionTradeCountRule : CommissionRule
{
	private int _currentCount;
	private int _count = 1;

	/// <summary>
	/// Number of trades.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradesOfKey,
		Description = LocalizedStrings.LimitOrderTifKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Count
	{
		get => _count;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_count = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _count.To<string>();

	/// <inheritdoc />
	public override void Reset()
	{
		_currentCount = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (!message.HasTradeInfo())
			return null;

		if (++_currentCount < Count)
			return null;

		_currentCount = 0;
		return (decimal)Value;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Count), Count);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Count = storage.GetValue<int>(nameof(Count));
	}
}
