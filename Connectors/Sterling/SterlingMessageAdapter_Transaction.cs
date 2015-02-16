using System;
using System.Collections.Generic;
using System.Linq;
using SterlingLib;
using StockSharp.Messages;
using Ecng.Common;
using Ecng.Collections;

namespace StockSharp.Sterling
{
	partial class SterlingMessageAdapter
	{
		private void ProcessOrderRegisterMessage(OrderRegisterMessage regMsg)
		{
			var condition = (SterlingOrderCondition)regMsg.Condition;

			var order = new STIOrder
			{
				Account = regMsg.PortfolioName, 
				Quantity = (int) regMsg.Volume, 
				Display = (int) regMsg.VisibleVolume, 
				ClOrderID = regMsg.TransactionId.To<string>(), 
				LmtPrice = (double) regMsg.Price, 
				Symbol = regMsg.SecurityId.SecurityCode, 
				Destination = regMsg.SecurityId.BoardCode, 
				Tif = regMsg.TimeInForce.ToSterlingTif(regMsg.TillDate), 
				PriceType = regMsg.OrderType.ToSterlingPriceType(condition), 
				User = regMsg.Comment, 
				Side = regMsg.Side.ToSterlingSide()
			};

			if (regMsg.TillDate != DateTime.MaxValue)
				order.EndTime = regMsg.TillDate.ToString("yyyyMMdd");

			if (regMsg.Currency != null)
				order.Currency = regMsg.Currency.ToString();

			if (regMsg.OrderType == OrderTypes.Conditional)
			{
				//order.Discretion = condition.Discretion;
				//order.ExecInst = condition.ExecutionInstruction;
				//order.ExecBroker = condition.ExecutionBroker;
				//order.ExecPriceLmt = condition.ExecutionPriceLimit;
				//order.PegDiff = condition.PegDiff;
				//order.TrailAmt = condition.TrailingVolume;
				//order.TrailInc = condition.TrailingIncrement;
				//order.StpPrice = (double)(condition.StopPrice ?? 0);
				//order.MinQuantity = condition.MinVolume;
				//order.AvgPriceLmt = condition.AveragePriceLimit;
				//order.Duration = condition.Duration;

				//order.LocateBroker = condition.LocateBroker;
				//order.LocateQty = condition.LocateVolume;
				//order.LocateTime = condition.LocateTime;

				//order.OpenClose = condition.Options.IsOpen;
				//order.Maturity = condition.Options.Maturity;
				//order.PutCall = condition.Options.Type;
				//order.Underlying = condition.Options.UnderlyingCode;
				//order.CoverUncover = condition.Options.IsCover;
				//order.Instrument = condition.Options.UnderlyingType;
				//order.StrikePrice = condition.Options.StrikePrice;
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
				Display = (int) replaceMsg.VisibleVolume, 
				ClOrderID = replaceMsg.TransactionId.To<string>(), 
				LmtPrice = (double) replaceMsg.Price, 
				Symbol = replaceMsg.SecurityId.SecurityCode, 
				Destination = replaceMsg.SecurityId.BoardCode, 
				Tif = replaceMsg.TimeInForce.ToSterlingTif(replaceMsg.TillDate), 
				PriceType = replaceMsg.OrderType.ToSterlingPriceType(condition), 
				User = replaceMsg.Comment
			};

			if (replaceMsg.TillDate != DateTime.MaxValue)
				replaceOrder.EndTime = replaceMsg.TillDate.ToString("yyyyMMdd");

			if (replaceMsg.Currency != null)
				replaceOrder.Currency = replaceMsg.Currency.ToString();

			replaceOrder.ReplaceOrder(0, replaceMsg.OldTransactionId.To<string>());
		}

		private void ProcessExecutionMessage(ExecutionMessage executionMsg)
		{
			if (executionMsg.ExtensionInfo != null && executionMsg.ExtensionInfo.ContainsKey("GetMyTrades"))
			{
				var myTrades = SessionHolder.Session.GetMyTrades();

				foreach (var trade in myTrades.Where(t => t.bstrClOrderId != ""))
				{
					SendOutMessage(new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Trade,
						PortfolioName = trade.bstrAccount,
						Volume = trade.nQuantity,
						Price = (decimal)trade.fExecPrice,
						Side = trade.bstrSide.ToSide(),
						OriginalTransactionId = trade.bstrClOrderId.To<long>(),
						SecurityId = new SecurityId { SecurityCode = trade.bstrSymbol, BoardCode = trade.bstrDestination },
						OrderType = Enum.GetName(typeof(STIPriceTypes), trade.nPriceType).To<STIPriceTypes>().ToPriceTypes(),
						OrderId = trade.nOrderRecordId,
						TradeId = trade.nTradeRecordId,
						Commission = trade.bEcnFee,
						ServerTime = trade.bstrTradeTime.StrToDateTime()
					});
				}
			}

