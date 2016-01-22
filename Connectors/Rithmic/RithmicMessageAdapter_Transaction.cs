#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Rithmic
File: RithmicMessageAdapter_Transaction.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic
{
	using System;
	using System.Collections.Generic;

	using com.omnesys.rapi;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class RithmicMessageAdapter
	{
		private readonly CachedSynchronizedDictionary<string, AccountInfo> _accounts = new CachedSynchronizedDictionary<string, AccountInfo>(StringComparer.InvariantCultureIgnoreCase);

		private void ProcessRegisterMessage(OrderRegisterMessage regMsg)
		{
			OrderParams orderParams;

			switch (regMsg.OrderType)
			{
				case OrderTypes.Limit:
				{
					orderParams = FillParams(regMsg, new OrderParams
					{
						Price = (double)regMsg.Price,
						OrderType = Constants.ORDER_TYPE_LIMIT,
					});
					break;
				}

				case OrderTypes.Market:
				{
					orderParams = FillParams(regMsg, new OrderParams
					{
						OrderType = Constants.ORDER_TYPE_MARKET,
					});
					break;
				}

				case OrderTypes.Conditional:
				{
					var condition = (RithmicOrderCondition)regMsg.Condition;

					var triggerPrice = condition.TriggerPrice;

					if (triggerPrice == null)
						throw new InvalidOperationException(LocalizedStrings.Str3494Params.Put(regMsg.TransactionId));

					if (regMsg.Price == 0)
					{
						orderParams = FillParams(regMsg, new OrderParams
						{
							TriggerPrice = (double)triggerPrice,
							OrderType = Constants.ORDER_TYPE_STOP_MARKET,
						});
					}
					else
					{
						orderParams = FillParams(regMsg, new OrderParams
						{
							TriggerPrice = (double)triggerPrice,
							Price = (double)regMsg.Price,
							OrderType = Constants.ORDER_TYPE_STOP_LIMIT,
						});
					}

					break;
				}
				case OrderTypes.Repo:
				case OrderTypes.ExtRepo:
				case OrderTypes.Rps:
				case OrderTypes.Execute:
					throw new NotSupportedException(LocalizedStrings.Str3495Params.Put(regMsg.TransactionId, regMsg.OrderType));
				default:
					throw new ArgumentOutOfRangeException();
			}

			_client.Session.sendOrderList(new List<OrderParams> { orderParams }.AsReadOnly());
		}

		private OrderParams FillParams(OrderRegisterMessage regMsg, OrderParams orderParams)
		{
			if (regMsg == null)
				throw new ArgumentNullException(nameof(regMsg));

			if (orderParams == null)
				throw new ArgumentNullException(nameof(orderParams));

			orderParams.Symbol = regMsg.SecurityId.SecurityCode;
			orderParams.Exchange = regMsg.SecurityId.BoardCode;
			orderParams.Account = _accounts[regMsg.PortfolioName];
			orderParams.BuySellType = regMsg.Side.ToRithmic();
			orderParams.UserMsg = regMsg.Comment;
			orderParams.UserTag = regMsg.UserOrderId;
			orderParams.Qty = (int)regMsg.Volume;
			orderParams.Tag = regMsg.TransactionId.To<string>();
			orderParams.Duration = regMsg.TimeInForce.ToRithmic(regMsg.ExpiryDate);
			orderParams.EntryType = Constants.ORDER_ENTRY_TYPE_AUTO;

			return orderParams;
		}

		private void ProcessReplaceMessage(OrderReplaceMessage replaceMsg)
		{
			ModifyOrderParams orderParams;

			switch (replaceMsg.OrderType)
			{
				case OrderTypes.Limit:
				{
					orderParams = FillModifyOrderParams(new ModifyOrderParams
					{
						Price = (double)replaceMsg.Price,
						OrderType = Constants.ORDER_TYPE_LIMIT,
					}, replaceMsg);

					break;
				}

				case OrderTypes.Conditional:
				{
					var condition = (RithmicOrderCondition)replaceMsg.Condition;

					var triggerPrice = condition.TriggerPrice;

					if (triggerPrice == null)
						throw new InvalidOperationException(LocalizedStrings.Str3494Params.Put(replaceMsg.TransactionId));

					if (replaceMsg.Price == 0)
					{
						orderParams = FillModifyOrderParams(new ModifyOrderParams
						{
							TriggerPrice = (double)triggerPrice,
							OrderType = Constants.ORDER_TYPE_STOP_MARKET,
						}, replaceMsg);
					}
					else
					{
						orderParams = FillModifyOrderParams(new ModifyOrderParams
						{
							Price = (double)replaceMsg.Price,
							TriggerPrice = (double)triggerPrice,
							OrderType = Constants.ORDER_TYPE_STOP_LIMIT,
						}, replaceMsg);
					}

					break;
				}

				case OrderTypes.Market:
				case OrderTypes.Repo:
				case OrderTypes.ExtRepo:
				case OrderTypes.Rps:
				case OrderTypes.Execute:
					throw new NotSupportedException(LocalizedStrings.Str3495Params.Put(replaceMsg.TransactionId, replaceMsg.OrderType));
				default:
					throw new ArgumentOutOfRangeException();
			}

			_client.Session.modifyOrderList(new List<ModifyOrderParams> { orderParams }.AsReadOnly());
		}

		private ModifyOrderParams FillModifyOrderParams(ModifyOrderParams orderParams, OrderReplaceMessage replaceMsg)
		{
			orderParams.Symbol = replaceMsg.SecurityId.SecurityCode;
			orderParams.Exchange = replaceMsg.SecurityId.BoardCode;
			orderParams.Account = _accounts[replaceMsg.PortfolioName];
			orderParams.UserMsg = replaceMsg.Comment;
			//orderParams.UserTag = replaceMsg.UserOrderId;
			orderParams.Qty = (int)replaceMsg.Volume;
			orderParams.EntryType = Constants.ORDER_ENTRY_TYPE_AUTO;
			orderParams.OrderNum = replaceMsg.OldOrderStringId;

			return orderParams;
		}

		private void ProcessCancelMessage(OrderCancelMessage cancelMsg)
		{
			_client.Session.cancelOrder(
						_accounts[cancelMsg.PortfolioName],
						cancelMsg.OrderStringId,
						Constants.ORDER_ENTRY_TYPE_AUTO,
						string.Empty, cancelMsg.UserOrderId, cancelMsg.TransactionId);
		}

		private void ProcessGroupCancelMessage(OrderGroupCancelMessage groupMsg)
		{
			_client.Session.cancelAllOrders(_accounts[groupMsg.PortfolioName], Constants.ORDER_ENTRY_TYPE_AUTO, string.Empty);
		}

		private void ProcessPortfolioMessage(PortfolioMessage pfMsg)
		{
			var account = _accounts[pfMsg.PortfolioName];

			if (pfMsg.IsSubscribe)
			{
				_client.Session.replayPnl(account, pfMsg.TransactionId);
				_client.Session.replayOpenOrders(account, pfMsg.TransactionId);
				_client.Session.replayExecutions(account, 0, 0, pfMsg.TransactionId);

				_client.Session.subscribeOrder(account);
				_client.Session.subscribePnl(account);
			}
			else
			{
				_client.Session.unsubscribeOrder(account);
				_client.Session.unsubscribePnl(account);
			}
		}

		private void ProcessOrderStatusMessage()
		{
			_accounts.CachedValues.ForEach(account =>
			{
				_client.Session.replayOpenOrders(account, null);
				_client.Session.replayExecutions(account, 0, 0, null);
			});
		}

		private ExecutionMessage ProcessOrderReport(OrderReport report, ExecutionMessage message)
		{
			ProcessAccount(report.Account);

			long transactionId;

			if (!long.TryParse(report.Tag, out transactionId))
				return null;

			message.PortfolioName = report.Account.AccountId;
			message.SecurityId = new SecurityId
			{
				SecurityCode = report.Symbol,
				BoardCode = report.Exchange,
			};
			message.OrderStringId = report.OrderNum;
			message.OrderBoardId = report.ExchOrdId;
			message.OriginalTransactionId = transactionId;
			message.Comment = report.UserMsg;
			message.OrderPrice = report.PriceToFill.ToDecimal() ?? 0;
			message.OrderVolume = report.TotalFilled + report.TotalUnfilled;
			message.OrderType = RithmicUtils.ToOrderType(report.OrderType);
			message.Side = RithmicUtils.ToSide(report.BuySellType);
			message.ServerTime = RithmicUtils.ToTime(report.GatewaySsboe, report.GatewayUsecs);
			message.LocalTime = RithmicUtils.ToTime(report.Ssboe, report.Usecs);
			message.TimeInForce = RithmicUtils.ToTif(report.OrderDuration);

			if (report.OrderDuration == Constants.ORDER_DURATION_DAY)
				message.ExpiryDate = DateTime.Today.ApplyTimeZone(TimeZoneInfo.Utc);

			if (report.ReportType == Constants.REPORT_TYPE_BUST)
			{
			}
			else if (report.ReportType == Constants.REPORT_TYPE_CANCEL)
			{
				message.OrderState = OrderStates.Done;
			}
			else if (report.ReportType == Constants.REPORT_TYPE_COMMISSION)
			{
				message.OrderState = report.TotalUnfilled > 0 ? OrderStates.Active : OrderStates.Done;
			}
			else if (report.ReportType == Constants.REPORT_TYPE_FAILURE)
			{
				message.OrderState = OrderStates.Failed;
				message.Error = new InvalidOperationException(((OrderFailureReport)report).Status);
			}
			else if (report.ReportType == Constants.REPORT_TYPE_FILL)
			{
				message.OrderState = report.TotalUnfilled > 0 ? OrderStates.Active : OrderStates.Done;
			}
			else if (report.ReportType == Constants.REPORT_TYPE_MODIFY)
			{
				message.OrderState = report.TotalUnfilled > 0 ? OrderStates.Active : OrderStates.Done;
			}
			else if (report.ReportType == Constants.REPORT_TYPE_NOT_CNCLLD)
			{
				message.OrderState = OrderStates.Failed;
				message.Error = new InvalidOperationException(LocalizedStrings.Str3496);
			}
			else if (report.ReportType == Constants.REPORT_TYPE_NOT_MODIFIED)
			{
				message.OrderState = OrderStates.Failed;
				message.Error = new InvalidOperationException(LocalizedStrings.Str3497);
			}
			else if (report.ReportType == Constants.REPORT_TYPE_REJECT)
			{
				message.OrderState = OrderStates.Failed;
				message.Error = new InvalidOperationException(LocalizedStrings.Str3498);
			}
			else if (report.ReportType == Constants.REPORT_TYPE_SOD)
			{
			}
			else if (report.ReportType == Constants.REPORT_TYPE_SOD_MODIFY)
			{
			}
			else if (report.ReportType == Constants.REPORT_TYPE_STATUS)
			{
				message.OrderState = report.TotalUnfilled > 0 ? OrderStates.Active : OrderStates.Done;
			}
			else if (report.ReportType == Constants.REPORT_TYPE_TRADE_CORRECT)
			{
				message.OrderState = report.TotalUnfilled > 0 ? OrderStates.Active : OrderStates.Done;
			}
			else if (report.ReportType == Constants.REPORT_TYPE_TRIGGER)
			{
			}
			else if (report.ReportType == Constants.REPORT_TYPE_TRIGGER_PULLED)
			{
			}

			return message;
		}

		private void ProcessOrderReport(OrderReport report)
		{
			var message = ProcessOrderReport(report, new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction
			});

			if (message == null)
				return;

			SendOutMessage(message);
		}

		private void SessionHolderOnOrderStatus(OrderStatusReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderReport(OrderReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderReject(OrderRejectReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderModifyFailure(OrderNotModifiedReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderModify(OrderModifyReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderLineUpdate(LineInfo info)
		{
			if (!ProcessErrorCode(info.RpCode))
				return;
		}

		private void SessionHolderOnOrderFill(OrderFillReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderFailure(OrderFailureReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderCancelFailure(OrderNotCancelledReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderCancel(OrderCancelReport report)
		{
			ProcessOrderReport(report);
		}

		private void SessionHolderOnOrderBust(OrderBustReport report)
		{
			
		}

		private void SessionHolderOnOrderInfo(OrderReplayInfo info)
		{
			if (!ProcessErrorCode(info.RpCode))
				return;

			//var message = ProcessOrderReport(info);

			//if (message == null)
			//	return;

			//message.OrderState = OrderStates.Done;

			//SendOutMessage(message);
		}

		private void SessionHolderOnAccountSodUpdate(SodReport report)
		{
			ProcessAccount(report.Account);

			SendOutMessage(new PositionChangeMessage
			{
				PortfolioName = report.Account.AccountId,
				SecurityId = new SecurityId
				{
					SecurityCode = report.Symbol,
					BoardCode = report.Exchange,
				},
				ServerTime = RithmicUtils.ToTime(report.Ssboe)
			}
			.Add(PositionChangeTypes.CurrentValue, (decimal)report.CarriedSize)
			.TryAdd(PositionChangeTypes.CurrentPrice, report.PrevClosePrice.ToDecimal()));
		}

		private void SessionHolderOnAccounts(AccountListInfo info)
		{
			ProcessErrorCode(info.RpCode);
			//if (!ProcessErrorCode(info.RpCode))
			//	return;

			foreach (var account in info.Accounts)
			{
				ProcessAccount(account);
			}
		}

		private void ProcessAccount(AccountInfo account)
		{
			bool isNew;
			_accounts.SafeAdd(account.AccountId, key => account, out isNew);

			if (!isNew)
				return;

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = account.AccountId,
				State = account.RmsInfo == null ? null : account.RmsInfo.Status.ToPortfolioState(),
			});
		}

		private void SessionHolderOnAccountRms(ProductRmsListInfo info)
		{
			if (!ProcessErrorCode(info.RpCode))
				return;
		}

		private void SessionHolderOnAccountPnLUpdate(PnlInfo info)
		{
			ProcessAccount(info.Account);
			ProcessPnL(info);
		}

		private void ProcessPnL(PnlInfo info)
		{
			//ProcessAccount(info.Account);

			BaseChangeMessage<PositionChangeTypes> message;

			if (info.Symbol.IsEmpty())
			{
				message = this.CreatePortfolioChangeMessage(info.Account.AccountId);
			}
			else
			{
				message = this.CreatePositionChangeMessage(
					info.Account.AccountId,
					new SecurityId
					{
						SecurityCode = info.Symbol,
						BoardCode = info.Exchange,
					});	
			}

			switch (info.Position.State)
			{
				case Ignorable<int>.ValueState.Ignore:
					break;
				case Ignorable<int>.ValueState.Clear:
					message.Add(PositionChangeTypes.CurrentValue, 0m);
					break;
				case Ignorable<int>.ValueState.Use:
					message.Add(PositionChangeTypes.CurrentValue, (decimal)info.Position.Value);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendOutMessage(message
				.TryAdd(PositionChangeTypes.CurrentPrice, info.AccountBalance)
				.TryAdd(PositionChangeTypes.VariationMargin, info.MarginBalance)
				.TryAdd(PositionChangeTypes.UnrealizedPnL, info.OpenPnl)
				.TryAdd(PositionChangeTypes.RealizedPnL, info.ClosedPnl)
				.TryAdd(PositionChangeTypes.AveragePrice, info.AvgOpenFillPrice)
			);
		}

		private void SessionHolderOnAccountPnL(PnlReplayInfo info)
		{
			ProcessErrorCode(info.RpCode);
			//if (!ProcessErrorCode(info.RpCode))
			//	return;

			ProcessAccount(info.Account);
			info.PnlInfoList.ForEach(ProcessPnL);
		}

		private void SessionHolderOnExecution(ExecutionReplayInfo info)
		{
			ProcessErrorCode(info.RpCode);
			//if (!ProcessErrorCode(info.RpCode))
			//	return;

			ProcessAccount(info.Account);

			foreach (var report in info.Executions)
			{
				ProcessAccount(report.Account);

				var message = ProcessOrderReport(report, new ExecutionMessage
				{
					ExecutionType = ExecutionTypes.Transaction,
					TradeVolume = report.FillSize,
					TradePrice = report.FillPrice.ToDecimal()
				});

				if (message == null)
					continue;

				SendOutMessage(message);
			}
		}

		private void SessionHolderOnOrderReplay(SingleOrderReplayInfo info)
		{
			ProcessErrorCode(info.RpCode);
		}
	}
}
