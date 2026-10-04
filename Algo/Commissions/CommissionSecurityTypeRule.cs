namespace StockSharp.Algo.Commissions;

/// <summary>
/// Security type commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SecurityTypeKey,
	Description = LocalizedStrings.SecurityTypeCommissionKey,
	GroupName = LocalizedStrings.SecuritiesKey)]
public class CommissionSecurityTypeRule : CommissionRule
{
	private readonly Dictionary<SecurityId, SecurityTypes?> _secTypes = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="CommissionSecurityTypeRule"/>.
	/// </summary>
	public CommissionSecurityTypeRule()
	{
		SecurityType = SecurityTypes.Stock;
	}

	private SecurityTypes _securityType;

	/// <summary>
	/// Security type.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TypeKey,
		Description = LocalizedStrings.SecurityTypeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityTypes SecurityType
	{
		get => _securityType;
		set
		{
			_securityType = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _securityType.ToString();

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_secTypes.Clear();
	}

	/// <inheritdoc />
	protected override async ValueTask<decimal?> OnProcessAsync(ExecutionMessage message, CancellationToken cancellationToken)
	{
		if (message.HasTradeInfo() && await GetSecurityTypeAsync(message.SecurityId, cancellationToken) == SecurityType)
			return GetValue(message.TradePrice, message.TradeVolume);

		return null;
	}

	private async ValueTask<SecurityTypes?> GetSecurityTypeAsync(SecurityId secId, CancellationToken cancellationToken)
	{
		if (secId.IsAllSecurity())
			return null;

		var provider = ServicesRegistry.TrySecurityProvider;

		if (provider is null)
			return null;

		using (EnterScope())
		{
			if (_secTypes.TryGetValue(secId, out var known))
				return known;
		}

		var secType = (await provider.LookupByIdAsync(secId, cancellationToken))?.Type;

		using (EnterScope())
			_secTypes.TryAdd(secId, secType);

		return secType;
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		storage.SetValue(nameof(SecurityType), SecurityType);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		SecurityType = storage.GetValue<SecurityTypes>(nameof(SecurityType));
	}
}
