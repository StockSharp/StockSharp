namespace StockSharp.Algo.Risk;

/// <summary>
/// The message adapter, automatically controlling risk rules.
/// </summary>
public class RiskMessageAdapter : MessageAdapterWrapper
{
	private readonly IRiskManager _riskManager;

	/// <summary>
	/// Initializes a new instance of the <see cref="RiskMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="riskManager">Risk control manager.</param>
	public RiskMessageAdapter(IMessageAdapter innerAdapter, IRiskManager riskManager)
		: base(innerAdapter)
	{
		_riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));

		if (_riskManager.Parent != null)
			_riskManager.Parent = this;
	}

	/// <inheritdoc />
	public override bool SendInMessage(Message message)
	{
		if (message.IsBack())
		{
			if (message.Adapter == this)
			{
				message.UndoBack();

				return base.OnSendInMessage(message);
			}
		}

		return base.SendInMessage(message);
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		ProcessRisk(message);
		
		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		if (message.Type != MessageTypes.Reset)
			ProcessRisk(message);

		base.OnInnerAdapterNewOutMessage(message);
	}

	private void ProcessRisk(Message message)
	{
		foreach (var rule in _riskManager.ProcessRules(message))
		{
			LogWarning(LocalizedStrings.ActivatingRiskRule,
				rule.GetType().GetDisplayName(), rule.Title, rule.Action);

			switch (rule.Action)
			{
				case RiskActions.ClosePositions:
				{
					break;
				}
				//case RiskActions.StopTrading:
				//	base.OnSendInMessage(new DisconnectMessage());
				//	break;
				case RiskActions.CancelOrders:
					RaiseNewOutMessage(new OrderGroupCancelMessage { TransactionId = TransactionIdGenerator.GetNextId() }.LoopBack(this));
					break;
				default:
					throw new InvalidOperationException(rule.Action.To<string>());
			}
		}
	}

	/// <summary>
	/// Create a copy of <see cref="RiskMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new RiskMessageAdapter(InnerAdapter.TypedClone(), _riskManager.Clone());
	}
}