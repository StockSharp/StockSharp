namespace StockSharp.Algo.Commissions;

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
	private string _boardCode;
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
			_boardCode = _board?.Code;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _boardCode;

	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo() && message.SecurityId.BoardCode.EqualsIgnoreCase(_boardCode))
			return GetValue(message.TradePrice, message.TradeVolume);

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

		Board = null;

		_boardCode = storage.GetValue<string>(nameof(Board));

		if (!_boardCode.IsEmpty())
		{
			_board = ServicesRegistry.TryExchangeInfoProvider?.TryGetExchangeBoard(_boardCode);
			UpdateTitle();
		}
	}
}
