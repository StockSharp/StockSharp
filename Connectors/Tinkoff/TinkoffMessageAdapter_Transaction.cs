namespace StockSharp.Tinkoff;

public partial class TinkoffMessageAdapter
{
	private readonly SynchronizedDictionary<long, CancellationTokenSource> _ordersCts = [];
	private readonly SynchronizedDictionary<long, CancellationTokenSource> _pfCts = [];
	private readonly CachedSynchronizedSet<string> _accountIds = [];
	private readonly SynchronizedPairSet<long, Guid> _orderUids = [];

	/// <inheritdoc/>
	protected override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		var transId = regMsg.TransactionId;

		string getOrderId()
		{
			var orderId = Guid.NewGuid();
			_orderUids.Add(transId, orderId);
			return orderId.ToString();
		}

		if (regMsg.OrderType == OrderTypes.Conditional)
		{
			if (IsDemo)
				throw new NotSupportedException();

			var condition = (TinkoffOrderCondition)regMsg.Condition;

			var stopOrder = new PostStopOrderRequest
			{
				OrderId = getOrderId(),
				AccountId = regMsg.PortfolioName,
				Direction = regMsg.Side.ToStopNative(),
				InstrumentId = regMsg.GetInstrumentId(),
				Price = regMsg.Price.DefaultAsNull()?.ToQuotation(),
				Quantity = (long)regMsg.Volume,
				ExpireDate = regMsg.TillDate?.ToTimestamp(),
				ExpirationType = regMsg.TimeInForce.ToStopNative(regMsg.TillDate),
				StopOrderType = ((ITakeProfitOrderCondition)condition).ActivationPrice is not null ? StopOrderType.TakeProfit : (((IStopLossOrderCondition)condition).ClosePositionPrice is not null ? StopOrderType.StopLimit : StopOrderType.StopLoss),
				StopPrice = condition.TriggerPrice?.ToQuotation(),
			};

			if (stopOrder.StopOrderType == StopOrderType.TakeProfit && condition.IsTrailing)
				stopOrder.TakeProfitType = TakeProfitType.Trailing;

			var response = await _service.StopOrders.PostStopOrderAsync(stopOrder, cancellationToken: cancellationToken);

			await SendOutMessageAsync(new ExecutionMessage
			{
				HasOrderInfo = true,
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = transId,
				OrderStringId = response.StopOrderId,
				OrderState = OrderStates.Active,
			}, cancellationToken);
		}
		else
		{
			PostOrderResponse response;

			if (IsDemo)
			{
				response = await _service.Sandbox.PostSandboxOrderAsync(new PostOrderRequest
				{
					AccountId = regMsg.PortfolioName,
					Direction = regMsg.Side.ToNative(),
					InstrumentId = regMsg.GetInstrumentId(),
					OrderId = getOrderId(),
					OrderType = regMsg.OrderType.ToNative(),
					Price = regMsg.Price.DefaultAsNull()?.ToQuotation(),
					Quantity = (long)regMsg.Volume,
					TimeInForce = regMsg.TimeInForce.ToNative(regMsg.TillDate),
				}, cancellationToken: cancellationToken);
			}
			else
			{
				response = await _service.Orders.PostOrderAsync(new PostOrderRequest
				{
					AccountId = regMsg.PortfolioName,
					Direction = regMsg.Side.ToNative(),
					InstrumentId = regMsg.GetInstrumentId(),
					OrderId = getOrderId(),
					OrderType = regMsg.OrderType.ToNative(),
					Price = regMsg.Price.DefaultAsNull()?.ToQuotation(),
					Quantity = (long)regMsg.Volume,
					TimeInForce = regMsg.TimeInForce.ToNative(regMsg.TillDate),
				}, cancellationToken: cancellationToken);
			}

			await SendOutMessageAsync(new ExecutionMessage
			{
				HasOrderInfo = true,
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = transId,
				OrderStringId = response.OrderId,
				OrderState = response.ExecutionReportStatus.ToOrderState(),
				Error = response.ExecutionReportStatus == OrderExecutionReportStatus.ExecutionReportStatusRejected ? new InvalidOperationException(response.Message) : null,
				Commission = response.ExecutedCommission?.ToDecimal(),

				// balance updates in stream
				//Balance = response.LotsRequested - response.LotsExecuted,

				AveragePrice = response.ExecutedOrderPrice?.ToDecimal(),
			}, cancellationToken);
		}
	}

	/// <inheritdoc/>
	protected override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderType == OrderTypes.Conditional)
		{
			if (IsDemo)
				throw new NotSupportedException();

			var response = await _service.StopOrders.CancelStopOrderAsync(new()
			{
				StopOrderId = cancelMsg.OrderStringId,
			}, cancellationToken: cancellationToken);

			await SendOutMessageAsync(new ExecutionMessage
			{
				OriginalTransactionId = cancelMsg.TransactionId,
				ServerTime = response.Time.ToDateTime(),
				OrderState = OrderStates.Done,
			}, cancellationToken);
		}
		else
		{
			if (IsDemo)
			{
				await _service.Sandbox.CancelSandboxOrderAsync(new()
				{
					OrderId = cancelMsg.OrderStringId,
					AccountId = cancelMsg.PortfolioName,
				}, cancellationToken: cancellationToken);
			}
			else
			{
				await _service.Orders.CancelOrderAsync(new()
				{
					OrderId = cancelMsg.OrderStringId,
					AccountId = cancelMsg.PortfolioName,
				}, cancellationToken: cancellationToken);
			}
		}
	}

	/// <inheritdoc/>
	protected override async ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
	{
		if (replaceMsg.OrderType == OrderTypes.Conditional)
		{
			throw new NotSupportedException();
		}
		else
		{
			if (IsDemo)
			{
				await _service.Sandbox.ReplaceSandboxOrderAsync(new()
				{
					OrderId = replaceMsg.OldOrderStringId,
					Price = replaceMsg.Price.ToQuotation(),
					Quantity = (long)replaceMsg.Volume,
					AccountId = replaceMsg.PortfolioName,
					IdempotencyKey = replaceMsg.TransactionId.To<string>(),
				}, cancellationToken: cancellationToken);
			}
			else
			{
				await _service.Orders.ReplaceOrderAsync(new()
				{
					OrderId = replaceMsg.OldOrderStringId,
					Price = replaceMsg.Price.ToQuotation(),
					Quantity = (long)replaceMsg.Volume,
					AccountId = replaceMsg.PortfolioName,
					IdempotencyKey = replaceMsg.TransactionId.To<string>(),
				}, cancellationToken: cancellationToken);
			}
		}
	}

	/// <inheritdoc/>
	protected override async ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		var errors = new List<Exception>();
		var accountIds = cancelMsg.PortfolioName.IsEmpty() ? await EnsureGetAccounts(cancellationToken) : [cancelMsg.PortfolioName];

		// Handle CancelOrders mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.CancelOrders))
		{
			foreach (var accountId in accountIds)
			{
				// Get all active orders
				var ordersResponse = await (IsDemo
					? _service.Sandbox.GetSandboxOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
					: _service.Orders.GetOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
				);

				foreach (var order in ordersResponse.Orders)
				{
					var secId = order.InstrumentUid.FromInstrumentIdToSecId();

					// If SecurityId is specified, cancel only orders for that security
					if (cancelMsg.SecurityId != default && cancelMsg.SecurityId != secId)
						continue;

					// Check Side filter
					if (cancelMsg.Side != null && cancelMsg.Side != order.Direction.ToSide())
						continue;

					try
					{
						if (IsDemo)
						{
							await _service.Sandbox.CancelSandboxOrderAsync(new()
							{
								OrderId = order.OrderId,
								AccountId = accountId,
							}, cancellationToken: cancellationToken);
						}
						else
						{
							await _service.Orders.CancelOrderAsync(new()
							{
								OrderId = order.OrderId,
								AccountId = accountId,
							}, cancellationToken: cancellationToken);
						}
					}
					catch (Exception ex)
					{
						this.AddErrorLog($"Failed to cancel order {order.OrderId}: {ex.Message}");
						errors.Add(ex);
					}
				}

				// Also cancel stop orders if not in demo mode
				if (!IsDemo)
				{
					var stopOrdersResponse = await _service.StopOrders.GetStopOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken);

					foreach (var order in stopOrdersResponse.StopOrders)
					{
						var secId = order.InstrumentUid.FromInstrumentIdToSecId();

						// If SecurityId is specified, cancel only orders for that security
						if (cancelMsg.SecurityId != default && cancelMsg.SecurityId != secId)
							continue;

						// Check Side filter
						if (cancelMsg.Side != null && cancelMsg.Side != order.Direction.ToSide())
							continue;

						try
						{
							await _service.StopOrders.CancelStopOrderAsync(new()
							{
								StopOrderId = order.StopOrderId,
							}, cancellationToken: cancellationToken);
						}
						catch (Exception ex)
						{
							this.AddErrorLog($"Failed to cancel stop order {order.StopOrderId}: {ex.Message}");
							errors.Add(ex);
						}
					}
				}
			}
		}

		// Handle ClosePositions mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.ClosePositions))
		{
			foreach (var accountId in accountIds)
			{
				var pfResponse = await (IsDemo
					? _service.Sandbox.GetSandboxPortfolioAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
					: _service.Operations.GetPortfolioAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
				);

				foreach (var position in pfResponse.Positions)
				{
					var quantity = position.Quantity?.ToDecimal();

					if (quantity == null || quantity == 0)
						continue;

					var secId = position.InstrumentUid.FromInstrumentIdToSecId();

					// If SecurityId is specified, close only that position
					if (cancelMsg.SecurityId != default && cancelMsg.SecurityId != secId)
						continue;

					// Determine side to close position
					var closingSide = quantity > 0 ? Sides.Sell : Sides.Buy;

					// Check Side filter
					if (cancelMsg.Side != null && cancelMsg.Side != closingSide)
						continue;

					try
					{
						var volume = Math.Abs(quantity.Value);

						// Create market order to close position
						if (IsDemo)
						{
							await _service.Sandbox.PostSandboxOrderAsync(new PostOrderRequest
							{
								AccountId = accountId,
								Direction = closingSide.ToNative(),
								InstrumentId = position.InstrumentUid,
								OrderId = Guid.NewGuid().ToString(),
								OrderType = OrderType.Market,
								Quantity = (long)volume,
							}, cancellationToken: cancellationToken);
						}
						else
						{
							await _service.Orders.PostOrderAsync(new PostOrderRequest
							{
								AccountId = accountId,
								Direction = closingSide.ToNative(),
								InstrumentId = position.InstrumentUid,
								OrderId = Guid.NewGuid().ToString(),
								OrderType = OrderType.Market,
								Quantity = (long)volume,
							}, cancellationToken: cancellationToken);
						}
					}
					catch (Exception ex)
					{
						this.AddErrorLog($"Failed to close position for {position.InstrumentUid}: {ex.Message}");
						errors.Add(ex);
					}
				}
			}
		}

		// Send result with errors if any
		if (errors.Count > 0)
		{
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = cancelMsg.TransactionId,
				ServerTime = CurrentTime,
				HasOrderInfo = true,
				Error = errors.Count == 1 ? errors[0] : new AggregateException(errors),
			}, cancellationToken);
		}
	}

	private async ValueTask<string[]> EnsureGetAccounts(CancellationToken cancellationToken)
	{
		if (_accountIds.Count == 0)
		{
			var response = await (IsDemo
				? _service.Sandbox.GetSandboxAccountsAsync(new(), cancellationToken: cancellationToken)
				: _service.Users.GetAccountsAsync(new(), cancellationToken: cancellationToken)
			);

			_accountIds.AddRange(response.Accounts.Select(a => a.Id));
		}

		return _accountIds.Cache;
	}

	/// <inheritdoc/>
	protected override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(statusMsg.TransactionId, cancellationToken);

		if (!statusMsg.IsSubscribe)
		{
			if (_ordersCts.TryGetAndRemove(statusMsg.OriginalTransactionId, out var cts1))
				cts1.Cancel();

			return;
		}

		bool tryGetTransId(string str, out long transId)
		{
			transId = default;

			if (!Guid.TryParse(str, out var requestId))
				return false;

			if (!_orderUids.TryGetKey(requestId, out transId))
				return false;

			return true;
		}

		var accountIds = statusMsg.PortfolioName.IsEmpty() ? await EnsureGetAccounts(cancellationToken) : [statusMsg.PortfolioName];

		foreach (var accountId in accountIds)
		{
			var ordersResponse = await (IsDemo
				? _service.Sandbox.GetSandboxOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
				: _service.Orders.GetOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
			);

			foreach (var order in ordersResponse.Orders)
			{
				if (!Guid.TryParse(order.OrderRequestId, out var requestId))
					continue;

				if (!_orderUids.TryGetKey(requestId, out var transId))
				{
					transId = TransactionIdGenerator.GetNextId();
					_orderUids.Add(transId, requestId);
				}

				await SendOutMessageAsync(new ExecutionMessage
				{
					HasOrderInfo = true,
					DataTypeEx = DataType.Transactions,
					TransactionId = transId,
					OriginalTransactionId = statusMsg.TransactionId,
					SecurityId = order.InstrumentUid.FromInstrumentIdToSecId(),
					PortfolioName = accountId,
					Currency = order.Currency.To<CurrencyTypes?>(),
					OrderStringId = order.OrderId,
					ServerTime = order.OrderDate.ToDateTime(),
					Side = order.Direction.ToSide(),
					OrderType = order.OrderType.ToOrderType(),
					OrderPrice = order.InitialSecurityPrice?.ToDecimal() ?? default,
					Commission = (order.InitialCommission ?? order.ExecutedCommission)?.ToDecimal(),
					OrderVolume = order.LotsRequested,
					Balance = order.LotsRequested - order.LotsExecuted,
					AveragePrice = order.ExecutedOrderPrice?.ToDecimal(),
					OrderState = order.ExecutionReportStatus.ToOrderState(),
				}, cancellationToken);

				foreach (var trade in order.Stages)
				{
					await SendOutMessageAsync(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						OriginalTransactionId = transId,
						TradeStringId = trade.TradeId,
						TradePrice = trade.Price?.ToDecimal(),
						TradeVolume = trade.Quantity,
						ServerTime = trade.ExecutionTime.ToDateTime(),
					}, cancellationToken);
				}
			}

			if (IsDemo)
			{

			}
			else
			{
				foreach (var order in (await _service.StopOrders.GetStopOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)).StopOrders)
				{
					await SendOutMessageAsync(new ExecutionMessage
					{
						HasOrderInfo = true,
						DataTypeEx = DataType.Transactions,
						OriginalTransactionId = statusMsg.TransactionId,
						SecurityId = order.InstrumentUid.FromInstrumentIdToSecId(),
						PortfolioName = accountId,
						Currency = order.Currency.To<CurrencyTypes?>(),
						OrderStringId = order.StopOrderId,
						ServerTime = order.CreateDate.ToDateTime(),
						Side = order.Direction.ToSide(),
						OrderType = OrderTypes.Conditional,
						OrderPrice = order.Price?.ToDecimal() ?? default,
						OrderVolume = order.LotsRequested,
						ExpiryDate = order.ExpirationTime.ToDateTime(),
						Condition = new TinkoffOrderCondition
						{
							TriggerPrice = order.StopPrice?.ToDecimal(),
						},
					}, cancellationToken);
				}
			}

			var opResponse = await (IsDemo
				? _service.Sandbox.GetSandboxOperationsAsync(new() { AccountId = accountId, From = statusMsg.From?.ToTimestamp(), To = statusMsg.To?.ToTimestamp() }, cancellationToken: cancellationToken)
				: _service.Operations.GetOperationsAsync(new() { AccountId = accountId, From = statusMsg.From?.ToTimestamp(), To = statusMsg.To?.ToTimestamp() }, cancellationToken: cancellationToken)
			);

			foreach (var op in opResponse.Operations)
			{
				if (!tryGetTransId(op.ParentOperationId ?? op.Id, out var transId))
					continue;

				var secId = op.InstrumentUid.FromInstrumentIdToSecId();

				foreach (var trade in op.Trades)
				{
					await SendOutMessageAsync(new ExecutionMessage
					{
						SecurityId = secId,
						DataTypeEx = DataType.Transactions,
						OriginalTransactionId = transId,
						TradeStringId = trade.TradeId,
						TradePrice = trade.Price?.ToDecimal(),
						TradeVolume = trade.Quantity,
						ServerTime = trade.DateTime.ToDateTime(),
					}, cancellationToken);
				}
			}
		}

		if (!statusMsg.IsHistoryOnly() && !IsDemo)
		{
			var (statesCts, statesToken) = cancellationToken.CreateChildToken();

			_ordersCts.Add(statusMsg.TransactionId, statesCts);

			_ = Task.Run(async () =>
			{
				var currentDelay = _baseDelay;

				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						var statesStream = _service.OrdersStream.OrderStateStream(new(), cancellationToken: statesToken).ResponseStream;

						await foreach (var response in statesStream.ReadAllAsync(statesToken).WithEnforcedCancellation(cancellationToken))
						{
							currentDelay = _baseDelay;

							var orderState = response.OrderState;

							if (orderState is null)
								continue;

							if (!tryGetTransId(orderState.OrderRequestId, out var transId))
								continue;

							await SendOutMessageAsync(new ExecutionMessage
							{
								DataTypeEx = DataType.Transactions,
								HasOrderInfo = true,
								OriginalTransactionId = transId,
								ServerTime = CurrentTime,
								OrderStringId = orderState.OrderId,
								OrderState = orderState.ExecutionReportStatus.ToOrderState(),
								Balance = orderState.LotsRequested - orderState.LotsExecuted,
								AveragePrice = orderState.ExecutedOrderPrice?.ToDecimal(),
							}, cancellationToken);

							var secId = orderState.InstrumentUid.FromInstrumentIdToSecId();

							if (orderState.Trades is not null)
							{
								foreach (var trade in orderState.Trades)
								{
									await SendOutMessageAsync(new ExecutionMessage
									{
										DataTypeEx = DataType.Transactions,
										SecurityId = secId,
										OrderStringId = orderState.OrderId,
										OriginalTransactionId = transId,
										ServerTime = trade.DateTime.ToDateTime(),
										TradeStringId = trade.TradeId,
										TradePrice = trade.Price?.ToDecimal(),
										TradeVolume = trade.Quantity,
									}, cancellationToken);
								}
							}
						}
					}
					catch (Exception ex)
					{
						if (statesToken.IsCancellationRequested)
							break;

						this.AddErrorLog(ex);

						currentDelay = GetCurrentDelay(currentDelay);
						await currentDelay.Delay(cancellationToken);
					}
				}
			}, statesToken);
		}

		await SendSubscriptionResultAsync(statusMsg, cancellationToken);
	}

	/// <inheritdoc/>
	protected override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var transId = lookupMsg.TransactionId;

		await SendSubscriptionReplyAsync(transId, cancellationToken);

		if (!lookupMsg.IsSubscribe)
		{
			if (_pfCts.TryGetAndRemove(lookupMsg.OriginalTransactionId, out var cts1))
				cts1.Cancel();

			return;
		}

		async ValueTask processResponse(PortfolioResponse portfolio)
		{
			await SendOutMessageAsync(new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				PortfolioName = portfolio.AccountId,
				ServerTime = CurrentTime,
				OriginalTransactionId = transId,
			}
			.TryAdd(PositionChangeTypes.CurrentValue, portfolio.TotalAmountPortfolio?.ToDecimal(), true)
			.TryAdd(PositionChangeTypes.RealizedPnL, portfolio.ExpectedYield?.ToDecimal(), true)
			, cancellationToken);

			foreach (var position in portfolio.Positions)
			{
				await SendOutMessageAsync(new PositionChangeMessage
				{
					SecurityId = position.InstrumentUid.FromInstrumentIdToSecId(),
					PortfolioName = portfolio.AccountId,
					ServerTime = CurrentTime,
					OriginalTransactionId = transId,
				}
				.TryAdd(PositionChangeTypes.CurrentValue, position.Quantity?.ToDecimal(), true)
				.TryAdd(PositionChangeTypes.AveragePrice, position.AveragePositionPrice?.ToDecimal(), true)
				.TryAdd(PositionChangeTypes.RealizedPnL, position.ExpectedYield?.ToDecimal() ?? position.CurrentNkd?.ToDecimal(), true)
				.TryAdd(PositionChangeTypes.CurrentPrice, position.CurrentPrice?.ToDecimal(), true)
				.TryAdd(PositionChangeTypes.VariationMargin, position.VarMargin?.ToDecimal(), true)
				, cancellationToken);
			}
		}

		var accResponse = await (IsDemo
			? _service.Sandbox.GetSandboxAccountsAsync(new(), cancellationToken: cancellationToken)
			: _service.Users.GetAccountsAsync(new(), cancellationToken: cancellationToken)
		);

		foreach (var account in accResponse.Accounts)
		{
			_accountIds.Add(account.Id);

			await SendOutMessageAsync(new PortfolioMessage
			{
				PortfolioName = account.Id,
				OriginalTransactionId = transId
			}, cancellationToken);

			var pfResponse = await (IsDemo
				? _service.Sandbox.GetSandboxPortfolioAsync(new() { AccountId = account.Id }, cancellationToken: cancellationToken)
				: _service.Operations.GetPortfolioAsync(new() { AccountId = account.Id }, cancellationToken: cancellationToken)
			);

			await SendOutMessageAsync(new PortfolioMessage
			{
				PortfolioName = pfResponse.AccountId,
				OriginalTransactionId = transId
			}, cancellationToken);

			await processResponse(pfResponse);
		}

		if (!lookupMsg.IsHistoryOnly() && !IsDemo)
		{
			var (cts, pfToken) = cancellationToken.CreateChildToken();

			_pfCts.Add(transId, cts);

			_ = Task.Run(async () =>
			{
				var currentDelay = _baseDelay;

				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						var pfStream = _service.OperationsStream.PortfolioStream(new() { Accounts = { _accountIds } }, cancellationToken: pfToken).ResponseStream;

						await foreach (var response in pfStream.ReadAllAsync(pfToken).WithEnforcedCancellation(cancellationToken))
						{
							currentDelay = _baseDelay;

							if (response.Portfolio is PortfolioResponse portfolio)
								await processResponse(portfolio);
						}
					}
					catch (Exception ex)
					{
						if (pfToken.IsCancellationRequested)
							break;

						this.AddErrorLog(ex);

						currentDelay = GetCurrentDelay(currentDelay);
						await currentDelay.Delay(cancellationToken);
					}
				}
			}, pfToken);

			_ = Task.Run(async () =>
			{
				var currentDelay = _baseDelay;

				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						var posStream = _service.OperationsStream.PositionsStream(new() { Accounts = { _accountIds } }, cancellationToken: pfToken).ResponseStream;

						await foreach (var response in posStream.ReadAllAsync(pfToken).WithEnforcedCancellation(cancellationToken))
						{
							currentDelay = _baseDelay;

							if (response.Position is PositionData position)
							{
								var time = position.Date.ToDateTime();
								var account = position.AccountId;

								//if (position.Money is not null)
								//{
								//	foreach (var money in position.Money)
								//	{
								//		SendOutMessage(new PositionChangeMessage
								//		{
								//			SecurityId = SecurityId.Money,
								//			PortfolioName = account,
								//			ServerTime = time,
								//			OriginalTransactionId = transId,
								//		}
								//		.TryAdd(PositionChangeTypes.CurrentValue, money.AvailableValue?.ToDecimal(), true)
								//		.TryAdd(PositionChangeTypes.BlockedValue, money.BlockedValue?.ToDecimal(), true)
								//		);
								//	}
								//}

								if (position.Securities is not null)
								{
									foreach (var sec in position.Securities)
									{
										await SendOutMessageAsync(new PositionChangeMessage
										{
											SecurityId = sec.InstrumentUid.FromInstrumentIdToSecId(),
											PortfolioName = account,
											ServerTime = time,
											OriginalTransactionId = transId,
										}
										.TryAdd(PositionChangeTypes.CurrentValue, (decimal)sec.Balance, true)
										.TryAdd(PositionChangeTypes.BlockedValue, (decimal)sec.Blocked, true)
										, cancellationToken);
									}
								}

								if (position.Futures is not null)
								{
									foreach (var fut in position.Futures)
									{
										await SendOutMessageAsync(new PositionChangeMessage
										{
											SecurityId = fut.InstrumentUid.FromInstrumentIdToSecId(),
											PortfolioName = account,
											ServerTime = time,
											OriginalTransactionId = transId,
										}
										.TryAdd(PositionChangeTypes.CurrentValue, (decimal)fut.Balance, true)
										.TryAdd(PositionChangeTypes.BlockedValue, (decimal)fut.Blocked, true)
										, cancellationToken);
									}
								}

								if (position.Options is not null)
								{
									foreach (var opt in position.Options)
									{
										await SendOutMessageAsync(new PositionChangeMessage
										{
											SecurityId = opt.InstrumentUid.FromInstrumentIdToSecId(),
											PortfolioName = account,
											ServerTime = time,
											OriginalTransactionId = transId,
										}
										.TryAdd(PositionChangeTypes.CurrentValue, (decimal)opt.Balance, true)
										.TryAdd(PositionChangeTypes.BlockedValue, (decimal)opt.Blocked, true)
										, cancellationToken);
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						if (pfToken.IsCancellationRequested)
							break;

						this.AddErrorLog(ex);

						currentDelay = GetCurrentDelay(currentDelay);
						await currentDelay.Delay(cancellationToken);
					}
				}
			}, pfToken);
		}

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}
}