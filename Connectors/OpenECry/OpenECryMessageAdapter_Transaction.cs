namespace StockSharp.OpenECry
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using OEC.API;
	using OEC.API.MarginCalculator;
	using OEC.Data;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class OpenECryMessageAdapter
	{
		private readonly PairSet<OEC.API.Order, long> _orderTransactions = new PairSet<OEC.API.Order, long>();

		//private static string GetOECAccountName(string localPortfolioName)
		//{
		//	return localPortfolioName.Split(new[] { '-' })[0];
		//}

		private void ProcessPortfolioLookupMessage(PortfolioLookupMessage message)
		{
			foreach (var account in SessionHolder.Session.Accounts)
			{
				ProcessAccount(account, null);

				foreach (var list in account.DetailedPositions)
				{
					foreach (var position in list)
						ProcessPosition(position);
				}
			}

			SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = message.TransactionId });
		}

		private void ProcessOrderRegister(OrderRegisterMessage message)
		{
			var draft = SessionHolder.Session.CreateDraft();

			draft.Comments = message.Comment;
			draft.Account = SessionHolder.Session.Accounts[message.PortfolioName];
			draft.Contract = SessionHolder.Session.Contracts[message.SecurityId.SecurityCode];
			draft.Route = SessionHolder.Session.Routes[message.SecurityId.BoardCode];
			draft.Side = message.Side.ToOec();
			draft.Quantity = (int)message.Volume;

			draft.ClearExtData();
			draft.Price = (double)message.Price;
			draft.Price2 = 0;

			if (message.OrderType == OrderTypes.Conditional)
			{
				var cond = (OpenECryOrderCondition)message.Condition;

				switch (cond.AssetType)
				{
					case OpenECryOrderCondition.AssetTypeEnum.All:
						switch (cond.StopType)
						{
							case OpenECryStopType.StopLimit:
								draft.Type = OrderType.StopLimit;
								draft.Price2 = draft.Price;
								draft.Price = (double)cond.StopPrice;
								break;
							case OpenECryStopType.StopMarket:
								draft.Type = OrderType.Stop;
								draft.Price = (double)cond.StopPrice;
								draft.Price2 = 0;
								break;
							default:
								throw new ArgumentException(LocalizedStrings.Str2553Params.Put(cond.StopType));
						}
						break;

					case OpenECryOrderCondition.AssetTypeEnum.Equity:
					case OpenECryOrderCondition.AssetTypeEnum.Future:

						//if (!draft.Contract.IsEquityAsset && !draft.Contract.IsFuture)
						//	throw new NotSupportedException(LocalizedStrings.Str2554);

						switch (cond.StopType)
						{
							case OpenECryStopType.TrailingStopLimit:
								draft.Type = OrderType.TrailingStopLimit;
								draft.Price2 = draft.Price;
								draft.Price = (double)cond.StopPrice;
								break;
							case OpenECryStopType.TrailingStopMarket:
								draft.Type = OrderType.TrailingStopLoss;
								draft.Price = (double)cond.StopPrice;
								draft.Price2 = 0;
								break;
							default:
								throw new ArgumentException(LocalizedStrings.Str2553Params.Put(cond.StopType));
						}

						if (cond.AssetType == OpenECryOrderCondition.AssetTypeEnum.Equity)
							draft.SetEquityTSData((double)cond.Delta, cond.IsPercentDelta, cond.TriggerType.ToOec());
						else
							draft.SetTSData((double)cond.ReferencePrice, (double)cond.Delta);

						break;
					default:
						throw new ArgumentException(LocalizedStrings.Str2555Params.Put(cond.AssetType));
				}
			}
			else
			{
				draft.Type = message.OrderType.ToOec();
			}

			draft.Flags = OrderFlags.None;
			draft.Start = OEC.API.Version.MinimumStart;
			draft.End = OEC.API.Version.MaximumEnd;

			if (message.TimeInForce == TimeInForce.MatchOrCancel)
			{
				draft.Flags = OrderFlags.FOK; // fill or kill
			}
			else if (message.ExpiryDate == DateTimeOffset.MaxValue)
			{
				draft.Flags = OrderFlags.GTC; // good till canceled
			}
			else if (message.ExpiryDate != DateTime.Today)
			{
				if (message.ExpiryDate != null)
					draft.End = message.ExpiryDate.Value.UtcDateTime;
			}

			if (message.VisibleVolume < message.Volume)
				draft.SetIcebergData((int)message.VisibleVolume);

			var invalid = draft.GetInvalidParts();
			if (invalid != OrderParts.None)
				throw new OpenECryException(LocalizedStrings.Str2556Params.Put(invalid.ToString()));

			var newOrder = SessionHolder.Session.SendOrder(draft);
			_orderTransactions.Add(newOrder, message.TransactionId);
			ProcessOrder(newOrder, message.TransactionId);
		}

		private void ProcessOrderCancel(OrderCancelMessage message)
		{
			SessionHolder.Session.CancelOrder(_orderTransactions[message.OrderTransactionId]);
		}

		private void ProcessOrderReplace(OrderReplaceMessage message)
		{
			var draft = SessionHolder.Session.CreateDraft(_orderTransactions[message.OldTransactionId]);

			draft.Price = (double)message.Price;
			draft.Quantity = (int)message.Volume;

			var invalid = draft.GetInvalidParts();
			if (invalid != OrderParts.None)
				throw new OpenECryException(LocalizedStrings.Str2556Params.Put(invalid.ToString()));

			var newOrder = SessionHolder.Session.ModifyOrder(draft);
			_orderTransactions.Add(newOrder, message.TransactionId);
			ProcessOrder(newOrder, message.TransactionId);
		}

		private void SessionOnBalanceChanged(OEC.API.Account account, OEC.API.Currency currency)
		{
			ProcessAccount(account, currency);
		}

		private void SessionOnAvgPositionChanged(OEC.API.Account account, OEC.API.Position position)
		{
			ProcessPosition(position);
		}

		private void ProcessPosition(OEC.API.Position position)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			SendOutMessage(SessionHolder
				.CreatePositionChangeMessage(
					position.Account.Name,
					new SecurityId
					{
						SecurityCode = position.Contract.Symbol,
						BoardCode = position.Contract.Exchange.Name,
					}
				)
			.TryAdd(PositionChangeTypes.BeginValue, (decimal)position.Prev.Volume)
			.TryAdd(PositionChangeTypes.CurrentValue, position.ContractSize.SafeCast())
			.TryAdd(PositionChangeTypes.CurrentPrice, position.CurrencyCostBasis.SafeCast())
			.TryAdd(PositionChangeTypes.RealizedPnL, position.CurrencyNetGain.SafeCast())
			.TryAdd(PositionChangeTypes.UnrealizedPnL, position.CurrencyOTE.SafeCast())
			.TryAdd(PositionChangeTypes.Commission, position.OpenCommissions.SafeCast() + position.RealizedCommissions.SafeCast())
			.TryAdd(PositionChangeTypes.VariationMargin, position.InitialMargin.SafeCast())
			.TryAdd(PositionChangeTypes.AveragePrice, position.Net.Price.SafeCast()));
		}

		private void SessionOnAllocationBlocksChanged(AllocationBlockList allocations)
		{

		}

		private void SessionOnAccountSummaryChanged(OEC.API.Account account, OEC.API.Currency currency)
		{
			ProcessAccount(account, currency);
		}

		private void ProcessAccount(OEC.API.Account account, OEC.API.Currency currency)
		{
			if (account == null)
				throw new ArgumentNullException("account");

			var balance = currency == null ? account.TotalBalance : account.Balances[currency];

			var commission = account.AvgPositions
				.Where(pos => balance == account.TotalBalance || pos.Contract.Currency.ID == balance.Currency.ID)
				.Sum(pos => pos.RealizedCommissions.SafeCast() + pos.OpenCommissions.SafeCast());

			var msg = SessionHolder.CreatePortfolioChangeMessage(account.Name)
				.TryAdd(PositionChangeTypes.BeginValue, balance.Cash.SafeCast())
				.TryAdd(PositionChangeTypes.RealizedPnL, balance.RealizedPnL.SafeCast())
				.TryAdd(PositionChangeTypes.UnrealizedPnL, balance.OpenPnL.SafeCast())
				.TryAdd(PositionChangeTypes.CurrentValue, balance.NetLiquidatingValue.SafeCast())
				.TryAdd(PositionChangeTypes.Commission, commission);

			SendOutMessage(msg);
		}

		private void SessionOnAccountRiskLimitChanged(OEC.API.Account account)
		{
			ProcessAccount(account, null);
		}

		private void SessionOnRiskLimitDetailsReceived(RiskLimitDetailList details)
		{

		}

		private void SessionOnPostAllocation(OEC.API.Order order, OEC.API.Contract contract, OEC.API.PostAllocationBlock allocation, PostAllocationCheckResult result)
		{

		}

		private void SessionOnPortfolioMarginChanged(OEC.API.Account account)
		{
			ProcessAccount(account, null);
		}

		private void SessionOnMarginCalculationCompleted(MarginCalculatorResponse response)
		{

		}

		private void SessionOnDetailedPositionChanged(OEC.API.Account account, OEC.API.Position position)
		{
			ProcessPosition(position);
		}

		private void SessionOnOrderStateChanged(OEC.API.Order order, OrderState oldState)
		{
			var trId = _orderTransactions.TryGetValue2(order);

			// TODO
			if (trId == null)
				return;

			ProcessOrder(order, trId.Value);
		}

		private void ProcessOrder(OEC.API.Order order, long trasactionId)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (trasactionId == 0)
				throw new ArgumentOutOfRangeException("trasactionId");

			var execMsg = new ExecutionMessage
			{
				OriginalTransactionId = trasactionId,
				OrderId = order.ID,
				Side = order.Side.ToStockSharp(),
				ExecutionType = ExecutionTypes.Order,
				ServerTime = order.States.Current.Timestamp.ApplyTimeZone(TimeHelper.Est),
				OrderType = order.Type.ToStockSharp(),
				Volume = order.Quantity,
				PortfolioName = order.Account.Name,
				SecurityId = new SecurityId
				{
					SecurityCode = order.Contract.Symbol,
					BoardCode = order.Route == null ? order.Contract.Exchange.Name : order.Route.Name,
				},
				Comment = order.Comments,
				Price = order.Contract.Cast(order.Price),
			};

			var currVersion = order.Versions.Current;

			switch (currVersion.Flags)
			{
				case OrderFlags.FOK:
					execMsg.TimeInForce = TimeInForce.MatchOrCancel;
					break;
				case OrderFlags.GTC:
					execMsg.ExpiryDate = DateTimeOffset.MaxValue;
					break;
				default:
					if (currVersion.End != OEC.API.Version.MaximumEnd)
						execMsg.ExpiryDate = currVersion.End;
					break;
			}

			if (execMsg.OrderType == OrderTypes.Conditional)
			{
				var condition = new OpenECryOrderCondition();
				execMsg.Condition = condition;

				switch (order.Type)
				{
					case OrderType.StopLimit:
						condition.StopType = OpenECryStopType.StopLimit;
						condition.StopPrice = order.Contract.Cast(order.Price);
						execMsg.Price = order.Contract.Cast(order.Price2);
						break;
					case OrderType.Stop:
						condition.StopType = OpenECryStopType.StopMarket;
						condition.StopPrice = order.Contract.Cast(order.Price);
						//execMsg.Price = 0;
						break;
					case OrderType.TrailingStopLoss:
					case OrderType.TrailingStopLimit:
						//execMsg.Price = 0;
						var stopType = order.Type == OrderType.TrailingStopLimit ? OpenECryStopType.TrailingStopLimit : OpenECryStopType.TrailingStopMarket;

						var eqtsData = currVersion.ExtData as EquityTrailingStopData;
						if (eqtsData != null)
						{
							condition.StopType = stopType;
							condition.Delta = eqtsData.Amount.SafeCast();
							condition.IsPercentDelta = eqtsData.IsPercentAmount;
							condition.TriggerType = eqtsData.TriggerType.ToStockSharp();
							condition.StopPrice = order.Contract.Cast(order.Price);
						}
						else
						{
							var tsData = currVersion.ExtData as OEC.API.TrailingStopData;

							if (tsData != null)
							{
								condition.StopType = stopType;
								condition.Delta = tsData.Delta.SafeCast();
								condition.ReferencePrice = tsData.ReferencePrice.SafeCast();
								condition.StopPrice = order.Contract.Cast(order.Price);
							}
						}

						break;
					default:
						throw new ArgumentOutOfRangeException(LocalizedStrings.Str1849Params.Put(order.Type));
				}
			}

			switch (order.CurrentState)
			{
				case OrderState.Accepted:
				{
					execMsg.OrderState = OrderStates.Pending;
					execMsg.OrderStatus = OrderStatus.Accepted;
					break;
				}
				case OrderState.Held:
				{
					execMsg.OrderState = OrderStates.Pending;
					execMsg.OrderStatus = OrderStatus.ReceiveByServer;
					break;
				}
				case OrderState.Sent:
				{
					execMsg.OrderState = OrderStates.Pending;
					execMsg.OrderStatus = OrderStatus.SentToServer;
					break;
				}
				case OrderState.Suspended:
				{
					break;
				}
				case OrderState.Rejected:
				{
					execMsg.OrderState = OrderStates.Failed;
					execMsg.OrderStatus = OrderStatus.RejectedBySystem;
					execMsg.Error = new InvalidOperationException(order.States.Current.Comments);

					break;
				}
				case OrderState.Working:
				{
					//execMsg.ExtensionInfo[_keyExchangeOrderId] = order.Versions.Current.Command.ExchangeOrderID;

					execMsg.OrderState = OrderStates.Active;
					execMsg.OrderStatus = OrderStatus.Accepted;

					execMsg.Balance = order.Quantity - order.Fills.TotalQuantity;

					break;
				}
				case OrderState.Completed:
				{
					execMsg.OrderState = OrderStates.Done;

					if (order.IsFilled)
					{
						execMsg.Balance = 0;
					}
					else
					{
						execMsg.Balance = order.Quantity - order.Fills.TotalQuantity;
					}

					break;
				}
				case OrderState.Cancelled:
				{
					execMsg.OrderState = OrderStates.Done;
					execMsg.Balance = order.Quantity - order.Fills.TotalQuantity;

					break;
				}
				case OrderState.None:
				{
					break;
				}
				case OrderState.Unknown:
				{
					break;
				}
				default:
					throw new OpenECryException(LocalizedStrings.Str2557Params.Put(order.CurrentState));
			}

			SendOutMessage(execMsg);
		}

		private void SessionOnOrderFilled(OEC.API.Order order, OEC.API.Fill fill)
		{
			if (!fill.Active)
				return;

			var commission = fill.Commission.SafeCast();

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Trade,
				OriginalTransactionId = _orderTransactions.TryGetValue2(order) ?? 0,
				OrderId = order.ID,
				TradeId = fill.ID,
				TradePrice = order.Contract.Cast(fill.Price),
				ServerTime = fill.Timestamp.ApplyTimeZone(TimeHelper.Est),
				Volume = fill.Quantity,
				SystemComment = fill.Comments,
				Commission = commission == 0 ? (decimal?)null : commission,
			});
		}

		private void SessionOnOrderConfirmed(OEC.API.Order order, int oldOrderId)
		{
			//if (oldOrderId > 0)
			//{
			//	SendOutMessage(new ExecutionMessage
			//	{
			//		ExecutionType = ExecutionTypes.Order,
			//		OrderId = oldOrderId,
			//		OrderState = OrderStates.Done,
			//	});	
			//}

			//SendOutMessage(new ExecutionMessage
			//{
			//	ExecutionType = ExecutionTypes.Order,
			//	OrderId = order.ID,
			//	OrderStatus = OrderStatus.ReceiveByServer,
			//});
		}

		private void SessionOnCompoundPositionGroupChanged(OEC.API.CompoundPositionGroup group, OEC.API.CompoundLegPosition position)
		{

		}

		private void SessionOnCommandUpdated(OEC.API.Order order, Command command)
		{
			//var msg = "Команда обновилась: #{0}, {1}-{2}, order #{3}".Put(command.ID, command.Type, command.State, oecOrder.OrderString());
			//this.AddInfoLog(msg);

			////ProcessEvents(() =>
			////{
			//var currentOrder = _orderMap.GetLocalOrder(oecOrder);

			//if (currentOrder == null)
			//	throw new OECException("CommandUpdated: Локальная заявка для OEC заявки '{0}' не найдена.".Put(oecOrder.OrderString()));

			//if (!(command.Type == CommandType.Modify || command.State == CommandState.Failed))
			//{
			//	// при необходимости будет обработано при получении OrderStateChanged
			//	currentOrder.Messages.Add(msg);
			//	return;
			//}

			//switch (command.State)
			//{
			//	case CommandState.Sent:
			//	{
			//		// Type == Modify
			//		var newOrder = _orderMap.GetLocalOrder(command.Version);
			//		if (newOrder == null)
			//			throw new OECException("CommandUpdated2: Локальная заявка для версии '{0}' не найдена.".Put(command.Version));

			//		if (command.ID > 0 && newOrder.Id <= 0)
			//		{
			//			var updatemsg = "OecCbOnCommandUpdated: Команда на модификацию (oldId={0}) принята сервером. newId={1}".Put(newOrder.Id, command.ID);
			//			this.AddInfoLog(updatemsg);

			//			GetOrder(newOrder.Security, newOrder.Type, command.ID, id => newOrder, order =>
			//			{
			//				_orderMap.BindOrderToOecOrderVersion(order, command.Version);
			//				order.Messages.Add(updatemsg);
			//				order.Status = OrderStatus.ReceiveByServer;

			//				return true;
			//			});
			//		}
			//		break;
			//	}
			//	case CommandState.Executed:
			//	{
			//		// Type == Modify
			//		var prevOecOrderVersion = oecOrder.Versions.Current.GetPreviousExecutedVersion();
			//		if (prevOecOrderVersion == null)
			//		{
			//			var errmsg = "Предыдущая версия заявки '{0}' не найдена.".Put(oecOrder.OrderString());
			//			this.AddErrorLog(errmsg);
			//			throw new OECException(errmsg);
			//		}

			//		var oldOrder = _orderMap.GetLocalOrder(prevOecOrderVersion);
			//		if (oldOrder == null)
			//			throw new OECException("CommandUpdated3: Локальная заявка для версии '{0}' не найдена.".Put(prevOecOrderVersion));

			//		OnModifyCommandExecuted(oldOrder, currentOrder, oecOrder);
			//		break;
			//	}
			//	case CommandState.Failed:
			//	{
			//		switch (command.Type)
			//		{
			//			case CommandType.Create:
			//				// will be handled in OnOrderChanged
			//				this.AddWarningLog("Команда Create вернула ошибку: {0}", command.ResultComments);
			//				break;
			//			case CommandType.Modify:
			//				var newOrder = _orderMap.GetLocalOrder(command.Version);
			//				if (newOrder == null)
			//					throw new OECException("CommandUpdated4: Локальная заявка для версии '{0}' не найдена.".Put(command.Version));

			//				newOrder.Id = 0;
			//				newOrder.State = OrderStates.Done;
			//				newOrder.Status = OrderStatus.NotValidated;
			//				newOrder.LastChangeTime = command.ResultTimestamp;

			//				RaiseOrderFailed(newOrder, new OECException("Команда Modify завершилась неудачно, комментарий='{0}'".Put(command.ResultComments)));
			//				break;
			//			case CommandType.Cancel:
			//				RaiseOrderFailed(currentOrder, new OECException("Команда Cancel завершилась неудачно, комментарий='{0}'".Put(command.ResultComments)));
			//				break;
			//		}
			//		break;
			//	}
			//}
			//});
		}

		private void ProcessOrderStatusMessage()
		{
			foreach (var order in SessionHolder.Session.Orders)
			{
				var trId = TransactionIdGenerator.GetNextId();
				_orderTransactions.Add(order, trId);
				ProcessOrder(order, trId);
			}
		}
	}
}