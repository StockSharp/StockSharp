namespace StockSharp.Algo.Risk;

/// <summary>
/// The base class for risk-rules, tracking commission for own transactions.
/// </summary>
public abstract class RiskTransactionCommissionRule : RiskRule
{
	private decimal _commission;
	private decimal _totalCommission;

	/// <summary>
	/// Commission limit.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.CommissionDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public decimal Commission
	{
		get => _commission;
		set
		{
			if (_commission == value)
				return;

			_commission = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _commission.To<string>();

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_totalCommission = 0m;
	}

	/// <summary>
	/// Determine whether the commission is applicable to this rule.
	/// </summary>
	/// <param name="execMsg"><see cref="ExecutionMessage"/></param>
	/// <returns>Check result.</returns>
	protected abstract bool IsMatch(ExecutionMessage execMsg);

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.Execution)
			return false;

		if (message is not ExecutionMessage execMsg ||
			!IsMatch(execMsg) ||
			execMsg.Commission is not decimal commission)
			return false;

		_totalCommission += commission;

		if (Commission >= 0)
			return _totalCommission >= Commission;
		else
			return _totalCommission <= Commission;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set(nameof(Commission), Commission);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Commission = storage.GetValue<decimal>(nameof(Commission));
	}
}
