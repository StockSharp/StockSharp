#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Sterling.Sterling
File: SterlingMessageAdapter_Transaction.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Sterling
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using SterlingLib;

	using StockSharp.Algo;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter for Sterling.
	/// </summary>
	partial class SterlingMessageAdapter
	{
		private void ProcessOrderRegisterMessage(OrderRegisterMessage regMsg)
		{
			var condition = (SterlingOrderCondition)regMsg.Condition;
			var orderType = regMsg.OrderType.Value;

			var order = new STIOrder
			{
				Account = regMsg.PortfolioName, 
				Quantity = (int)regMsg.Volume,
				Display = (int)(regMsg.VisibleVolume ?? regMsg.Volume), 
				ClOrderID = regMsg.TransactionId.To<string>(), 
				LmtPrice = (double)regMsg.Price, 
				Symbol = regMsg.SecurityId.SecurityCode, 
				Destination = regMsg.SecurityId.BoardCode, 
				Tif = regMsg.TimeInForce.ToSterlingTif(regMsg.TillDate), 
				PriceType = orderType.ToSterlingPriceType(condition), 
				User = regMsg.Comment, 
				Side = regMsg.Side.ToSterlingSide()
			};

			if (regMsg.TillDate != null && regMsg.TillDate != DateTimeOffset.MaxValue)
				order.EndTime = regMsg.TillDate.Value.ToString("yyyyMMdd");

			if (regMsg.Currency != null)
				order.Currency = regMsg.Currency.ToString();

			if (orderType == OrderTypes.Conditional)
			{
				order.Discretion = condition.Discretion.ToDouble();
				order.ExecInst = condition.ExecutionInstruction.ToSterling();
				order.ExecBroker = condition.ExecutionBroker;
				order.ExecPriceLmt = condition.ExecutionPriceLimit.ToDouble();
				order.PegDiff = condition.PegDiff.ToDouble();
				order.TrailAmt = condition.TrailingVolume.ToDouble();
				order.TrailInc = condition.TrailingIncrement.ToDouble();
				order.StpPrice = condition.StopPrice.ToDouble();
				order.MinQuantity = condition.MinVolume.ToInt();
				order.AvgPriceLmt = condition.AveragePriceLimit.ToDouble();
				order.Duration = condition.Duration ?? 0;

				order.LocateBroker = condition.LocateBroker;
				order.LocateQty = condition.LocateVolume.ToInt();
				order.LocateTime = condition.LocateTime.ToSterling();

				order.OpenClose = condition.Options.IsOpen.ToSterling();
				order.Maturity = condition.Options.Maturity.ToSterling();
				order.PutCall = condition.Options.Type.ToSterling();
				order.Underlying = condition.Options.UnderlyingCode;
				order.CoverUncover = condition.Options.IsCover.ToSterling();
				order.Instrument = condition.Options.UnderlyingType.ToSterling();
				order.StrikePrice = condition.Options.StrikePrice.ToDouble();
			}

			order.SubmitOrder();
		}

		private void ProcessOrderCancelMessage(OrderCancelMessage cancelMsg)
		{
			var orderMaint = new STIOrderMaint();
			orderMaint.CancelOrder(cancelMsg.PortfolioName, 0, cancelMsg.TransactionId.To<string>(), null);
		}

		private void ProcessOrderReplaceMessage(OrderReplaceMessage replaceMsg)
		{
			var condition = (SterlingOrderCondition)replaceMsg.Condition;

			var replaceOrder = new STIOrder
			{
				Account = replaceMsg.PortfolioName, 
				Quantity = (int) replaceMsg.Volume,
				Display = (int)(replaceMsg.VisibleVolume ?? replaceMsg.Volume), 
				ClOrderID = replaceMsg.TransactionId.To<string>(), 
				LmtPrice = (double) replaceMsg.Price, 
				Symbol = replaceMsg.SecurityId.SecurityCode, 
				Destination = replaceMsg.SecurityId.BoardCode, 
				Tif = replaceMsg.TimeInForce.ToSterlingTif(replaceMsg.TillDate), 
				PriceType = replaceMsg.OrderType.Value.ToSterlingPriceType(condition), 
				User = replaceMsg.Comment
			};

			if (replaceMsg.TillDate != null && replaceMsg.TillDate != DateTimeOffset.MaxValue)
				replaceOrder.EndTime = replaceMsg.TillDate.Value.ToString("yyyyMMdd");

			if (replaceMsg.Currency != null)
				replaceOrder.Currency = replaceMsg.Currency.ToString();

			replaceOrder.ReplaceOrder(0, replaceMsg.OldTransactionId.To<string>());
		}

		private void ProcessOrderStatusMessage()
		{
			var orders = _client.GetOrders();

			foreach (var order in orders.Where(o => o.bstrClOrderId != ""))
			{
				SendOutMessage(new ExecutionMessage
				{
					ExecutionType = ExecutionTypes.Transaction,
					PortfolioName = order.bstrAccount,
					OrderVolume = order.nQuantity,
					VisibleVolume = order.nDisplay,
					OrderPrice = (decimal)order.fLmtPrice,
					Side = order.bstrSide.ToSide(),
					OriginalTransactionId = order.bstrClOrderId.To<long>(),
					SecurityId = new SecurityId { SecurityCode = order.bstrSymbol, BoardCode = order.bstrDestination },
					TimeInForce = order.bstrTif.ToTif(),
					OrderType = ((STIPriceTypes)order.nPriceType).ToPriceTypes(),
					Comment = order.bstrUser,
					OrderState = ((STIOrderStatus)order.nOrderStatus).ToOrderStates(),
					OrderId = order.nOrderRecordId,
					ServerTime = order.bstrOrderTime.StrToDateTime(),
					HasOrderInfo = true,
				});
			}

			var myTrades = _client.GetMyTrades();

			foreach (var trade in myTrades.Where(t => t.bstrClOrderId != ""))
			{
				SendOutMessage(new ExecutionMessage
				{
					ExecutionType = ExecutionTypes.Transaction,
					PortfolioName = trade.bstrAccount,
					Side = trade.bstrSide.ToSide(),
					OriginalTransactionId = trade.bstrClOrderId.To<long>(),
					SecurityId = new SecurityId { SecurityCode = trade.bstrSymbol, BoardCode = trade.bstrDestination },
					OrderType = Enum.GetName(typeof(STIPriceTypes), trade.nPriceType).To<STIPriceTypes>().ToPriceTypes(),
					OrderId = trade.nOrderRecordId,
					TradeId = trade.nTradeRecordId,
					TradePrice = (decimal)trade.fExecPrice,
					TradeVolume = trade.nQuantity,
					Commission = trade.bEcnFee,
					ServerTime = trade.bstrTradeTime.StrToDateTime(),
					HasTradeInfo = true,
				});
			}
		}

		private void SessionOnStiTradeUpdate(STITradeUpdateMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				ExecutionType = ExecutionTypes.Transaction,
				PortfolioName = msg.Account,
				TradePrice = (decimal)msg.ExecPrice,
				TradeVolume = msg.Quantity,
				Side = msg.Side.ToSide(),
				SecurityId = new SecurityId { SecurityCode = msg.Symbol, BoardCode = msg.Destination },
				OrderType = msg.PriceType.ToPriceTypes(),
				OrderId = msg.OrderRecordID,
				TradeId = msg.TradeRecordID,
				ServerTime = msg.TradeTime.StrToDateTime(),
				LocalTime = msg.UpdateTime.StrToDateTime(),
				HasTradeInfo = true,
			});
		}

		private void SessionOnStiOrderUpdate(STIOrderUpdateMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				ExecutionType = ExecutionTypes.Transaction,
				PortfolioName = msg.Account,
				OrderVolume = msg.Quantity,
				Side = msg.Side.ToSide(),
				Balance = msg.LvsQuantity,
				SecurityId = new SecurityId { SecurityCode = msg.Symbol, BoardCode = msg.Destination },
				OrderState = msg.OrderStatus.ToOrderStates(),
				OrderId = msg.OrderRecordID,
				VisibleVolume = msg.Display,
				ServerTime = msg.OrderTime.StrToDateTime(),
				LocalTime = msg.UpdateTime.StrToDateTime(),
				HasOrderInfo = true,
			});
		}

		private void SessionOnStiOrderReject(STIOrderRejectMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(),
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
				ServerTime = CurrentTime,
			});
		}

		private void SessionOnStiOrderConfirm(STIOrderConfirmMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				OrderState = OrderStates.Active,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
				ServerTime = CurrentTime,
			});
		}

		private void SessionOnStiAcctUpdate(ref structSTIAcctUpdate msg)
		{
		}

		private void SessionOnStiPositionUpdate(ref structSTIPositionUpdate msg)
		{
			var message = new PositionChangeMessage
			{
				PortfolioName = msg.bstrAcct,
				SecurityId = new SecurityId { SecurityCode = msg.bstrSym, BoardCode = AssociatedBoardCode },
			};

			message.TryAdd(PositionChangeTypes.RealizedPnL, (decimal)msg.fReal);
			message.TryAdd(PositionChangeTypes.BeginValue, (decimal)msg.nOpeningPosition);
			message.TryAdd(PositionChangeTypes.CurrentValue, (decimal)(msg.nOpeningPosition + (msg.nSharesBot - msg.nSharesSld)));
			message.TryAdd(PositionChangeTypes.Commission, (decimal)msg.fPositionCost);

			SendOutMessage(message);
		}

		private void ProcessPortfolioLookupMessage(PortfolioLookupMessage message)
		{
			var portfolios = _client.GetPortfolios();

			foreach (var portfolio in portfolios)
			{
				SendOutMessage(new PortfolioMessage
				{
					PortfolioName = portfolio.bstrAcct,
					State = PortfolioStates.Active, // ???
					OriginalTransactionId = message.TransactionId,
				});
			}

			var pos = _client.GetPositions();

			foreach (var position in pos)
			{
				var m = new PositionMessage
				{
					PortfolioName = position.bstrAcct,
					SecurityId = new SecurityId { SecurityCode = position.bstrSym, BoardCode = AssociatedBoardCode, SecurityType = position.bstrInstrument.ToSecurityType() },
					OriginalTransactionId = message.TransactionId,
				};

				SendOutMessage(m);

				var changeMsg = new PositionChangeMessage
				{
					PortfolioName = position.bstrAcct,
					SecurityId = new SecurityId { SecurityCode = position.bstrSym, BoardCode = AssociatedBoardCode, SecurityType = position.bstrInstrument.ToSecurityType() },
					ServerTime = CurrentTime
				};

				changeMsg.TryAdd(PositionChangeTypes.RealizedPnL, (decimal)position.fReal);
				changeMsg.TryAdd(PositionChangeTypes.BeginValue, (decimal)position.nOpeningPosition);
				changeMsg.TryAdd(PositionChangeTypes.CurrentValue, (decimal)(position.nOpeningPosition + (position.nSharesBot - position.nSharesSld)));
				changeMsg.TryAdd(PositionChangeTypes.Commission, (decimal)position.fPositionCost);

				SendOutMessage(message);
			}

			SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = message.TransactionId });
		}
	}
}