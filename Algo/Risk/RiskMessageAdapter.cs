namespace StockSharp.Algo.Risk;

/// <summary>
/// The message adapter, automatically controlling risk rules.
/// </summary>
public class RiskMessageAdapter : MessageAdapterWrapper
{
	private readonly IRiskManager _riskManager;
	private readonly Lock _sync = new();
	private readonly Dictionary<IRiskRule, RiskSubject> _blockingRules = [];
	private bool _isTradingBlocked;

	private readonly record struct RiskSubject(MessageTypes Type, SecurityId? SecurityId, string PortfolioName, bool MatchOrderFamily)
	{
		private static bool IsOrderMessage(MessageTypes type)
			=> type is MessageTypes.OrderRegister or MessageTypes.OrderReplace;

		public static RiskSubject Create(IRiskRule rule, Message message)
		{
			// Order price, volume and frequency rules are global and accept both registration and
			// replacement as the same input family. Position rules remain scoped to their subject.
			if (rule is RiskOrderPriceRule or RiskOrderVolumeRule or RiskOrderFreqRule)
				return new(message.Type, null, null, true);

			return new(
				message.Type,
				(message as ISecurityIdMessage)?.SecurityId,
				(message as IPortfolioNameMessage)?.PortfolioName,
				false);
		}

		public bool Matches(Message message)
		{
			var sameType = message.Type == Type
				|| (MatchOrderFamily && IsOrderMessage(Type) && IsOrderMessage(message.Type));

			if (!sameType)
				return false;

			if (SecurityId is not null && (message as ISecurityIdMessage)?.SecurityId != SecurityId)
				return false;

			return PortfolioName.IsEmpty() || (message as IPortfolioNameMessage)?.PortfolioName == PortfolioName;
		}
	}

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
		var (extra, wasTradingBlocked, isTradingBlocked) = await ProcessRiskAsync(message, cancellationToken);

		// Every triggered action is independent of whether the original order is accepted. In particular,
		// a ClosePositions command must not be lost when an existing StopTrading block rejects the order.
		if (extra is not null)
			await base.OnSendInMessageAsync(extra, cancellationToken);

		// A rule that was already blocking gets to inspect its next applicable order before the order is
		// refused. This lets a message-scoped limit (for example order volume) release its own block while
		// preserving the historical behavior in which the order that first triggers StopTrading is sent.
		if (wasTradingBlocked && isTradingBlocked)
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

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (extra, _, _) = await ProcessRiskAsync(message, cancellationToken);
		if (extra is not null)
		{
			extra.LoopBack(this);
			await RaiseNewOutMessageAsync(extra, cancellationToken);
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private async ValueTask<(Message extra, bool wasTradingBlocked, bool isTradingBlocked)> ProcessRiskAsync(Message message, CancellationToken cancellationToken)
	{
		Message retVal = null;
		List<Message> extraOut = null;
		bool wasTradingBlocked;
		bool isTradingBlocked;

		using (_sync.EnterScope())
		{
			wasTradingBlocked = _isTradingBlocked;

			var triggeredRules = _riskManager.ProcessRules(message).ToArray();
			var triggeredSet = triggeredRules.ToHashSet();

			if (message.Type == MessageTypes.Reset)
				_blockingRules.Clear();

			foreach (var rule in triggeredRules)
			{
				LogWarning(LocalizedStrings.ActivatingRiskRule,
					rule.GetType().GetDisplayName(), rule.Title, rule.Action);

				switch (rule.Action)
				{
					case RiskActions.ClosePositions:
					{
						// Delegate closing positions to the inner adapter.
						retVal = new OrderGroupCancelMessage
						{
							TransactionId = TransactionIdGenerator.GetNextId(),
							Mode = OrderGroupCancelModes.ClosePositions,
						};
						break;
					}
					case RiskActions.StopTrading:
					{
						_blockingRules[rule] = RiskSubject.Create(rule, message);
						LogInfo(LocalizedStrings.TradingDisabled);
						break;
					}
					case RiskActions.CancelOrders:
						(extraOut ??= []).Add(new OrderGroupCancelMessage { TransactionId = TransactionIdGenerator.GetNextId() }.LoopBack(this));
						break;
					default:
						throw new InvalidOperationException(rule.Action.To<string>());
				}
			}

			foreach (var (rule, subject) in _blockingRules.ToArray())
			{
				if (!triggeredSet.Contains(rule) && subject.Matches(message))
					_blockingRules.Remove(rule);
			}

			_isTradingBlocked = _blockingRules.Count > 0;
			isTradingBlocked = _isTradingBlocked;

			if (wasTradingBlocked && !isTradingBlocked)
				LogInfo("Trading unblocked - risk limits no longer exceeded.");
		}

		if (extraOut is not null)
		{
			foreach (var extra in extraOut)
				await RaiseNewOutMessageAsync(extra, cancellationToken);
		}

		return (retVal, wasTradingBlocked, isTradingBlocked);
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
