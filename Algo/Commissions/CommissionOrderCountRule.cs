namespace StockSharp.Algo.Commissions;

/// <summary>
/// Number of orders commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderCountKey,
	Description = LocalizedStrings.OrderCountCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class CommissionOrderCountRule : CommissionRule
{
	private int _currentCount;
	private int _count;

	/// <summary>
	/// Order count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrdersKey,
		Description = LocalizedStrings.OrdersCountKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Count
	{
		get => _count;
		set
		{
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
		if (!message.HasOrderInfo())
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
