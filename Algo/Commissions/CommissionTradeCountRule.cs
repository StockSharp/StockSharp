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
		using (EnterScope())
			_currentCount = 0;

		base.Reset();
	}

	/// <inheritdoc />
	protected override ValueTask<decimal?> OnProcessAsync(ExecutionMessage message, CancellationToken cancellationToken)
		=> new(Calculate(message));

	private decimal? Calculate(ExecutionMessage message)
	{
		if (!message.HasTradeInfo())
			return null;

		using (EnterScope())
		{
			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		storage.SetValue(nameof(Count), Count);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		Count = storage.GetValue<int>(nameof(Count));
	}
}
