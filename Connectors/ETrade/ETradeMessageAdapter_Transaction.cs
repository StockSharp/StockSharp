#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.ETrade
File: ETradeMessageAdapter_Transaction.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.ETrade.Native;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The messages adapter for ETrade.
	/// </summary>
	partial class ETradeMessageAdapter
	{
		private readonly SynchronizedPairSet<long, long> _ordersByTransactionId = new SynchronizedPairSet<long, long>(); 

		/// <summary>
		/// Callback of the order (re)registration.
		/// </summary>
		/// <param name="transId">Transaction ID.</param>
		/// <param name="data">(Re)registration result.</param>
		/// <param name="ex">(Re)registration error.</param>
		private void ClientOnOrderRegisterResult(long transId, PlaceEquityOrderResponse2 data, Exception ex)
		{
			if (ex != null)
			{
				this.AddWarningLog("RegisterOrder: {0}", ex);
				SendOutMessage(new ExecutionMessage
				{
					OriginalTransactionId = transId,
					OrderState = OrderStates.Failed,
					Error = new InvalidOperationException(LocalizedStrings.Str2258Params.Put(transId, ex)),
					OrderStatus = OrderStatus.RejectedBySystem,
					ExecutionType = ExecutionTypes.Transaction,
				});
				return;
			}

			var msg = new ExecutionMessage
			{
				SecurityId = new SecurityId { SecurityCode = data.symbol, BoardCode = AssociatedBoardCode },
				PortfolioName = data.accountId.To<string>(),
				Side = data.orderAction.ETradeActionToSide(),
				OriginalTransactionId = transId,
				OrderState = OrderStates.Active,
				OrderId = data.orderNum,
				ExecutionType = ExecutionTypes.Transaction,
				ServerTime = ETradeUtil.ETradeTimestampToUTC(data.orderTime)
			};

			SaveOrder(transId, msg.OrderId ?? 0);

			if (data.messageList != null)
			{
				foreach (var m in data.messageList)
					this.AddDebugLog("ord #{0}: ({1}) {2}", data.orderNum, m.msgCode, m.msgDesc);
			}

			SendOutMessage(msg);
		}

		/// <summary>
		/// Cancellation order result callback.
		/// </summary>
		/// <param name="cancelTransId">Cancellation transaction id.</param>
		/// <param name="orderId">Order ID.</param>
		/// <param name="data">Cancellation result.</param>
		/// <param name="ex">Error cancellation.</param>
		private void ClientOnOrderCancelResult(long cancelTransId, long orderId, CancelOrderResponse2 data, Exception ex)
		{
			if (ex != null)
			{
				this.AddWarningLog("CancelOrder: {0}", ex);

				SendOutMessage(new ExecutionMessage
				{
					OriginalTransactionId = cancelTransId,
					ExecutionType = ExecutionTypes.Transaction,
					OrderState = OrderStates.Failed,
					Error = new InvalidOperationException(LocalizedStrings.Str3373Params.Put(orderId, ex)),
				});

				return;
			}

			var msg = new ExecutionMessage
			{
				OriginalTransactionId = cancelTransId,
				OrderId = orderId,
				PortfolioName = data.accountId.To<string>(),
				ExecutionType = ExecutionTypes.Transaction,
				OrderStatus = OrderStatus.SentToCanceled,
				ServerTime = ETradeUtil.ETradeTimestampToUTC(data.cancelTime)
			};

			this.AddDebugLog("ord #{0}: {1}", orderId, data.resultMessage);

			SendOutMessage(msg);
		}

		/// <summary>Коллбэк результата запроса списка заявок.</summary>
		/// <param name="portName">Имя портфеля.</param>
		/// <param name="data">Результат запроса списка заявок.</param>
		/// <param name="ex">Ошибка запроса списка заявок.</param>
		private void ClientOnOrdersData(string portName, IEnumerable<Order> data, Exception ex)
		{
			if (ex != null)
			{
				SendOutError(ex);
				return;
			}

			foreach (var nativeOrder in data)
			{
				if (!IsOrderSupported(nativeOrder))
					continue;

				var leg = nativeOrder.legDetails[0];

				var secId = new SecurityId
				{
					SecurityCode = leg.symbolInfo.symbol,
					BoardCode = AssociatedBoardCode
				};

				var transId = _ordersByTransactionId.TryGetKey(nativeOrder.orderId);
				if (transId == 0)
				{
					transId = TransactionIdGenerator.GetNextId();
					SaveOrder(transId, nativeOrder.orderId);
				}

				var tuple = _orderStateMap[nativeOrder.orderStatus];

				var orderType = nativeOrder.priceType.ETradePriceTypeToOrderType();

				var msg = new ExecutionMessage
				{
					SecurityId = secId,
					PortfolioName = portName,
					Side = leg.orderAction.ETradeActionToSide(),
					OrderPrice = nativeOrder.limitPrice.To<decimal>(),
					OrderVolume = leg.orderedQuantity.To<decimal>(),
					Balance = (leg.orderedQuantity - leg.filledQuantity).To<decimal>(),
					OriginalTransactionId = transId,
					OrderId = nativeOrder.orderId,
					ExecutionType = ExecutionTypes.Transaction,
					OrderType = orderType,
					ServerTime = ETradeUtil.ETradeTimestampToUTC(nativeOrder.orderExecutedTime > 0 ? nativeOrder.orderExecutedTime : nativeOrder.orderPlacedTime), 
					OrderState = tuple.Item1, 
					OrderStatus = tuple.Item2,
				};

				switch (orderType)
				{
					case OrderTypes.Limit:
					{
						msg.OrderPrice = (decimal)nativeOrder.limitPrice;
						break;
					}
					case OrderTypes.Conditional:
					{
						if (nativeOrder.priceType == "STOP")
						{
							msg.Condition = new ETradeOrderCondition
							{
								StopType = ETradeStopTypes.StopMarket,
								StopPrice = (decimal)nativeOrder.stopPrice
							};
						}
						else if (nativeOrder.priceType == "STOP_LIMIT")
						{
							msg.Condition = new ETradeOrderCondition
							{
								StopType = ETradeStopTypes.StopLimit,
								StopPrice = (decimal)nativeOrder.stopPrice
							};
							msg.OrderPrice = nativeOrder.limitPrice.To<decimal>();
						}
						else
						{
							this.AddErrorLog(LocalizedStrings.Str3374Params, nativeOrder.priceType);
						}

						break;
					}
				}

				SendOutMessage(msg);
			}
		}

		/// <summary>
		/// Callback of the portfolios request.
		/// </summary>
		/// <param name="data">Result of the portfolios request.</param>
		/// <param name="ex">Error of the portfolios request.</param>
		private void ClientOnAccountsData(List<AccountInfo> data, Exception ex)
		{
			if (ex != null)
			{
				SendOutError(new ETradeException(LocalizedStrings.Str3375, ex));
				return;
			}

			foreach (var accountInfo in data)
			{
				var name = accountInfo.accountId.To<string>();

				var changesMsg = this
					.CreatePortfolioChangeMessage(name)
						.Add(PositionChangeTypes.Currency, CurrencyTypes.USD)
						.Add(PositionChangeTypes.CurrentPrice, (decimal)accountInfo.netAccountValue);

				SendOutMessage(changesMsg);
			}
		}

		/// <summary>Коллбэк результата запроса позиций.</summary>
		/// <param name="portfName">Имя портфеля.</param>
		/// <param name="data">Результат запроса списка позиций.</param>
		/// <param name="ex">Ошибка запроса списка позиций.</param>
		private void ClientOnPositionsData(string portfName, IEnumerable<PositionInfo> data, Exception ex)
		{
			if (ex != null)
			{
				SendOutError(new ETradeException(LocalizedStrings.Str3376, ex));
				return;
			}

			foreach (var positionInfo in data)
			{
				var secId = new SecurityId { SecurityCode = positionInfo.productId.symbol, BoardCode = positionInfo.productId.typeCode };

				SendOutMessage(new PositionMessage
				{
					PortfolioName = portfName,
					SecurityId = secId
				});

				var changesMsg = this.CreatePositionChangeMessage(portfName, secId);

				var begin = positionInfo.costBasis.To<decimal>();
				var marketValue = positionInfo.marketValue.To<decimal>();
				var absval = (decimal)Math.Abs(positionInfo.qty);
				var actualPos = positionInfo.longOrShort == "SHORT" ? -absval : absval;

				changesMsg.Add(PositionChangeTypes.CurrentValue, actualPos);
				changesMsg.Add(PositionChangeTypes.UnrealizedPnL, marketValue - begin);
				changesMsg.Add(PositionChangeTypes.CurrentPrice, positionInfo.currentPrice.To<decimal>());
				changesMsg.Add(PositionChangeTypes.AveragePrice, absval == 0 ? 0 : marketValue / absval);

				SendOutMessage(changesMsg);
			}
		}

		private void SaveOrder(long transId, long orderId)
		{
			var added = false;
			var oldTransId = _ordersByTransactionId.TryGetKey(orderId);

			if (oldTransId == 0)
				oldTransId = _ordersByTransactionId.SafeAdd(transId, k => orderId, out added);

			if (!added)
			{
				var oldOrderId = _ordersByTransactionId[oldTransId];
				if (transId != oldTransId || orderId != oldOrderId)
					this.AddErrorLog("pair exists: old=(t={0}, o={1}), new=(t={2}, o={3})", oldTransId, oldOrderId, transId, orderId);
			}
		}

		private static bool IsOrderSupported(Order nativeOrder)
		{
			return nativeOrder.orderType == "EQ" &&
				    _supportedPriceTypes.Contains(nativeOrder.priceType) &&
				    nativeOrder.legDetails != null &&
				    nativeOrder.legDetails.Count == 1 &&
				    _supportedActions.Contains(nativeOrder.legDetails[0].orderAction);
		}

		#region native data maps

		private static readonly Dictionary<string, Tuple<OrderStates, OrderStatus>> _orderStateMap = new Dictionary
			<string, Tuple<OrderStates, OrderStatus>>
		{
			{ "OPEN",             Tuple.Create(OrderStates.Active, OrderStatus.Accepted) },
			{ "EXECUTED",         Tuple.Create(OrderStates.Done, OrderStatus.Matched) },
			{ "CANCEL_REQUESTED", Tuple.Create(OrderStates.Active, OrderStatus.SentToCanceled) },
			{ "CANCELLED",        Tuple.Create(OrderStates.Done, OrderStatus.Cancelled) },
			{ "EXPIRED",          Tuple.Create(OrderStates.Done, OrderStatus.Cancelled) },
			{ "REJECTED",         Tuple.Create(OrderStates.Done, OrderStatus.NotValidated) }
		};

		//private static readonly Dictionary<string, OrderTypes> _orderTypesMap = new Dictionary<string, OrderTypes>
		//{
		//	{ "MARKET", OrderTypes.Market },
		//	{ "LIMIT", OrderTypes.Limit },
		//	{ "STOP", OrderTypes.Conditional },
		//	{ "STOP_LIMIT", OrderTypes.Conditional }
		//};

		private static readonly string[] _supportedPriceTypes = { "MARKET", "LIMIT", "STOP", "STOP_LIMIT" };
		private static readonly string[] _supportedActions = { "BUY", "SELL", "BUY_TO_COVER", "SELL_SHORT" };

		#endregion
	}
}
