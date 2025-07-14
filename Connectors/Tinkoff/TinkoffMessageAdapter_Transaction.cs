namespace StockSharp.Tinkoff;

public partial class TinkoffMessageAdapter
{
	private readonly SynchronizedDictionary<long, CancellationTokenSource> _ordersCts = [];
	private readonly SynchronizedDictionary<long, CancellationTokenSource> _pfCts = [];
	private readonly CachedSynchronizedSet<string> _accountIds = [];
	private readonly SynchronizedPairSet<long, Guid> _orderUids = [];

	/// <inheritdoc/>
	public override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
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
				Price = regMsg.Price,
				Quantity = (long)regMsg.Volume,
				ExpireDate = regMsg.TillDate?.ToTimestamp(),
				ExpirationType = regMsg.TimeInForce.ToStopNative(regMsg.TillDate),
				StopOrderType = ((ITakeProfitOrderCondition)condition).ActivationPrice is not null ? StopOrderType.TakeProfit : (((IStopLossOrderCondition)condition).ClosePositionPrice is not null ? StopOrderType.StopLimit : StopOrderType.StopLoss),
				StopPrice = condition.TriggerPrice,
			};

			if (stopOrder.StopOrderType == StopOrderType.TakeProfit && condition.IsTrailing)
				stopOrder.TakeProfitType = TakeProfitType.Trailing;

			var response = await _service.StopOrders.PostStopOrderAsync(stopOrder, cancellationToken: cancellationToken);

			SendOutMessage(new ExecutionMessage
			{
				HasOrderInfo = true,
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = transId,
				OrderStringId = response.StopOrderId,
				OrderState = OrderStates.Active,
			});
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
					Price = regMsg.Price,
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
					Price = regMsg.Price,
					Quantity = (long)regMsg.Volume,
					TimeInForce = regMsg.TimeInForce.ToNative(regMsg.TillDate),
				}, cancellationToken: cancellationToken);
			}

			SendOutMessage(new ExecutionMessage
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
			});
		}
	}

	/// <inheritdoc/>
	public override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderType == OrderTypes.Conditional)
		{
			if (IsDemo)
				throw new NotSupportedException();

			var response = await _service.StopOrders.CancelStopOrderAsync(new()
			{
				StopOrderId = cancelMsg.OrderStringId,
			}, cancellationToken: cancellationToken);

			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = cancelMsg.TransactionId,
				ServerTime = response.Time.ToDateTimeOffset(),
				OrderState = OrderStates.Done,
			});
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
	public override async ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
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
					Price = replaceMsg.Price,
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
					Price = replaceMsg.Price,
					Quantity = (long)replaceMsg.Volume,
					AccountId = replaceMsg.PortfolioName,
					IdempotencyKey = replaceMsg.TransactionId.To<string>(),
				}, cancellationToken: cancellationToken);
			}
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
	public override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(statusMsg.TransactionId);

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

				SendOutMessage(new ExecutionMessage
				{
					HasOrderInfo = true,
					DataTypeEx = DataType.Transactions,
					TransactionId = transId,
					OriginalTransactionId = statusMsg.TransactionId,
					SecurityId = order.InstrumentUid.FromInstrumentIdToSecId(),
					PortfolioName = accountId,
					Currency = order.Currency.To<CurrencyTypes?>(),
					OrderStringId = order.OrderId,
					ServerTime = order.OrderDate.ToDateTimeOffset(),
					Side = order.Direction.ToSide(),
					OrderType = order.OrderType.ToOrderType(),
					OrderPrice = order.InitialSecurityPrice?.ToDecimal() ?? default,
					Commission = order.InitialCommission ?? order.ExecutedCommission,
					OrderVolume = order.LotsRequested,
					Balance = order.LotsRequested - order.LotsExecuted,
					AveragePrice = order.ExecutedOrderPrice?.ToDecimal(),
					OrderState = order.ExecutionReportStatus.ToOrderState(),
				});

				foreach (var trade in order.Stages)
				{
					SendOutMessage(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						OriginalTransactionId = transId,
						TradeStringId = trade.TradeId,
						TradePrice = trade.Price?.ToDecimal(),
						TradeVolume = trade.Quantity,
						ServerTime = trade.ExecutionTime.ToDateTimeOffset(),
					});
				}
			}

			if (IsDemo)
			{

			}
			else
			{
				foreach (var order in (await _service.StopOrders.GetStopOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)).StopOrders)
				{
					SendOutMessage(new ExecutionMessage
					{
						HasOrderInfo = true,
						DataTypeEx = DataType.Transactions,
						OriginalTransactionId = statusMsg.TransactionId,
						SecurityId = order.InstrumentUid.FromInstrumentIdToSecId(),
						PortfolioName = accountId,
						Currency = order.Currency.To<CurrencyTypes?>(),
						OrderStringId = order.StopOrderId,
						ServerTime = order.CreateDate.ToDateTimeOffset(),
						Side = order.Direction.ToSide(),
						OrderType = OrderTypes.Conditional,
						OrderPrice = order.Price?.ToDecimal() ?? default,
						OrderVolume = order.LotsRequested,
						ExpiryDate = order.ExpirationTime.ToDateTimeOffset(),
						Condition = new TinkoffOrderCondition
						{
							TriggerPrice = order.StopPrice,
						},
					});
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
					SendOutMessage(new ExecutionMessage
					{
						SecurityId = secId,
						DataTypeEx = DataType.Transactions,
						OriginalTransactionId = transId,
						TradeStringId = trade.TradeId,
						TradePrice = trade.Price?.ToDecimal(),
						TradeVolume = trade.Quantity,
						ServerTime = trade.DateTime.ToDateTimeOffset(),
					});
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

						await foreach (var response in statesStream.ReadAllAsync(statesToken))
						{
							currentDelay = _baseDelay;

							var orderState = response.OrderState;

							if (orderState is null)
								continue;

							if (!tryGetTransId(orderState.OrderRequestId, out var transId))
								continue;

							SendOutMessage(new ExecutionMessage
							{
								DataTypeEx = DataType.Transactions,
								HasOrderInfo = true,
								OriginalTransactionId = transId,
								ServerTime = CurrentTime,
								OrderStringId = orderState.OrderId,
								OrderState = orderState.ExecutionReportStatus.ToOrderState(),
								Balance = orderState.LotsRequested - orderState.LotsExecuted,
								AveragePrice = orderState.ExecutedOrderPrice?.ToDecimal(),
							});

							var secId = orderState.InstrumentUid.FromInstrumentIdToSecId();

							if (orderState.Trades is not null)
							{
								foreach (var trade in orderState.Trades)
								{
									SendOutMessage(new ExecutionMessage
									{
										DataTypeEx = DataType.Transactions,
										SecurityId = secId,
										OrderStringId = orderState.OrderId,
										OriginalTransactionId = transId,
										ServerTime = trade.DateTime.ToDateTimeOffset(),
										TradeStringId = trade.TradeId,
										TradePrice = trade.Price?.ToDecimal(),
										TradeVolume = trade.Quantity,
									});
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

		SendSubscriptionResult(statusMsg);
	}

	/// <inheritdoc/>
	public override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var transId = lookupMsg.TransactionId;

		SendSubscriptionReply(transId);

		if (!lookupMsg.IsSubscribe)
		{
			if (_pfCts.TryGetAndRemove(lookupMsg.OriginalTransactionId, out var cts1))
				cts1.Cancel();

			return;
		}

		void processResponse(PortfolioResponse portfolio)
		{
			SendOutMessage(new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				PortfolioName = portfolio.AccountId,
				ServerTime = CurrentTime,
				OriginalTransactionId = transId,
			}
			.TryAdd(PositionChangeTypes.CurrentValue, portfolio.TotalAmountPortfolio?.ToDecimal(), true)
			.TryAdd(PositionChangeTypes.RealizedPnL, portfolio.ExpectedYield?.ToDecimal(), true)
			);

			foreach (var position in portfolio.Positions)
			{
				SendOutMessage(new PositionChangeMessage
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
				);
			}
		}

		var accResponse = await (IsDemo
			? _service.Sandbox.GetSandboxAccountsAsync(new(), cancellationToken: cancellationToken)
			: _service.Users.GetAccountsAsync(new(), cancellationToken: cancellationToken)
		);

		foreach (var account in accResponse.Accounts)
		{
			_accountIds.Add(account.Id);

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = account.Id,
				OriginalTransactionId = transId
			});

			var pfResponse = await (IsDemo
				? _service.Sandbox.GetSandboxPortfolioAsync(new() { AccountId = account.Id }, cancellationToken: cancellationToken)
				: _service.Operations.GetPortfolioAsync(new() { AccountId = account.Id }, cancellationToken: cancellationToken)
			);

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = pfResponse.AccountId,
				OriginalTransactionId = transId
			});

			processResponse(pfResponse);
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

						await foreach (var response in pfStream.ReadAllAsync(pfToken))
						{
							currentDelay = _baseDelay;

							if (response.Portfolio is PortfolioResponse portfolio)
								processResponse(portfolio);
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

						await foreach (var response in posStream.ReadAllAsync(pfToken))
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
										SendOutMessage(new PositionChangeMessage
										{
											SecurityId = sec.InstrumentUid.FromInstrumentIdToSecId(),
											PortfolioName = account,
											ServerTime = time,
											OriginalTransactionId = transId,
										}
										.TryAdd(PositionChangeTypes.CurrentValue, (decimal)sec.Balance, true)
										.TryAdd(PositionChangeTypes.BlockedValue, (decimal)sec.Blocked, true)
										);
									}
								}

								if (position.Futures is not null)
								{
									foreach (var fut in position.Futures)
									{
										SendOutMessage(new PositionChangeMessage
										{
											SecurityId = fut.InstrumentUid.FromInstrumentIdToSecId(),
											PortfolioName = account,
											ServerTime = time,
											OriginalTransactionId = transId,
										}
										.TryAdd(PositionChangeTypes.CurrentValue, (decimal)fut.Balance, true)
										.TryAdd(PositionChangeTypes.BlockedValue, (decimal)fut.Blocked, true)
										);
									}
								}

								if (position.Options is not null)
								{
									foreach (var opt in position.Options)
									{
										SendOutMessage(new PositionChangeMessage
										{
											SecurityId = opt.InstrumentUid.FromInstrumentIdToSecId(),
											PortfolioName = account,
											ServerTime = time,
											OriginalTransactionId = transId,
										}
										.TryAdd(PositionChangeTypes.CurrentValue, (decimal)opt.Balance, true)
										.TryAdd(PositionChangeTypes.BlockedValue, (decimal)opt.Blocked, true)
										);
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

		SendSubscriptionResult(lookupMsg);
	}
}