namespace StockSharp.Algo.Risk;

/// <summary>
/// The message adapter, automatically controlling risk rules.
/// </summary>
public class RiskMessageAdapter : MessageAdapterWrapper
{
	private readonly IRiskManager _riskManager;
	private bool _isTradingBlocked;

	/// <summary>
	/// Initializes a new instance of the <see cref="RiskMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="riskManager">Risk control manager.</param>
	public RiskMessageAdapter(IMessageAdapter innerAdapter, IRiskManager riskManager)
		: base(innerAdapter)
	{
		_riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
		_riskManager.Parent ??= this;
	}

	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.IsBack())
		{
			if (message.Adapter == this)
			{
				message.UndoBack();

				return base.OnSendInMessageAsync(message, cancellationToken);
			}
		}

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		// Check if trading is blocked and reject order registration/modification
		if (_isTradingBlocked)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					RaiseNewOutMessage(new ExecutionMessage
					{
						OriginalTransactionId = regMsg.TransactionId,
						DataTypeEx = DataType.Transactions,
						ServerTime = DateTime.UtcNow,
						HasOrderInfo = true,
						OrderState = OrderStates.Failed,
						Error = new InvalidOperationException(LocalizedStrings.TradingDisabled)
					});
					return default;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;
					RaiseNewOutMessage(new ExecutionMessage
					{
						OriginalTransactionId = replaceMsg.TransactionId,
						DataTypeEx = DataType.Transactions,
						ServerTime = DateTime.UtcNow,
						HasOrderInfo = true,
						OrderState = OrderStates.Failed,
						Error = new InvalidOperationException(LocalizedStrings.TradingDisabled)
					});
					return default;
				}
			}
		}

		var extra = ProcessRisk(message);

		if (extra is not null)
			message = extra;

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		if (message.Type != MessageTypes.Reset)
		{
			var extra = ProcessRisk(message);
			extra.LoopBack(this);
			RaiseNewOutMessage(extra);
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	private Message ProcessRisk(Message message)
	{
		Message retVal = null;
		var triggeredRules = _riskManager.ProcessRules(message).ToArray();

		foreach (var rule in triggeredRules)
		{
			LogWarning(LocalizedStrings.ActivatingRiskRule,
				rule.GetType().GetDisplayName(), rule.Title, rule.Action);

			switch (rule.Action)
			{
				case RiskActions.ClosePositions:
				{
					// Delegate closing positions to the inner adapter
					retVal = new OrderGroupCancelMessage
					{
						TransactionId = TransactionIdGenerator.GetNextId(),
						Mode = OrderGroupCancelModes.ClosePositions,
					};
					break;
				}
				case RiskActions.StopTrading:
				{
					_isTradingBlocked = true;
					LogInfo(LocalizedStrings.TradingDisabled);
					break;
				}
				case RiskActions.CancelOrders:
					RaiseNewOutMessage(new OrderGroupCancelMessage { TransactionId = TransactionIdGenerator.GetNextId() }.LoopBack(this));
					break;
				default:
					throw new InvalidOperationException(rule.Action.To<string>());
			}
		}

		// Check if trading should be unblocked: if no rules triggered, clear the flag
		if (_isTradingBlocked && triggeredRules.Length == 0)
		{
			_isTradingBlocked = false;
			LogInfo("Trading unblocked - risk limits no longer exceeded.");
		}

		return retVal;
	}

	/// <summary>
	/// Create a copy of <see cref="RiskMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new RiskMessageAdapter(InnerAdapter.TypedClone(), _riskManager.Clone());
	}
}