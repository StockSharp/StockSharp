namespace StockSharp.Algo.Commissions;

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