namespace StockSharp.Tinkoff;

public partial class TinkoffMessageAdapter
{
	private readonly SynchronizedDictionary<long, CancellationTokenSource> _ordersCts = new();
	private readonly SynchronizedDictionary<long, CancellationTokenSource> _pfCts = new();
	private readonly CachedSynchronizedSet<string> _accountIds = new();

	/// <inheritdoc/>
	public override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		if (regMsg.OrderType == OrderTypes.Conditional)
		{
			if (IsDemo)
				throw new NotSupportedException();

			var condition = (TinkoffOrderCondition)regMsg.Condition;

			var response = await _service.StopOrders.PostStopOrderAsync(new()
			{
				OrderId = regMsg.TransactionId.To<string>(),
				AccountId = regMsg.PortfolioName,
				Direction = regMsg.Side.ToStopNative(),
				InstrumentId = regMsg.GetInstrumentId(),
				Price = regMsg.Price,
				Quantity = (long)regMsg.Volume,
				ExpireDate = regMsg.TillDate?.ToTimestamp(),
				ExpirationType = regMsg.TimeInForce.ToNative(regMsg.TillDate),
				StopOrderType = ((ITakeProfitOrderCondition)condition).ActivationPrice is not null ? StopOrderType.TakeProfit : (((IStopLossOrderCondition)condition).ClosePositionPrice is not null ? StopOrderType.StopLimit : StopOrderType.StopLoss),
				StopPrice = condition.TriggerPrice,
			}, cancellationToken: cancellationToken);

			SendOutMessage(new ExecutionMessage
			{
				HasOrderInfo = true,
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = regMsg.TransactionId,
				OrderStringId = response.StopOrderId,
				OrderState = OrderStates.Active,
			});
		}
		else
		{
			PostOrderResponse response;

			if (IsDemo)
			{
				response = await _service.Sandbox.PostSandboxOrderAsync(new()
				{
					AccountId = regMsg.PortfolioName,
					Direction = regMsg.Side.ToNative(),
					InstrumentId = regMsg.GetInstrumentId(),
					OrderId = regMsg.TransactionId.To<string>(),
					OrderType = regMsg.OrderType.ToNative(),
					Price = regMsg.Price,
					Quantity = (long)regMsg.Volume,
				}, cancellationToken: cancellationToken);
			}
			else
			{
				response = await _service.Orders.PostOrderAsync(new()
				{
					AccountId = regMsg.PortfolioName,
					Direction = regMsg.Side.ToNative(),
					InstrumentId = regMsg.GetInstrumentId(),
					OrderId = regMsg.TransactionId.To<string>(),
					OrderType = regMsg.OrderType.ToNative(),
					Price = regMsg.Price,
					Quantity = (long)regMsg.Volume,
				}, cancellationToken: cancellationToken);
			}

			SendOutMessage(new ExecutionMessage
			{
				HasOrderInfo = true,
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = regMsg.TransactionId,
				OrderStringId = response.OrderId,
				OrderState = response.ExecutionReportStatus.ToOrderState(),
				Error = response.ExecutionReportStatus == OrderExecutionReportStatus.ExecutionReportStatusRejected ? new InvalidOperationException(response.Message) : null,
				Commission = response.ExecutedCommission,
				Balance = response.LotsRequested - response.LotsExecuted,
				AveragePrice = response.ExecutedOrderPrice,
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

		var accountIds = statusMsg.PortfolioName.IsEmpty() ? await EnsureGetAccounts(cancellationToken) : new[] { statusMsg.PortfolioName };

		foreach (var accountId in accountIds)
		{
			var ordersResponse = await (IsDemo
				? _service.Sandbox.GetSandboxOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
				: _service.Orders.GetOrdersAsync(new() { AccountId = accountId }, cancellationToken: cancellationToken)
			);

			foreach (var order in ordersResponse.Orders)
			{
				if (!long.TryParse(order.OrderRequestId, out var transId))
					continue;

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
					OrderPrice = order.InitialSecurityPrice,
					Commission = order.InitialCommission ?? order.ExecutedCommission,
					OrderVolume = order.LotsRequested,
					Balance = order.LotsRequested - order.LotsExecuted,
					AveragePrice = order.ExecutedOrderPrice,
					OrderState = order.ExecutionReportStatus.ToOrderState(),
				});

				foreach (var trade in order.Stages)
				{
					SendOutMessage(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						OriginalTransactionId = transId,
						TradeStringId = trade.TradeId,
						TradePrice = trade.Price,
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
				foreach (var order in (await _service.StopOrders.GetStopOrdersAsync(new() { AccountId = accountId, From = statusMsg.From?.ToTimestamp(), To = statusMsg.To?.ToTimestamp() }, cancellationToken: cancellationToken)).StopOrders)
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
						OrderPrice = order.Price,
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
				if (!long.TryParse(op.ParentOperationId ?? op.Id, out var transId))
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
						TradePrice = trade.Price,
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
				var currError = 0;

				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						var statesStream = _service.OrdersStream.OrderStateStream(new(), cancellationToken: statesToken).ResponseStream;
						currError = 0;

						await foreach (var response in statesStream.ReadAllAsync(statesToken))
						{
							var orderState = response.OrderState;

							if (orderState is null)
								continue;

							if (!long.TryParse(orderState.OrderRequestId, out var transId))
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
								AveragePrice = orderState.ExecutedOrderPrice,
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
										TradePrice = trade.Price,
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

						if (++currError >= 10)
							break;
					}
				}
			}, statesToken);
		}

		SendSubscriptionResult(statusMsg);
	}

