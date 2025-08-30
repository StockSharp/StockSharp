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
	public override decimal? Process(ExecutionMessage message)
	{
		SecurityTypes? getSecType(SecurityId secId)
		{
			if (secId.IsAllSecurity())
				return null;

			var provider = ServicesRegistry.TrySecurityProvider;

			if (provider is null)
				return null;

			return _secTypes.SafeAdd(secId, key => provider.LookupById(key)?.Type);
		}

		if (message.HasTradeInfo() && getSecType(message.SecurityId) == SecurityType)
			return GetValue(message.TradePrice, message.TradeVolume);

		return null;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(SecurityType), SecurityType);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		SecurityType = storage.GetValue<SecurityTypes>(nameof(SecurityType));
	}
}
