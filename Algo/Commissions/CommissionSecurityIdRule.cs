namespace StockSharp.Algo.Commissions;

/// <summary>
/// Security commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SecurityKey,
	Description = LocalizedStrings.SecurityCommissionKey,
	GroupName = LocalizedStrings.SecuritiesKey)]
public class CommissionSecurityIdRule : CommissionRule
{
	private const string _storageKey = "Security";

	private SecurityId? _securityId;

	/// <summary>
	/// Security ID.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityIdKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityId? SecurityId
	{
		get => _securityId;
		set
		{
			_securityId = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => (_securityId?.ToStringId()).IsEmpty(LocalizedStrings.NoSecurities);

	/// <inheritdoc />
	protected override ValueTask<decimal?> OnProcessAsync(ExecutionMessage message, CancellationToken cancellationToken)
		=> new(Calculate(message));

	private decimal? Calculate(ExecutionMessage message)
	{
		if (message.HasTradeInfo() && message.SecurityId == _securityId)
			return GetValue(message.TradePrice, message.TradeVolume);

		return null;
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		if (_securityId != null)
			storage.SetValue(_storageKey, _securityId.Value.ToStringId());
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		var secId = storage.GetValue<string>(_storageKey);

		SecurityId = secId.IsEmpty() ? null : secId.ToSecurityId();
	}
}
