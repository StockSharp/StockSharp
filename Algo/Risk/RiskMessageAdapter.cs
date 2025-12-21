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
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		// Check if trading is blocked and reject order registration/modification
		if (_isTradingBlocked)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					await RaiseNewOutMessageAsync(new ExecutionMessage
					{
						OriginalTransactionId = regMsg.TransactionId,
						DataTypeEx = DataType.Transactions,
						ServerTime = DateTime.UtcNow,
						HasOrderInfo = true,
						OrderState = OrderStates.Failed,
						Error = new InvalidOperationException(LocalizedStrings.TradingDisabled)
					}, cancellationToken);
					return;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;
					await RaiseNewOutMessageAsync(new ExecutionMessage
					{
						OriginalTransactionId = replaceMsg.TransactionId,
						DataTypeEx = DataType.Transactions,
						ServerTime = DateTime.UtcNow,
						HasOrderInfo = true,
						OrderState = OrderStates.Failed,
						Error = new InvalidOperationException(LocalizedStrings.TradingDisabled)
					}, cancellationToken);
					return;
				}
			}
		}

		var extra = await ProcessRiskAsync(message, cancellationToken);

		if (extra is not null)
			message = extra;

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.Type != MessageTypes.Reset)
		{
			var extra = await ProcessRiskAsync(message, cancellationToken);
			if (extra is not null)
			{
				extra.LoopBack(this);
				await RaiseNewOutMessageAsync(extra, cancellationToken);
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private async ValueTask<Message> ProcessRiskAsync(Message message, CancellationToken cancellationToken)
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
					await RaiseNewOutMessageAsync(new OrderGroupCancelMessage { TransactionId = TransactionIdGenerator.GetNextId() }.LoopBack(this), cancellationToken);
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