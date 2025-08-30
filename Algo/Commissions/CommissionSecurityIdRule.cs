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
	private SecurityId? _securityId;
	private Security _security;

	/// <summary>
	/// Security ID.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityIdKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Security Security
	{
		get => _security;
		set
		{
			_security = value;
			_securityId = _security?.ToSecurityId();
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => (_securityId?.ToStringId()).IsEmpty(LocalizedStrings.NoSecurities);

	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo() && message.SecurityId == _securityId)
			return GetValue(message.TradePrice, message.TradeVolume);

		return null;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		if (_securityId != null)
			storage.SetValue(nameof(Security), _securityId.Value.ToStringId());
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Security = null;

		if (storage.Contains(nameof(Security)))
		{
			var secId = storage.GetValue<string>(nameof(Security));
			_securityId = secId.ToSecurityId();

			var secProvider = ServicesRegistry.TrySecurityProvider;

			if (secProvider is not null)
				_security = secProvider.LookupById(secId);

			UpdateTitle();
		}
	}
}