			if (executionMsg.ExtensionInfo != null && executionMsg.ExtensionInfo.ContainsKey("GetOrders"))
			{
				var orders = SessionHolder.Session.GetOrders();

				foreach (var order in orders.Where(o => o.bstrClOrderId != ""))
				{
					SendOutMessage(new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Order,
						PortfolioName = order.bstrAccount,
						Volume = order.nQuantity,
						VisibleVolume = order.nDisplay,
						Price = (decimal)order.fLmtPrice,
						Side = order.bstrSide.ToSide(),
						OriginalTransactionId = order.bstrClOrderId.To<long>(),
						SecurityId = new SecurityId { SecurityCode = order.bstrSymbol, BoardCode = order.bstrDestination },
						TimeInForce = order.bstrTif.ToTif(),
						OrderType = Enum.GetName(typeof(STIPriceTypes), order.nPriceType).To<STIPriceTypes>().ToPriceTypes(),
						Comment = order.bstrUser,
						OrderState = Enum.GetName(typeof(STIOrderStatus), order.nOrderStatus).To<STIOrderStatus>().ToOrderStates(),
						OrderId = order.nOrderRecordId,
						ServerTime = order.bstrOrderTime.StrToDateTime()
					});
				}
			}		
		}

		private void ProcessPositionMessage(PositionMessage posMsg)
		{
			if (posMsg.ExtensionInfo != null && posMsg.ExtensionInfo.ContainsKey("GetPositions"))
			{
				var pos = SessionHolder.Session.GetPositions();

				foreach (var position in pos)
				{
					var m = new PositionMessage
					{
						PortfolioName = position.bstrAcct,
						SecurityId = new SecurityId { SecurityCode = position.bstrSym, BoardCode = "All", SecurityType = position.bstrInstrument.ToSecurityType() },
					};

					SendOutMessage(m);

					var message = new PositionChangeMessage
					{
						PortfolioName = position.bstrAcct,
						SecurityId = new SecurityId { SecurityCode = position.bstrSym, BoardCode = "All", SecurityType = position.bstrInstrument.ToSecurityType() },
						ServerTime = SessionHolder.CurrentTime
					};

					message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.RealizedPnL, (decimal)position.fReal));
					message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.BeginValue, (decimal)position.nOpeningPosition));
					message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.CurrentValue, (decimal)(position.nOpeningPosition + (position.nSharesBot - position.nSharesSld))));
					message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.Commission, (decimal)position.fPositionCost));

					SendOutMessage(message);
				}
			}
		}

		private void ProcessPositionChangeMessage(PositionChangeMessage posChgMsg)
		{

		}

		private void SessionOnStiTradeUpdate(STITradeUpdateMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				ExecutionType = ExecutionTypes.Trade,
				PortfolioName = msg.Account,
				Price = (decimal)msg.ExecPrice,
				Volume = msg.Quantity,
				Side = msg.Side.ToSide(),
				SecurityId = new SecurityId { SecurityCode = msg.Symbol, BoardCode = msg.Destination },
				OrderType = msg.PriceType.ToPriceTypes(),
				OrderId = msg.OrderRecordID,
				TradeId = msg.TradeRecordID,
				ServerTime = msg.TradeTime.StrToDateTime(),
				LocalTime = msg.UpdateTime.StrToDateTime()
			});
		}

		private void SessionOnStiOrderUpdate(STIOrderUpdateMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				ExecutionType = ExecutionTypes.Order,
				PortfolioName = msg.Account,
				Volume = msg.Quantity,
				Side = msg.Side.ToSide(),
				Balance = msg.LvsQuantity,
				SecurityId = new SecurityId { SecurityCode = msg.Symbol, BoardCode = msg.Destination },
				OrderState = msg.OrderStatus.ToOrderStates(),
				OrderId = msg.OrderRecordID,
				VisibleVolume = msg.Display,
				ServerTime =  msg.OrderTime.StrToDateTime(),
				LocalTime =  msg.UpdateTime.StrToDateTime()
			});
		}

		private void SessionOnStiOrderReject(STIOrderRejectMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(),
				ExecutionType = ExecutionTypes.Order,
			});
		}

		private void SessionOnStiOrderConfirm(STIOrderConfirmMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				OrderState = OrderStates.Active,
				ExecutionType = ExecutionTypes.Order,
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
				SecurityId = new SecurityId { SecurityCode = msg.bstrSym, BoardCode = "All" },
			};

			message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.RealizedPnL, (decimal)msg.fReal));
			message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.BeginValue, (decimal)msg.nOpeningPosition));
			message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.CurrentValue, (decimal)(msg.nOpeningPosition + (msg.nSharesBot - msg.nSharesSld))));
			message.Changes.TryAdd(new KeyValuePair<PositionChangeTypes, object>(PositionChangeTypes.Commission, (decimal)msg.fPositionCost));

			SendOutMessage(message);
		}
	}
}