	/// <inheritdoc/>
	public override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(lookupMsg.TransactionId);

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
			}
			.TryAdd(PositionChangeTypes.CurrentValue, (decimal?)portfolio.TotalAmountPortfolio, true)
			.TryAdd(PositionChangeTypes.RealizedPnL, (decimal?)portfolio.ExpectedYield, true)
			.TryAdd(PositionChangeTypes.CurrentPrice, (decimal?)portfolio.TotalAmountPortfolio, true)
			);

			foreach (var position in portfolio.Positions)
			{
				SendOutMessage(new PositionChangeMessage
				{
					SecurityId = position.InstrumentUid.FromInstrumentIdToSecId(),
					PortfolioName = portfolio.AccountId,
					ServerTime = CurrentTime,
				}
				.TryAdd(PositionChangeTypes.CurrentValue, (decimal?)position.Quantity, true)
				.TryAdd(PositionChangeTypes.AveragePrice, (decimal?)position.AveragePositionPrice, true)
				.TryAdd(PositionChangeTypes.RealizedPnL, (decimal?)position.ExpectedYield ?? (decimal?)position.CurrentNkd, true)
				.TryAdd(PositionChangeTypes.CurrentPrice, (decimal?)position.CurrentPrice, true)
				.TryAdd(PositionChangeTypes.VariationMargin, (decimal?)position.VarMargin, true)
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
				OriginalTransactionId = lookupMsg.TransactionId
			});

			var pfResponse = await (IsDemo
				? _service.Sandbox.GetSandboxPortfolioAsync(new() { AccountId = account.Id }, cancellationToken: cancellationToken)
				: _service.Operations.GetPortfolioAsync(new() { AccountId = account.Id }, cancellationToken: cancellationToken)
			);

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = pfResponse.AccountId,
				OriginalTransactionId = lookupMsg.TransactionId
			});

			processResponse(pfResponse);
		}

		if (!lookupMsg.IsHistoryOnly() && !IsDemo)
		{
			var (cts, pfToken) = cancellationToken.CreateChildToken();

			_pfCts.Add(lookupMsg.TransactionId, cts);

			_ = Task.Run(async () =>
			{
				var currError = 0;

				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						var pfStream = _service.OperationsStream.PortfolioStream(new(), cancellationToken: pfToken).ResponseStream;
						currError = 0;

						await foreach (var response in pfStream.ReadAllAsync(pfToken))
						{
							if (response.Portfolio is PortfolioResponse portfolio)
								processResponse(portfolio);
						}
					}
					catch (Exception ex)
					{
						if (pfToken.IsCancellationRequested)
							break;

						this.AddErrorLog(ex);

						if (++currError >= 10)
							break;
					}
				}
			}, pfToken);
		}

		SendSubscriptionResult(lookupMsg);
	}
}