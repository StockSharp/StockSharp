namespace StockSharp.Bitexbook;

public partial class BitexbookMessageAdapter
{
	private string PortfolioName => nameof(Bitexbook) + "_" + Key.ToId();

	private readonly SynchronizedDictionary<long, RefTriple<long, decimal, string>> _orderInfo = new();

	/// <inheritdoc />
	protected override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		var symbol = regMsg.SecurityId.ToNative();

		switch (regMsg.OrderType)
		{
			case null:
			case OrderTypes.Limit:
			case OrderTypes.Market:
				break;
			case OrderTypes.Conditional:
			{
				var condition = (BitexbookOrderCondition)regMsg.Condition;

				if (!condition.IsWithdraw)
					throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));

				var withdrawId = await _httpClient.Withdraw(symbol, regMsg.Volume, condition.WithdrawInfo, cancellationToken);

				await SendOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderId = withdrawId,
					ServerTime = CurrentTimeUtc,
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				}, cancellationToken);

				await PortfolioLookupAsync(null, cancellationToken);
				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var isMarket = regMsg.OrderType == OrderTypes.Market;
		var price = regMsg.OrderType == OrderTypes.Market ? (decimal?)null : regMsg.Price;

		var orderId = await _httpClient.RegisterOrder(symbol, regMsg.Side.ToNative(), price, regMsg.Volume, cancellationToken);

		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = orderId,
			ServerTime = CurrentTimeUtc,
			OriginalTransactionId = regMsg.TransactionId,
			OrderState = isMarket ? OrderStates.Done : OrderStates.Active,
			Balance = isMarket ? 0 : null,
			HasOrderInfo = true,
		}, cancellationToken);

		if (isMarket)
		{

		}
		else
			_orderInfo.Add(orderId, RefTuple.Create(regMsg.TransactionId, regMsg.Volume, symbol));

		await PortfolioLookupAsync(null, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		await _httpClient.CancelOrder(cancelMsg.SecurityId.ToNative(), cancelMsg.OrderId.Value, cancellationToken);

		//SendOutMessage(new ExecutionMessage
		//{
		//	ServerTime = CurrentTimeUtc,
		//	DataTypeEx = DataType.Transactions,
		//	OriginalTransactionId = cancelMsg.TransactionId,
		//	OrderState = OrderStates.Done,
		//	HasOrderInfo = true,
		//});

		await OrderStatusAsync(null, cancellationToken);
		await PortfolioLookupAsync(null, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		var errors = new List<Exception>();

		// Handle CancelOrders mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.CancelOrders))
		{
			// Bitexbook doesn't have a cancel all API, cancel orders one by one
			// We can only cancel orders we know about from _orderInfo
			foreach (var info in _orderInfo.Keys.ToArray())
			{
				// We don't have SecurityId or Side info in _orderInfo, so we can't filter
				// If filters are specified, skip this emulation
				if (cancelMsg.SecurityId != default || cancelMsg.Side != null)
					continue;

				try
				{
					var orderInfo = _orderInfo[info];
					await _httpClient.CancelOrder(orderInfo.Third, info, cancellationToken);
				}
				catch (Exception ex)
				{
					this.AddErrorLog($"Failed to cancel order {info}: {ex.Message}");
					errors.Add(ex);
				}
			}

			// Refresh order status
			await OrderStatusAsync(null, cancellationToken);
		}

		// Handle ClosePositions mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.ClosePositions))
		{
			// Bitexbook stores balances similar to other crypto exchanges
			// We need to get balances and sell all non-base currencies

			// Note: Bitexbook's PortfolioLookupAsync doesn't actually fetch balances from the API
			// For a real implementation, we would need to add GetBalances API call
			// For now, we'll try to close positions based on known open orders

			// Cancel all open orders first (they might be holding funds)
			foreach (var info in _orderInfo.Values.ToArray())
			{
				// Check Side filter if applicable
				// (we don't know the side of the order from the stored info)

				try
				{
					await _httpClient.CancelOrder(info.Third, info.First, cancellationToken);
				}
				catch (Exception ex)
				{
					this.AddErrorLog($"Failed to cancel order {info.First}: {ex.Message}");
					errors.Add(ex);
				}
			}

			// Bitexbook likely doesn't support streaming, refresh status manually
			await OrderStatusAsync(null, cancellationToken);
			await PortfolioLookupAsync(null, cancellationToken);
		}

		// Send result with errors if any
		if (errors.Count > 0)
		{
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = cancelMsg.TransactionId,
				ServerTime = CurrentTimeUtc,
				HasOrderInfo = true,
				Error = errors.Count == 1 ? errors[0] : new AggregateException(errors),
			}, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		if (lookupMsg != null)
		{
			SendSubscriptionReply(lookupMsg.TransactionId);

			if (!lookupMsg.IsSubscribe)
				return;

			await SendOutMessageAsync(new PortfolioMessage
			{
				PortfolioName = PortfolioName,
				BoardCode = BoardCodes.Bitexbook,
				OriginalTransactionId = lookupMsg.TransactionId
			}, cancellationToken);

			SendSubscriptionResult(lookupMsg);
			return;
		}

		//ProcessPosition("BTC", balance.Free?.Btc, balance.Freezed?.Btc);
		//ProcessPosition("BCH", balance.Free?.Bch, balance.Freezed?.Bch);
		//ProcessPosition("ETC", balance.Free?.Etc, balance.Freezed?.Etc);
		//ProcessPosition("ETH", balance.Free?.Eth, balance.Freezed?.Eth);
		//ProcessPosition("LTC", balance.Free?.Ltc, balance.Freezed?.Ltc);
		//ProcessPosition("USD", balance.Free?.Usd, balance.Freezed?.Usd);

		_lastTimeBalanceCheck = CurrentTimeUtc;
	}

	/// <inheritdoc />
	protected override ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		if (statusMsg == null)
		{

		}
		else
		{
			SendSubscriptionReply(statusMsg.TransactionId);
			SendSubscriptionResult(statusMsg);
		}

		return default;
	}

	private ValueTask ProcessOrder(SecurityId secId, Order order, long transId, long origTransId, OrderStates state, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = (transId != 0 ? order.CreatedTimestamp : order.ClosedTimestamp) ?? CurrentTimeUtc,
			SecurityId = secId,
			TransactionId = transId,
			OriginalTransactionId = origTransId,
			OrderStringId = order.Id,
			OrderVolume = (decimal)order.Volume,
			Balance = order.GetBalance(),
			Side = order.Type.ToSide(),
			OrderPrice = (decimal)(order.Price ?? 0),
			PortfolioName = PortfolioName,
			OrderState = state,
		}, cancellationToken);
	}

	private ValueTask ProcessPosition(string currency, decimal? free, decimal? freezed, CancellationToken cancellationToken)
	{
		if (free == null && freezed == null)
			return default;

		return SendOutMessageAsync(new PositionChangeMessage
		{
			PortfolioName = PortfolioName,
			SecurityId = currency.ToStockSharp(),
			ServerTime = CurrentTimeUtc,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, free, true)
		.TryAdd(PositionChangeTypes.BlockedValue, freezed, true), cancellationToken);
	}
}
