namespace StockSharp.LMAX
{
	using System;

	using Com.Lmax.Api.Account;
	using Com.Lmax.Api.Order;
	using Com.Lmax.Api.Position;
	using Com.Lmax.Api.Reject;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using LmaxOrder = Com.Lmax.Api.Order.Order;
	using LmaxTimeInForce = Com.Lmax.Api.TimeInForce;
	using StockSharpTimeInForce = StockSharp.Messages.TimeInForce;

	partial class LmaxMessageAdapter
	{
		private void ProcessOrderRegisterMessage(OrderRegisterMessage message)
		{
			var transactionId = message.TransactionId.To<string>();
			var lmaxSecId = (long)message.SecurityId.Native;

			LmaxTimeInForce tif;

			switch (message.TimeInForce)
			{
				case TimeInForce.PutInQueue:
					tif = message.TillDate == DateTimeOffset.MaxValue ? LmaxTimeInForce.GoodTilCancelled : LmaxTimeInForce.GoodForDay;
					break;
				case TimeInForce.MatchOrCancel:
					tif = LmaxTimeInForce.FillOrKill;
					break;
				case TimeInForce.CancelBalance:
					tif = LmaxTimeInForce.ImmediateOrCancel;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var volume = message.Volume;

			if (message.Side == Sides.Sell)
				volume = -volume;

			switch (message.OrderType)
			{
				case OrderTypes.Limit:
					Session.PlaceLimitOrder(new LimitOrderSpecification(transactionId, lmaxSecId, message.Price, volume, tif), id => { }, CreateErrorHandler("PlaceLimitOrder"));
					break;
				case OrderTypes.Market:
					Session.PlaceMarketOrder(new MarketOrderSpecification(transactionId, lmaxSecId, volume, tif), id => { }, CreateErrorHandler("PlaceMarketOrder"));
					break;
				case OrderTypes.Conditional:
					var condition = (LmaxOrderCondition)message.Condition;
					Session.PlaceStopOrder(new StopOrderSpecification(transactionId, lmaxSecId, message.Price, volume, tif, condition.StopLossOffset, condition.TakeProfitOffset), id => { }, CreateErrorHandler("PlaceStopOrder"));
					break;
				case OrderTypes.Repo:
				case OrderTypes.ExtRepo:
				case OrderTypes.Rps:
				case OrderTypes.Execute:
					throw new NotSupportedException(LocalizedStrings.Str1849Params.Put(message.OrderType));
				default:
					throw new ArgumentOutOfRangeException("message", message.OrderType, LocalizedStrings.Str1600);
			}
		}

		private void OnSessionPositionChanged(PositionEvent lmaxPos)
		{
			SendOutMessage(SessionHolder
				.CreatePositionChangeMessage(
						lmaxPos.AccountId.To<string>(),
						new SecurityId { Native = lmaxPos.InstrumentId }
					)
					.Add(PositionChangeTypes.CurrentValue, lmaxPos.OpenQuantity)
					.Add(PositionChangeTypes.CurrentPrice, lmaxPos.OpenCost));
		}

		private void OnSessionOrderChanged(LmaxOrder lmaxOrder)
		{
			var transactionId = TryParseTransactionId(lmaxOrder.InstructionId);

			if (transactionId == null)
				return;

			LmaxOrderCondition condition = null;
			decimal price = 0;
			OrderTypes orderType;

			switch (lmaxOrder.OrderType)
			{
				case OrderType.MARKET:
					orderType = OrderTypes.Market;
					break;
				case OrderType.LIMIT:
					orderType = OrderTypes.Limit;

					if (lmaxOrder.LimitPrice == null)
						throw new ArgumentException(LocalizedStrings.Str3394Params.Put(transactionId), "lmaxOrder");

					price = (decimal)lmaxOrder.LimitPrice;
					break;
				case OrderType.STOP_ORDER:
				case OrderType.STOP_LOSS_MARKET_ORDER:
				case OrderType.STOP_PROFIT_LIMIT_ORDER:
					orderType = OrderTypes.Conditional;

					if (lmaxOrder.StopPrice == null)
						throw new ArgumentException(LocalizedStrings.Str3395Params.Put(transactionId), "lmaxOrder");

					price = (decimal)lmaxOrder.StopPrice;

					condition = new LmaxOrderCondition
					{
						StopLossOffset = lmaxOrder.StopLossOffset,
						TakeProfitOffset = lmaxOrder.StopProfitOffset,
					};
					break;
				case OrderType.CLOSE_OUT_ORDER_POSITION:
				case OrderType.CLOSE_OUT_POSITION:
				case OrderType.SETTLEMENT_ORDER:
				case OrderType.OFF_ORDERBOOK:
				case OrderType.REVERSAL:
				case OrderType.UNKNOWN:
					orderType = OrderTypes.Execute;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var expiryDate = DateTimeOffset.MaxValue;
			var tif = StockSharpTimeInForce.PutInQueue;

			switch (lmaxOrder.TimeInForce)
			{
				case LmaxTimeInForce.FillOrKill:
					tif = StockSharpTimeInForce.MatchOrCancel;
					break;
				case LmaxTimeInForce.ImmediateOrCancel:
					tif = StockSharpTimeInForce.CancelBalance;
					break;
				case LmaxTimeInForce.GoodForDay:
					expiryDate = DateTime.Today.ApplyTimeZone(TimeZoneInfo.Utc);
					break;
				case LmaxTimeInForce.GoodTilCancelled:
					break;
				case LmaxTimeInForce.Unknown:
					throw new NotSupportedException(LocalizedStrings.Str3396Params.Put(lmaxOrder.TimeInForce, transactionId.Value, lmaxOrder.OrderId));
				default:
					throw new InvalidOperationException(LocalizedStrings.Str3397Params.Put(lmaxOrder.TimeInForce, transactionId.Value, lmaxOrder.OrderId));
			}

			var msg = new ExecutionMessage
			{
				SecurityId = new SecurityId { Native = lmaxOrder.InstrumentId },
				OriginalTransactionId = transactionId.Value,
				OrderType = orderType,
				Price = price,
				Condition = condition,
				Volume = lmaxOrder.Quantity.Abs(),
				Side = lmaxOrder.Quantity > 0 ? Sides.Buy : Sides.Sell,
				Balance = lmaxOrder.Quantity - lmaxOrder.FilledQuantity,
				PortfolioName = lmaxOrder.AccountId.To<string>(),
				TimeInForce = tif,
				ExpiryDate = expiryDate,
				OrderStringId = lmaxOrder.OrderId,
				ExecutionType = ExecutionTypes.Order,
				Commission = lmaxOrder.Commission,
				ServerTime = SessionHolder.CurrentTime.Convert(TimeZoneInfo.Utc)
			};

			msg.OrderState = lmaxOrder.CancelledQuantity > 0
								? OrderStates.Done
								: (msg.Balance == 0 ? OrderStates.Done : OrderStates.Active);

			//msg.Action = lmaxOrder.CancelledQuantity > 0
			//				 ? ExecutionActions.Canceled
			//				 : (lmaxOrder.FilledQuantity == 0 ? ExecutionActions.Registered : ExecutionActions.Matched);

			SendOutMessage(msg);
		}

		private void OnSessionOrderExecuted(Execution execution)
		{
			var transactionId = TryParseTransactionId(execution.Order.InstructionId);

			if (transactionId == null)
				return;

			SendOutMessage(new ExecutionMessage
			{
				SecurityId = new SecurityId { Native = execution.Order.InstrumentId },
				OriginalTransactionId = transactionId.Value,
				TradeId = execution.ExecutionId,
				TradePrice = execution.Price,
				Volume = execution.Quantity.Abs(),
				ExecutionType = ExecutionTypes.Trade,
				Side = execution.Order.Quantity > 0 ? Sides.Buy : Sides.Sell,
				Commission = execution.Order.Commission,
				ServerTime = SessionHolder.CurrentTime.Convert(TimeZoneInfo.Utc)
			});
		}

		private void OnSessionAccountStateUpdated(AccountStateEvent accountState)
		{
			SendOutMessage(
				SessionHolder
					.CreatePortfolioChangeMessage(accountState.AccountId.To<string>())
						.Add(PositionChangeTypes.CurrentPrice, accountState.Balance)
						.Add(PositionChangeTypes.VariationMargin, accountState.Margin)
						.Add(PositionChangeTypes.UnrealizedPnL, accountState.UnrealisedProfitAndLoss));
		}

		private void OnSessionInstructionRejected(InstructionRejectedEvent evt)
		{
			var transactionId = TryParseTransactionId(evt.InstructionId);

			if (transactionId == null)
				return;

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				SecurityId = new SecurityId { Native = evt.InstrumentId },
				OriginalTransactionId = transactionId.Value,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(evt.Reason),
				PortfolioName = evt.AccountId.To<string>(),
				ServerTime = SessionHolder.CurrentTime.Convert(TimeZoneInfo.Utc)
			});
		}
	}
}