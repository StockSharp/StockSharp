#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.SmartCom.SmartCom
File: SmartComMessageAdapter_Transaction.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.SmartCom
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.SmartCom.Native;
	using StockSharp.Localization;

	partial class SmartComMessageAdapter
	{
		/// <summary>
		/// Ассоциация площадок и их кодами, для заполнения <see cref="PortfolioMessage.BoardCode"/>.
		/// </summary>
		public IDictionary<string, string> PortfolioBoardCodes { get; }

		private void ProcessRegisterMessage(OrderRegisterMessage regMsg)
		{
			if (regMsg.TimeInForce != TimeInForce.PutInQueue && regMsg.TimeInForce != null)
				throw new ArgumentException(LocalizedStrings.Str1867Params.Put(regMsg.TimeInForce));

			var condition = (SmartComOrderCondition)regMsg.Condition;

			_wrapper.RegisterOrder(
				regMsg.PortfolioName, (string)regMsg.SecurityId.Native, regMsg.Side == Sides.Buy ? SmartOrderAction.Buy : SmartOrderAction.Sell,
				regMsg.GetSmartOrderType(), regMsg.TillDate == null || regMsg.TillDate == DateTimeOffset.MaxValue ? SmartOrderValidity.Gtc : SmartOrderValidity.Day,
				(double)regMsg.Price, (int)regMsg.Volume, condition != null ? (double)(condition.StopPrice ?? 0) : 0, (int)regMsg.TransactionId);
		}

		private void ProcessCancelMessage(OrderCancelMessage cancelMsg)
		{
			_wrapper.CancelOrder(cancelMsg.PortfolioName, (string)cancelMsg.SecurityId.Native, cancelMsg.OrderStringId);
		}

		private void ProcessReplaceMessage(OrderReplaceMessage replaceMsg)
		{
			//this.AddOrderInfoLog(newOrder, "ReRegisterOrder", () => "ReRegisterOrder(FORTS), old tid={0}, id={1}, sid={2}".Put(oldOrder.TransactionId, oldOrder.Id, oldOrder.GetSmartId()));

			_wrapper.ReRegisterOrder(replaceMsg.PortfolioName, (double)replaceMsg.Price, replaceMsg.OldOrderStringId);
		}

		private void ProcessPortfolioMessage(PortfolioMessage pfMsg)
		{
			if (pfMsg.IsSubscribe)
				_wrapper.SubscribePortfolio(pfMsg.PortfolioName);
			else
				_wrapper.UnSubscribePortfolio(pfMsg.PortfolioName);
		}

		private void ProcessPortolioLookupMessage(PortfolioLookupMessage pfMsg)
		{
			if (_lookupPortfoliosId == 0)
			{
				_lookupPortfoliosId = pfMsg.TransactionId;
				_wrapper.LookupPortfolios();
			}
			else
				SendOutError(LocalizedStrings.Str1868);
		}

		private void OnOrderReRegisterFailed(string smartOrderId)
		{
			this.AddErrorLog(() => "MoveFailed, smartOrderId={0}".Put(smartOrderId));

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				OrderStringId = smartOrderId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(LocalizedStrings.Str1869Params.Put(smartOrderId)),
				HasOrderInfo = true,
				ServerTime = CurrentTime,
			});
		}

		private void OnOrderReRegistered(string smartOrderId)
		{
			this.AddInfoLog(() => "MoveSucc, smartOrderId={0}".Put(smartOrderId));
		}

		private void OnNewPortfolio(int row, int rowCount, string portfolioName, string exchange, SmartPortfolioStatus status)
		{
			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = portfolioName,
				BoardCode = PortfolioBoardCodes.TryGetValue(exchange),
				ExtensionInfo = new Dictionary<object, object>
				{
					{ SmartComExtensionInfoHelper.PortfolioStatus, status }
				}
			});

			// надо использовать из клиентского кода явную подписку
			//SendInMessage(new PortfolioMessage
			//{
			//	PortfolioName = portfolioName,
			//	IsSubscribe = true,
			//});

			if ((row + 1) < rowCount)
				return;

			SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = _lookupPortfoliosId });
			_lookupPortfoliosId = 0;
		}

		private void OnPortfolioChanged(string portfolioName, decimal? cash, decimal? leverage, decimal? commission, decimal? saldo)
		{
			SendOutMessage(
				this
					.CreatePortfolioChangeMessage(portfolioName)
						.TryAdd(PositionChangeTypes.Leverage, leverage)
						.TryAdd(PositionChangeTypes.Commission, commission)
						.TryAdd(PositionChangeTypes.CurrentValue, cash)
						.TryAdd(PositionChangeTypes.BeginValue, cash));
		}

		private void OnPositionChanged(string portfolioName, string smartId, decimal? avPrice, decimal? amount, decimal? planned)
		{
			SendOutMessage(
				this
					.CreatePositionChangeMessage(portfolioName, new SecurityId { Native = smartId })
						.Add(PositionChangeTypes.BlockedValue, planned)
						.Add(PositionChangeTypes.AveragePrice, avPrice)
						.Add(PositionChangeTypes.CurrentValueInLots, amount));
		}

		private void OnNewMyTrade(string portfolio, string smartId, long orderId, decimal? price, decimal? volume, DateTime time, long tradeId)
		{
			this.AddInfoLog("SmartTrader.AddTrade: tradeId {0} orderId {1} price {2} volume {3} time {4} security {5}",
				tradeId, orderId, price, volume, time, smartId);

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				SecurityId = new SecurityId { Native = smartId },
				OrderId = orderId == 0 ? (long?)null : orderId,
				TradeId = tradeId,
				ServerTime = time.ApplyTimeZone(TimeHelper.Moscow),
				TradeVolume = volume,
				TradePrice = price,
				HasTradeInfo = true,
			});
		}

		private void OnNewOrder(int transactionId, string smartOrderId)
		{
			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				OriginalTransactionId = transactionId,
				OrderState = OrderStates.Active,
				OrderStringId = smartOrderId,
				HasOrderInfo = true,
			});
		}

		private void OnOrderFailed(int transactionId, string smartOrderId, string reason)
		{
			//this.AddOrderErrorLog(order, "OnOrderFailed", () => "sid={0}, reason={1}".Put(smartOrderId, reason));

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				OriginalTransactionId = transactionId,
				OrderState = OrderStates.Failed,
				OrderStringId = smartOrderId,
				Error = new InvalidOperationException(reason ?? LocalizedStrings.Str1870Params.Put(transactionId)),
				HasOrderInfo = true,
				ServerTime = CurrentTime,
			});
		}

		private void OnOrderCancelFailed(string smartOrderId)
		{
			//this.AddOrderErrorLog(order, "CancelFailed", () => "sid=" + smartOrderId);

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				//OriginalTransactionId = transactionId,
				OrderStringId = smartOrderId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(LocalizedStrings.Str1871Params.Put(smartOrderId)),
				HasOrderInfo = true,
				ServerTime = CurrentTime,
			});
		}

		private void OnOrderChanged(string portfolioName, string secSmartId, SmartOrderState state, SmartOrderAction action, SmartOrderType smartType, bool isOneDay,
			decimal? price, int volume, decimal? stop, int balance, DateTime time, string smartOrderId, long orderId, int status, int transactionId)
		{
			this.AddInfoLog(() => "SmartTrader.UpdateOrder: id {0} smartId {1} type {2} direction {3} price {4} volume {5} balance {6} time {7} security {8} state {9}"
				.Put(orderId, smartOrderId, smartType, action, price, volume, balance, time, secSmartId, state));

			var side = action.ToSide();

			if (side == null)
				throw new InvalidOperationException(LocalizedStrings.Str1872Params.Put(action, orderId, transactionId));

			// http://stocksharp.com/forum/yaf_postsm28324_Oshibka-pri-vystavlienii-ili-sniatii-zaiavki.aspx#post28324
			if (transactionId == 0)
				return;

			if (state.IsReject())
			{
				// заявка была ранее зарегистрирована через SmartTrader
				//if (_smartIdOrders.ContainsKey(smartOrderId))
				//{
				// замечены SystemCancel приходящие в процессе Move после которых приходит Active
				if (state != SmartOrderState.SystemCancel)
				{
					//var trId = GetTransactionBySmartId(smartOrderId);

					SendOutMessage(new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Transaction,
						OriginalTransactionId = transactionId,
						OrderStringId = smartOrderId,
						ServerTime = time.ApplyTimeZone(TimeHelper.Moscow),
						OrderState = OrderStates.Failed,
						Error = new InvalidOperationException(LocalizedStrings.Str1873Params.Put(transactionId)),
						HasOrderInfo = true,
					});
				}
				//}

				return;
			}

			var orderType = smartType.ToOrderType();

			var orderState = OrderStates.Active;
			var orderStatus = OrderStatus.Accepted;

			switch (state)
			{
				case SmartOrderState.ContragentReject:
					orderStatus = OrderStatus.NotAcceptedByManager;
					orderState = OrderStates.Failed;
					break;
				// Принята ТС
				case SmartOrderState.Submited:
					orderStatus = OrderStatus.SentToServer;
					break;
				// Зарегистрирована в ТС
				case SmartOrderState.Pending:
					orderStatus = OrderStatus.ReceiveByServer;

					if (orderType == OrderTypes.Conditional)
						orderState = OrderStates.Active;

					break;
				// Выведена на рынок
				case SmartOrderState.Open:
					orderStatus = OrderStatus.Accepted;

					if (orderType == OrderTypes.Conditional && orderId != 0)
						orderState = OrderStates.Done;
					else
						orderState = OrderStates.Active;
					break;
				// Снята по окончанию торгового дня
				case SmartOrderState.Expired:
					orderState = OrderStates.Done;
					break;
				// Отменёна
				case SmartOrderState.Cancel:
					orderState = OrderStates.Done;
					break;
				// Исполнена
				case SmartOrderState.Filled:
					orderState = OrderStates.Done;
					break;
				// Частично исполнена
				case SmartOrderState.Partial:
					if (orderState == OrderStates.None)
						orderState = OrderStates.Active;
					break;
				// Отклонена биржей
				case SmartOrderState.ContragentCancel:
					orderStatus = OrderStatus.CanceledByManager;
					orderState = OrderStates.Failed;
					break;
				// Отклонена биржей
				case SmartOrderState.SystemReject:
					orderStatus = OrderStatus.NotDone;
					orderState = OrderStates.Failed;
					break;
				// Отклонена биржей
				case SmartOrderState.SystemCancel:
					orderStatus = OrderStatus.GateError;
					orderState = OrderStates.Failed;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendOutMessage(new ExecutionMessage
			{
				SecurityId = new SecurityId { Native = secSmartId },
				PortfolioName = portfolioName,
				Side = (Sides)side,
				OrderPrice = price ?? 0,
				OrderVolume = volume,
				ServerTime = time.ApplyTimeZone(TimeHelper.Moscow),
				Balance = balance,
				OrderId = orderId == 0 ? (long?)null : orderId,
				OrderType = orderType,
				OrderState = orderState,
				OrderStatus = orderStatus,
				OriginalTransactionId = transactionId,
				OrderStringId = smartOrderId,
				ExpiryDate = isOneDay ? DateTimeOffset.Now.Date.ApplyTimeZone(TimeHelper.Moscow) : (DateTimeOffset?)null,
				Condition = orderType == OrderTypes.Conditional ? new SmartComOrderCondition { StopPrice = stop } : null,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
			});
		}
	}
}