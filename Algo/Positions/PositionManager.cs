namespace StockSharp.Algo.Positions;

/// <summary>
/// The position calculation manager.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PositionManager"/>.
/// </remarks>
/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
/// <param name="state">State storage.</param>
public class PositionManager(bool byOrders, IPositionManagerState state) : BaseLogReceiver, IPositionManager
{
	private readonly IPositionManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <summary>
    /// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
    /// </summary>
    public bool ByOrders { get; } = byOrders;

    /// <inheritdoc />
    public virtual PositionChangeMessage ProcessMessage(Message message)
	{
		decimal EnsureGetOrderBalance<TMessage>(TMessage msg, Sides side, decimal volume, decimal balance)
			where TMessage : Message, ITransactionIdMessage, ISecurityIdMessage, IPortfolioNameMessage
		{
			LogDebug("{0} bal_new {1}/{2}.", msg.TransactionId, balance, volume);
			return _state.AddOrGetOrder(msg.TransactionId, msg.SecurityId, msg.PortfolioName, side, volume, balance);
		}

		void ProcessRegOrder(OrderRegisterMessage regMsg)
			=> EnsureGetOrderBalance(regMsg, regMsg.Side, regMsg.Volume, regMsg.Volume);

		PositionChangeMessage UpdatePositions(SecurityId secId, string portfolioName, decimal diff, DateTime time)
		{
			var newPosition = _state.UpdatePosition(secId, portfolioName, diff);

			return new PositionChangeMessage
			{
				SecurityId = secId,
				PortfolioName = portfolioName,
				ServerTime = time,
				BuildFrom = DataType.Transactions,
			}.Add(PositionChangeTypes.CurrentValue, newPosition);
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_state.Clear();
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

				SecurityId infoSecId = default;
				string infoPfName = null;
				Sides infoSide = default;
				decimal infoBalance = 0;
				var hasInfo = false;

				if (isOrderInfo)
				{
					if (execMsg.TransactionId != 0)
					{
						infoBalance = EnsureGetOrderBalance(execMsg, execMsg.Side, execMsg.OrderVolume ?? 0, execMsg.Balance ?? 0);
						infoSecId = execMsg.SecurityId;
						infoPfName = execMsg.PortfolioName;
						infoSide = execMsg.Side;
						hasInfo = true;
					}
					else
					{
						hasInfo = _state.TryGetOrder(execMsg.OriginalTransactionId, out infoSecId, out infoPfName, out infoSide, out infoBalance);
					}
				}
				else if (execMsg.OriginalTransactionId != 0 && execMsg.HasTradeInfo())
				{
					hasInfo = _state.TryGetOrder(execMsg.OriginalTransactionId, out infoSecId, out infoPfName, out infoSide, out infoBalance);
				}

				var canUpdateOrder = isOrderInfo && hasInfo;

				decimal? balDiff = null;

				if (canUpdateOrder)
				{
					var oldBalance = execMsg.TransactionId != 0 ? execMsg.OrderVolume : infoBalance;
					balDiff = execMsg.Balance is { } newBalance ? oldBalance - newBalance : null;
					if (balDiff.HasValue && balDiff != 0)
					{
						_state.UpdateOrderBalance(execMsg.TransactionId != 0 ? execMsg.TransactionId : execMsg.OriginalTransactionId, execMsg.Balance.Value);
						LogDebug("{0} bal_upd {1}.", execMsg.TransactionId != 0 ? execMsg.TransactionId : execMsg.OriginalTransactionId, execMsg.Balance.Value);
					}

					if (execMsg.OrderState?.IsFinal() == true)
						_state.RemoveOrder(execMsg.TransactionId != 0 ? execMsg.TransactionId : execMsg.OriginalTransactionId);
				}

				if (ByOrders)
				{
					if (!canUpdateOrder)
						break;

					if (balDiff.HasValue && balDiff != 0)
					{
						var posDiff = infoSide == Sides.Buy ? balDiff.Value : -balDiff.Value;
						return UpdatePositions(infoSecId, infoPfName, posDiff, execMsg.ServerTime);
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

					var secId = hasInfo ? infoSecId : execMsg.SecurityId;
					var portfolioName = hasInfo ? infoPfName : execMsg.PortfolioName;

					return UpdatePositions(secId, portfolioName, tradeVol.Value, execMsg.ServerTime);
				}

				break;
			}
		}

		return null;
	}
}
