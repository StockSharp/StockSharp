namespace StockSharp.Algo.Positions;

/// <summary>
/// The position calculation manager.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PositionManager"/>.
/// </remarks>
/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
public class PositionManager(bool byOrders) : BaseLogReceiver, IPositionManager
{
	private class OrderInfo(long transactionId, SecurityId securityId, string portfolioName, Sides side, decimal volume, decimal balance)
	{
		public long TransactionId { get; } = transactionId;
		public SecurityId SecurityId { get; } = securityId;
		public string PortfolioName { get; } = portfolioName;
		public Sides Side { get; } = side;
		public decimal Volume { get; } = volume;
		public decimal Balance { get; set; } = balance;

		public override string ToString() => $"{TransactionId}: {Balance}/{Volume}";
	}

	private class PositionInfo
	{
		public decimal Value { get; set; }

		public override string ToString() => Value.ToString();
	}

	private readonly Dictionary<long, OrderInfo> _ordersInfo = [];
	private readonly Dictionary<(SecurityId, string), PositionInfo> _positions = [];

	/// <summary>
	/// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
	/// </summary>
	public bool ByOrders { get; } = byOrders;

	/// <inheritdoc />
	public virtual PositionChangeMessage ProcessMessage(Message message)
	{
		static (SecurityId, string) CreateKey(SecurityId secId, string pf)
			=> (secId, pf.ToLowerInvariant());

		OrderInfo EnsureGetInfo<TMessage>(TMessage msg, Sides side, decimal volume, decimal balance)
			where TMessage : Message, ITransactionIdMessage, ISecurityIdMessage, IPortfolioNameMessage
		{
			LogDebug("{0} bal_new {1}/{2}.", msg.TransactionId, balance, volume);
			return _ordersInfo.SafeAdd(msg.TransactionId, key => new OrderInfo(key, msg.SecurityId, msg.PortfolioName, side, volume, balance));
		}

		void ProcessRegOrder(OrderRegisterMessage regMsg)
			=> EnsureGetInfo(regMsg, regMsg.Side, regMsg.Volume, regMsg.Volume);

		PositionChangeMessage UpdatePositions(SecurityId secId, string portfolioName, decimal diff, DateTimeOffset time)
		{
			var position = _positions.SafeAdd(CreateKey(secId, portfolioName));
			position.Value += diff;

			return new PositionChangeMessage
			{
				SecurityId = secId,
				PortfolioName = portfolioName,
				ServerTime = time,
				BuildFrom = DataType.Transactions,
			}.Add(PositionChangeTypes.CurrentValue, position.Value);
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_ordersInfo.Clear();
				_positions.Clear();

				break;
			}

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			{
				ProcessRegOrder((OrderRegisterMessage)message);
				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					break;

				var isOrderInfo = execMsg.HasOrderInfo();

				var info =
					isOrderInfo
						? execMsg.TransactionId != 0
							? EnsureGetInfo(execMsg, execMsg.Side, execMsg.OrderVolume ?? 0, execMsg.Balance ?? 0)
							: _ordersInfo.TryGetValue(execMsg.OriginalTransactionId)
						: execMsg.OriginalTransactionId != 0 && execMsg.HasTradeInfo()
							? _ordersInfo.TryGetValue(execMsg.OriginalTransactionId)
							: null;

				var canUpdateOrder = isOrderInfo && info != null;

				decimal? balDiff = null;

				if (canUpdateOrder)
				{
					var oldBalance = execMsg.TransactionId != 0 ? execMsg.OrderVolume : info.Balance;
					balDiff = oldBalance - execMsg.Balance;
					if (balDiff.HasValue && balDiff != 0)
					{
						// ReSharper disable once PossibleInvalidOperationException
						info.Balance = execMsg.Balance.Value;
						LogDebug("{0} bal_upd {1}/{2}.", info.TransactionId, info.Balance, info.Volume);
					}

					if (execMsg.OrderState?.IsFinal() == true)
						_ordersInfo.Remove(info.TransactionId);
				}

				if (ByOrders)
				{
					if (!canUpdateOrder)
						break;

					if (balDiff.HasValue && balDiff != 0)
					{
						var posDiff = info.Side == Sides.Buy ? balDiff.Value : -balDiff.Value;
						return UpdatePositions(info.SecurityId, info.PortfolioName, posDiff, execMsg.ServerTime);
					}
				}
				else
				{
					if (!execMsg.HasTradeInfo())
						break;

					var tradeVol = execMsg.TradeVolume;

					if (tradeVol == null)
						break;

					if (tradeVol == 0)
					{
						LogWarning("Trade {0}/{1} of order {2} has zero volume.", execMsg.TradeId, execMsg.TradeStringId, execMsg.OriginalTransactionId);
						break;
					}

					if (execMsg.Side == Sides.Sell)
						tradeVol = -tradeVol;

					var secId = info?.SecurityId ?? execMsg.SecurityId;
					var portfolioName = info?.PortfolioName ?? execMsg.PortfolioName;

					return UpdatePositions(secId, portfolioName, tradeVol.Value, execMsg.ServerTime);
				}

				break;
			}
		}

		return null;
	}
}