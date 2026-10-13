namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking error count.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ErrorKey,
	Description = LocalizedStrings.RiskErrorKey,
	GroupName = LocalizedStrings.StrategyKey)]
public class RiskErrorRule : RiskRule
{
	private int _current;

	private int _count;

	/// <summary>
	/// Error count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ErrorsKey,
		Description = LocalizedStrings.ErrorsCountKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public int Count
	{
		get => _count;
		set
		{
			if (_count == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_count = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => Count.To<string>();

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_current = 0;
	}

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.Error)
			return false;

		return ++_current >= Count;
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