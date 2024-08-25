namespace StockSharp.DarkHorse;

using System.Net.Http;
using System.Runtime.CompilerServices;

partial class DarkHorseMessageAdapter
{
	private long _lastMyTradeId;
	private readonly Dictionary<long, RefPair<long, decimal>> _orderInfo = new();
	//private readonly Dictionary<string, List<ExecutionMessage>> _unkOrds = new Dictionary<string, List<ExecutionMessage>>(StringComparer.InvariantCultureIgnoreCase);

	private readonly Dictionary<SecurityId, decimal> _positions = new();

	private string PortfolioName => nameof(DarkHorse) + "_" + Key.ToId();

	/// <inheritdoc />
	public override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		switch (regMsg.OrderType)
		{
			case null:
			case OrderTypes.Limit:
			//case OrderTypes.Market:
				break;
			case OrderTypes.Conditional:
			{
				var condition = (DarkHorseCondition)regMsg.Condition;

				if (!condition.IsWithdraw)
					throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));

				var withdrawId = await _httpClient.Withdraw(regMsg.SecurityId.SecurityCode, regMsg.Volume, condition.WithdrawInfo, cancellationToken);

				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderId = withdrawId,
					ServerTime = CurrentTime.ConvertToUtc(),
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				});

				await PortfolioLookupAsync(null, cancellationToken);
				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var currency = regMsg.SecurityId.ToCurrency();
		var reply = await _httpClient.MakeOrder(
			currency,
			regMsg.Side.ToBtce(),
			regMsg.Price,
			regMsg.Volume,
			cancellationToken
		);

		var isDone = reply.Command.OrderId == 0;
		var msg = new ExecutionMessage
		{
			OriginalTransactionId = regMsg.TransactionId,
			DataTypeEx = DataType.Transactions,
			OrderId = isDone ? null : reply.Command.OrderId,
			Balance = isDone ? 0m : (decimal)reply.Command.Remains,
			OrderState = isDone ? OrderStates.Done : OrderStates.Active,
			HasOrderInfo = true,

			OrderPrice = regMsg.Price,
			OrderVolume = regMsg.Volume,
			Side = regMsg.Side,
			SecurityId = regMsg.SecurityId,
			OrderType = OrderTypes.Limit
		};

		if (isDone)
		{
			//_unkOrds.SafeAdd(currency).Add(msg);

			var trades = GetTrades(cancellationToken);

			await foreach(var trade in trades)
			{
				if (!_orderInfo.ContainsKey(trade.OrderId) && msg != null)
				{
					msg.OrderId = trade.OrderId;

					_orderInfo.Add(trade.OrderId, RefTuple.Create(regMsg.TransactionId, regMsg.Volume));
					
					// send only 1 time
					SendOutMessage(msg);
					msg = null;
				}

				ProcessTrade(trade);
			}
		}
		else
		{
			_orderInfo.Add(reply.Command.OrderId, RefTuple.Create(regMsg.TransactionId, regMsg.Volume));
			SendOutMessage(msg);
		}

		ProcessFunds(reply.Command.Funds);
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		var reply = await _httpClient.CancelOrder(cancelMsg.OrderId.Value, cancellationToken);

		//SendOutMessage(new ExecutionMessage
		//{
		//	OriginalTransactionId = cancelMsg.TransactionId,
		//	OrderId = cancelMsg.OrderId,
		//	OrderState = OrderStates.Done,
		//	DataTypeEx = DataType.Transactions,
		//	HasOrderInfo = true,
		//	ServerTime = CurrentTime.ConvertToUtc(),
		//});

		await OrderStatusAsync(null, cancellationToken);
		ProcessFunds(reply.Command.Funds);
	}

	private void ProcessOrder(Order order, decimal balance, long transId, long origTransId)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = order.Id,
			TransactionId = transId,
			OriginalTransactionId = origTransId,
			OrderPrice = (decimal)order.Price,
			Balance = balance,
			OrderVolume = (decimal)order.Volume,
			Side = order.Side.ToSide(),
			SecurityId = order.Instrument.ToStockSharp(),
			ServerTime = transId != 0 ? order.Timestamp.ApplyUtc() : CurrentTime.ConvertToUtc(),
			PortfolioName = PortfolioName,
			//OrderState = order.Status.ToOrderState(),
			OrderState = OrderStates.Active,
			HasOrderInfo = true,
			OrderType = OrderTypes.Limit
		});
	}

	//private const decimal _epsilon = 0.000000001m;

	private void ProcessTrade(Trade trade)
	{
		var serverTime = trade.Timestamp.ApplyUtc();
		var secId = trade.Instrument.ToStockSharp();
		var side = trade.Side.ToSide();

		var info = _orderInfo.TryGetValue(trade.OrderId);
		if (info == null)
		{
			return;

			//if (!_unkOrds.TryGetValue(trade.Instrument, out var que) || que.Count == 0)
			//	return;

			//// последни?неизвестны?орде??деке
			//var msg = que.FindLast(o => o.SecurityId == secId);
			//if (msg == null)
			//	return;

			//que.Remove(msg);
			//msg.OrderId = trade.OrderId;
			//msg.ServerTime = serverTime;
			//msg.PortfolioName = PortfolioName;
			//msg.Balance = msg.OrderVolume - (decimal)trade.Volume;

			//var waitingMoreTrades = msg.Balance > _epsilon;
			//if (waitingMoreTrades)
			//	msg.OrderState = OrderStates.Active;

			//SendOutMessage(msg);

			//if (waitingMoreTrades)
			//{
			//	info = new RefPair<long, decimal>
			//	{
			//		First = msg.OriginalTransactionId,
			//		Second = msg.OrderVolume.Value
			//	};

			//	_orderInfo.Add(trade.OrderId, info);
			//}
		}

		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = trade.OrderId,
			OriginalTransactionId = info.First,
			TradeId = trade.Id,
			TradePrice = (decimal)trade.Price,
			TradeVolume = (decimal)trade.Volume,
			Side = side,
			SecurityId = secId,
			ServerTime = serverTime,
			PortfolioName = PortfolioName,
			OriginSide = trade.IsMyOrder ? side.Invert() : side,
		});

		//if (info == null || info.Second <= 0)
		//	return;

		info.Second -= (decimal)trade.Volume;

		if (info.Second < 0)
			throw new InvalidOperationException(LocalizedStrings.OrderBalanceNotEnough.Put(trade.OrderId, info.Second));

		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = trade.OrderId,
			OriginalTransactionId = info.First,
			Balance = info.Second,
			OrderState = info.Second > 0 ? OrderStates.Active : OrderStates.Done,
			HasOrderInfo = true,
			ServerTime = serverTime,
		});

		if (info.Second == 0)
			_orderInfo.Remove(trade.OrderId);
	}

	private void ProcessFunds(IEnumerable<KeyValuePair<string, double>> funds, bool init = false)
	{
		foreach (var fund in funds)
		{
			//var currency = fund.Key;

			//if (!currency.StartsWithIgnoreCase("usd") &&
			//	!currency.StartsWithIgnoreCase("eur") &&
			//    !currency.StartsWithIgnoreCase("rur"))
			//{
			//	currency += "_usd";
			//}

			var pos = fund.Value.ToDecimal();

			if (pos == null)
				continue;

			var secId = new SecurityId
			{
				SecurityCode = fund.Key,
				BoardCode = BoardCodes.Btce,
			};

			if (init)
			{
				_positions[secId] = pos.Value;

				if (pos.Value == 0)
					continue;
			}
			else
			{
				if (_positions.ContainsKey(secId))
				{
					if (_positions[secId] == pos.Value)
						continue;
				}	
			}
			
			SendOutMessage(this
				.CreatePositionChangeMessage(PortfolioName, secId)
				.Add(PositionChangeTypes.CurrentValue, pos.Value));
		}
	}

    /// <inheritdoc />
    public override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
    {
        try
        {
            if (statusMsg == null)
            {
                var portfolioRefresh = false;

                // Retrieve active orders
                var orders = (await _httpClient.GetActiveOrders(cancellationToken)).Items.Values;

                var ids = _orderInfo.Keys.ToSet();

                foreach (var order in orders)
                {
                    ids.Remove(order.Id);

                    var info = _orderInfo.TryGetValue(order.Id);

                    if (info == null)
                    {
                        info = RefTuple.Create(TransactionIdGenerator.GetNextId(), (decimal)order.Volume);

                        _orderInfo.Add(order.Id, info);

                        ProcessOrder(order, (decimal)order.Volume, info.First, 0);

                        portfolioRefresh = true;
                    }
                    else
                    {
                        // Balance existing orders tracked by trades
                    }
                }

                // Process trades
                var trades = GetTrades(cancellationToken);

                await foreach (var trade in trades)
                {
                    ProcessTrade(trade);
                }

                // Handle orders that are no longer active
                foreach (var id in ids)
                {
                    portfolioRefresh = true;

                    // Can be removed from ProcessTrade
                    if (!_orderInfo.TryGetAndRemove(id, out var info))
                        return;

                    SendOutMessage(new ExecutionMessage
                    {
                        DataTypeEx = DataType.Transactions,
                        HasOrderInfo = true,
                        OrderId = id,
                        OriginalTransactionId = info.First,
                        ServerTime = CurrentTime.ConvertToUtc(),
                        OrderState = OrderStates.Done,
                    });
                }

                if (portfolioRefresh)
                    await PortfolioLookupAsync(null, cancellationToken);
            }
            else
            {
                SendSubscriptionReply(statusMsg.TransactionId);

                if (!statusMsg.IsSubscribe)
                    return;

                // Retrieve active orders
                var orders = (await _httpClient.GetActiveOrders(cancellationToken)).Items.Values;

                foreach (var order in orders)
                {
                    var info = RefTuple.Create(TransactionIdGenerator.GetNextId(), (decimal)order.Volume);

                    _orderInfo.Add(order.Id, info);

                    ProcessOrder(order, (decimal)order.Volume, info.First, statusMsg.TransactionId);
                }

                // Process trades
                var trades = GetTrades(cancellationToken);

                await foreach (var trade in trades)
                {
                    ProcessTrade(trade);
                }

                SendSubscriptionResult(statusMsg);
            }
        }
        catch (HttpRequestException httpEx)
        {
            // Handle HTTP request-related exceptions
            this.AddErrorLog($"HTTP request error in OrderStatusAsync: {httpEx.Message}");
            //SendOutMessage(new ErrorMessage { Error = httpEx.Message });
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            // Handle task cancellation, e.g., when a timeout or cancellation occurs
            this.AddErrorLog($"Task canceled in OrderStatusAsync: {taskCanceledEx.Message}");
            //SendOutMessage(new ErrorMessage { Error = "Task was canceled." });
        }
        catch (Exception ex)
        {
            // General exception handling for any other errors
            this.AddErrorLog($"Unexpected error in OrderStatusAsync: {ex.Message}");
            //SendOutMessage(new ErrorMessage { Error = "An unexpected error occurred during order status processing." });
        }
    }


    private async IAsyncEnumerable<Trade> GetTrades([EnumeratorCancellation]CancellationToken cancellationToken)
	{
		const int pageSize = 1000;
		const int maxIter = 10;

		var i = 0;

		while (true)
		{
			var batch = (await _httpClient.GetMyTrades(_lastMyTradeId + 1, cancellationToken)).Items.Values;

			var anyLess = false;

			foreach (var t in batch)
			{
				if (_lastMyTradeId < t.Id)
				{
					_lastMyTradeId = t.Id;
					yield return t;
				}

				anyLess = true;
			}

			if (anyLess || batch.Count < pageSize)
				break;

			if (++i >= maxIter)
				break;
		}
	}

    /// <inheritdoc />
    public override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
    {
        try
        {
            if (lookupMsg != null)
            {
                SendSubscriptionReply(lookupMsg.TransactionId);

                if (!lookupMsg.IsSubscribe)
                    return;
            }

            var transId = lookupMsg?.TransactionId ?? 0;

            SendOutMessage(new PortfolioMessage
            {
                PortfolioName = PortfolioName,
                BoardCode = BoardCodes.Btce,
                OriginalTransactionId = transId
            });

            // Attempt to retrieve information from the external service
            var reply = await _httpClient.GetInfo(cancellationToken);

            // Process funds returned from the reply
            ProcessFunds(reply.State.Funds, true);

            // Check if trading rights are available
            if (!reply.State.Rights.CanTrade)
            {
                SendOutMessage(this
                    .CreatePortfolioChangeMessage(PortfolioName)
                    .Add(PositionChangeTypes.State, PortfolioStates.Blocked));
            }

            // Send subscription result if necessary
            if (lookupMsg != null)
                SendSubscriptionResult(lookupMsg);

            _lastTimeBalanceCheck = CurrentTime;
        }
        catch (HttpRequestException httpEx)
        {
            // Handle potential HTTP request-related exceptions (e.g., network issues)
            this.AddErrorLog($"HTTP request error while fetching portfolio data: {httpEx.Message}");
            // Optionally, send an error message or notification
            //SendOutMessage(new ErrorMessage { Error = httpEx.Message });
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            // Handle operation cancellation (e.g., timeout or cancellation token triggered)
            this.AddErrorLog($"Task canceled while fetching portfolio data: {taskCanceledEx.Message}");
            // Optionally, send a cancellation message or notification
            //SendOutMessage(new ErrorMessage { Error = "Portfolio data retrieval was canceled." });
        }
        catch (Exception ex)
        {
            // Handle any other general exceptions
            this.AddErrorLog($"Unexpected error in PortfolioLookupAsync: {ex.Message}");
            // Optionally, send a general error message or notification
            //SendOutMessage(new ErrorMessage { Error = "An unexpected error occurred while processing the portfolio lookup." });
        }
    }

}