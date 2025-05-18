namespace StockSharp.Algo.Commissions;

/// <summary>
/// The commission calculating rule.
/// </summary>
[DataContract]
public abstract class CommissionRule : NotifiableObject, ICommissionRule
{
	/// <summary>
	/// Initialize <see cref="CommissionRule"/>.
	/// </summary>
	protected CommissionRule()
	{
		UpdateTitle();
	}

	private Unit _value = new();

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.CommissionValueKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Unit Value
	{
		get => _value;
		set
		{
			_value = value ?? throw new ArgumentNullException(nameof(value));
			NotifyChanged();
		}
	}

	/// <summary>
	/// Get title.
	/// </summary>
	protected virtual string GetTitle() => string.Empty;

	/// <summary>
	/// Update title.
	/// </summary>
	protected void UpdateTitle() => Title = GetTitle();

	private string _title;

	/// <inheritdoc />
	[Browsable(false)]
	public string Title
	{
		get => _title;
		private set
		{
			_title = value;
			NotifyChanged();
		}
	}

	/// <inheritdoc />
	public virtual void Reset()
	{
	}

	/// <inheritdoc />
	public abstract decimal? Process(ExecutionMessage message);

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		Value = storage.GetValue<Unit>(nameof(Value));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Value), Value);
	}

	/// <summary>
	/// Get result value.
	/// </summary>
	/// <param name="baseValue">Base value.</param>
	/// <returns>Result value.</returns>
	protected decimal? GetValue(decimal? baseValue)
	{
		if (baseValue == null)
			return null;

		if (Value.Type == UnitTypes.Percent)
			return (baseValue.Value * Value.Value) / 100m;

		return (decimal)Value;
	}
}

/// <summary>
/// Order commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderKey,
	Description = LocalizedStrings.OrderCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class CommissionPerOrderRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasOrderInfo())
			return GetValue(message.OrderPrice);

		return null;
	}
}

/// <summary>
/// Trade commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradeKey,
	Description = LocalizedStrings.TradeCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionPerTradeRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo())
			return GetValue(message.TradePrice);

		return null;
	}
}

/// <summary>
/// Order volume commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderVolume2Key,
	Description = LocalizedStrings.OrderVolCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class CommissionPerOrderVolumeRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasOrderInfo())
			return (decimal)(message.OrderVolume * Value);

		return null;
	}
}

/// <summary>
/// Trade volume commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradeVolumeKey,
	Description = LocalizedStrings.TradeVolCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionPerTradeVolumeRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo())
			return (decimal)(message.TradeVolume * Value);

		return null;
	}
}

/// <summary>
/// Number of orders commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderCountKey,
	Description = LocalizedStrings.OrderCountCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class CommissionPerOrderCountRule : CommissionRule
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

/// <summary>
/// Number of trades commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradesCountKey,
	Description = LocalizedStrings.TradesCountCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionPerTradeCountRule : CommissionRule
{
	private int _currentCount;
	private int _count;

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

/// <summary>
/// Trade price commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradePriceKey,
	Description = LocalizedStrings.TradePriceCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionPerTradePriceRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo())
			return (decimal)(message.TradePrice * message.TradeVolume * Value);

		return null;
	}
}

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
	protected override string GetTitle() => _securityId?.ToStringId();

	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo() && message.SecurityId == _securityId)
			return GetValue(message.TradePrice);

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

			return _secTypes.SafeAdd(secId, key => ServicesRegistry.TrySecurityProvider?.LookupById(key)?.Type);
		}

		if (message.HasTradeInfo() && getSecType(message.SecurityId) == SecurityType)
			return GetValue(message.TradePrice);

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

/// <summary>
/// Board commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BoardKey,
	Description = LocalizedStrings.BoardCommissionKey,
	GroupName = LocalizedStrings.BoardKey)]
public class CommissionBoardCodeRule : CommissionRule
{
	private ExchangeBoard _board;

	/// <summary>
	/// Board code.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.BoardCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ExchangeBoard Board
	{
		get => _board;
		set
		{
			_board = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _board?.Code;

	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo() && Board != null && message.SecurityId.BoardCode.EqualsIgnoreCase(Board.Code))
			return GetValue(message.TradePrice);

		return null;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		if (Board != null)
			storage.SetValue(nameof(Board), Board.Code);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		var boardCode = storage.GetValue<string>(nameof(Board));

		if (!boardCode.IsEmpty())
			Board = ServicesRegistry.TryExchangeInfoProvider?.TryGetExchangeBoard(boardCode);
	}
}

/// <summary>
/// Turnover commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TurnoverKey,
	Description = LocalizedStrings.TurnoverCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionTurnOverRule : CommissionRule
{
	private decimal _currentTurnOver;
	private decimal _turnOver;

	/// <summary>
	/// Turnover.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TurnoverKey,
		Description = LocalizedStrings.TurnoverKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal TurnOver
	{
		get => _turnOver;
		set
		{
			_turnOver = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _turnOver.To<string>();

	/// <inheritdoc />
	public override void Reset()
	{
		_currentTurnOver = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (!message.HasTradeInfo())
			return null;

		_currentTurnOver += message.GetTradePrice() * message.SafeGetVolume();

		if (_currentTurnOver < TurnOver)
			return null;

		return (decimal)Value;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(TurnOver), TurnOver);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		TurnOver = storage.GetValue<decimal>(nameof(TurnOver));
	}
}