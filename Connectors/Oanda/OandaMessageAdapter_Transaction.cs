#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Oanda
File: OandaMessageAdapter_Transaction.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Oanda.Native.DataTypes;

	/// <summary>
	/// The messages adapter for OANDA (REST protocol).
	/// </summary>
	partial class OandaMessageAdapter
	{
		private const string _orderImit = "marketIfTouched";
		private readonly CachedSynchronizedDictionary<string, int> _accountIds = new CachedSynchronizedDictionary<string, int>();

		private static long GetExpiryTime(OrderMessage message)
		{
			return (message.ExpiryDate ?? DateTime.UtcNow.EndOfDay().ApplyTimeZone(TimeZoneInfo.Utc)).ToOanda();
		}

		private void ProcessOrderRegisterMessage(OrderRegisterMessage message)
		{
			var condition = (OandaOrderCondition)message.Condition;

			string type;

			if (condition == null)
				type = message.Price == 0 ? "market" : "limit";
			else
				type = condition.IsMarket ? _orderImit : "stop";

			var response = _restClient.CreateOrder(GetAccountId(message.PortfolioName),
				message.SecurityId.ToOanda(), (int)message.Volume, message.Side.To<string>().ToLowerInvariant(),
				type,
				GetExpiryTime(message),
				message.Price,
				condition?.LowerBound,
				condition?.UpperBound,
				condition?.StopLossOffset,
				condition?.TakeProfitOffset,
				condition?.TrailingStopLossOffset);

			var execMsg = new ExecutionMessage
			{
				OriginalTransactionId = message.TransactionId,
				ServerTime = response.Time.FromOanda(),
				ExecutionType = ExecutionTypes.Transaction,
				PortfolioName = message.PortfolioName,
				HasOrderInfo = true,
			};

			var tradeData = response.TradeOpened;

			if (tradeData != null)
			{
				execMsg.OrderState = OrderStates.Done;
				execMsg.OrderId = tradeData.Id;
			}
			else
			{
				execMsg.OrderState = OrderStates.Active;
				execMsg.OrderId = response.OrderOpened.Id;
			}

			SendOutMessage(execMsg);

			if (tradeData != null)
			{
				SendOutMessage(new ExecutionMessage
				{
					OriginalTransactionId = message.TransactionId,
					ExecutionType = ExecutionTypes.Transaction,
					TradePrice = (decimal)tradeData.Price,
					TradeVolume = tradeData.Units,
					ServerTime = tradeData.Time.FromOanda(),
					TradeId = tradeData.Id,
					HasTradeInfo = true,
				});
			}
		}

		private void ProcessCancelMessage(OrderCancelMessage message)
		{
			if (message.OrderId == null)
				throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(message.OrderTransactionId));

			var response = _restClient.CloseOrder(GetAccountId(message.PortfolioName), message.OrderId.Value);
			
			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				OriginalTransactionId = message.TransactionId,
				OrderId = message.OrderId,
				OrderState = OrderStates.Done,
				Balance = response.Units,
				ServerTime = response.Time.FromOanda(),
				PortfolioName = message.PortfolioName,
				HasOrderInfo = true,
			});
		}

		private void ProcessOrderReplaceMessage(OrderReplaceMessage message)
		{
			var condition = (OandaOrderCondition)message.Condition;

			if (message.OldOrderId == null)
				throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(message.OldTransactionId));

			var response = _restClient.ModifyOrder(GetAccountId(message.PortfolioName),
				message.OldOrderId.Value,
				(int)message.Volume,
				GetExpiryTime(message),
				message.Price,
				condition?.LowerBound,
				condition?.UpperBound,
				condition?.StopLossOffset,
				condition?.TakeProfitOffset,
				condition?.TrailingStopLossOffset);

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				OriginalTransactionId = message.TransactionId,
				OrderId = message.OldOrderId,
				OrderState = OrderStates.Done,
				ServerTime = response.Time.FromOanda(),
				PortfolioName = message.PortfolioName,
				HasOrderInfo = true,
			});

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				OriginalTransactionId = message.TransactionId,
				OrderId = response.Id,
				OrderState = OrderStates.Active,
				ServerTime = response.Time.FromOanda(),
				PortfolioName = message.PortfolioName,
				HasOrderInfo = true,
			});
		}

		private void ProcessOrderStatusMessage()
		{
			foreach (var accountId in _accountIds.CachedValues)
			{
				int? maxOrderId = null;

				while (true)
				{
					var orders = _restClient.GetOrders(accountId, maxOrderId);

					var count = 0;

					foreach (var order in orders)
					{
						count++;

						maxOrderId = order.Id - 1;

						OandaOrderCondition condition = null;
						OrderTypes orderType;

						if (order.Type != "market" && order.Type != "limit")
						{
							orderType = OrderTypes.Conditional;

							condition = new OandaOrderCondition
							{
								IsMarket = order.Type == _orderImit,
								TakeProfitOffset = (decimal?)order.TakeProfit,
								StopLossOffset = (decimal?)order.StopLoss,
								UpperBound = (decimal?)order.UpperBound,
								LowerBound = (decimal?)order.LowerBound,
								TrailingStopLossOffset = order.TrailingStop,
							};
						}
						else
							orderType = order.Type == "market" ? OrderTypes.Market : OrderTypes.Limit;

						SendOutMessage(new ExecutionMessage
						{
							OrderType = orderType,
							ExecutionType = ExecutionTypes.Transaction,
							OrderId = order.Id,
							ServerTime = order.Time.FromOanda(),
							OrderPrice = (decimal)order.Price,
							OrderVolume = order.Units,
							Side = order.Side.To<Sides>(),
							SecurityId = order.Instrument.ToSecurityId(),
							ExpiryDate = order.Expiry?.FromOanda(),
							Condition = condition,
							PortfolioName = GetPortfolioName(accountId),
							HasOrderInfo = true,
						});
					}

					if (count < 50)
						break;
				}

				int? maxTradeId = null;

				while (true)
				{
					var trades = _restClient.GetTrades(accountId, maxTradeId);

					var count = 0;

					foreach (var trade in trades)
					{
						count++;

						maxTradeId = trade.Id - 1;

						var takeProfit = Math.Abs(trade.TakeProfit) < 0.0000001 ? (decimal?)null : (decimal)trade.TakeProfit;
						var stopLoss = Math.Abs(trade.StopLoss) < 0.0000001 ? (decimal?)null : (decimal)trade.StopLoss;
						var tralingStop = trade.TrailingStop == 0 ? (int?)null : trade.TrailingStop;

						var isConditional = takeProfit != null || stopLoss != null || tralingStop != null;

						var transId = TransactionIdGenerator.GetNextId();

						SendOutMessage(new ExecutionMessage
						{
							ExecutionType = ExecutionTypes.Transaction,
							OrderType = isConditional ? OrderTypes.Conditional : OrderTypes.Limit,
							TradeId = trade.Id,
							OriginalTransactionId = transId,
							ServerTime = trade.Time.FromOanda(),
							OrderPrice = (decimal)trade.Price,
							TradeVolume = trade.Units,
							Balance = 0,
							Side = trade.Side.To<Sides>(),
							SecurityId = trade.Instrument.ToSecurityId(),
							OrderState = isConditional ? OrderStates.Active : OrderStates.Done,
							PortfolioName = GetPortfolioName(accountId),
							Condition = isConditional ? new OandaOrderCondition
							{
								TakeProfitOffset = takeProfit,
								StopLossOffset = stopLoss,
								TrailingStopLossOffset = tralingStop,
							} : null,
							HasOrderInfo = true,
							HasTradeInfo = true,
						});

						if (!isConditional)
						{
							SendOutMessage(new ExecutionMessage
							{
								ExecutionType = ExecutionTypes.Transaction,
								OrderId = trade.Id,
								OriginalTransactionId = transId,
								TradeId = trade.Id,
								TradePrice = (decimal)trade.Price,
								TradeVolume = trade.Units,
								ServerTime = trade.Time.FromOanda(),
								SecurityId = trade.Instrument.ToSecurityId(),
								PortfolioName = GetPortfolioName(accountId),
							});
						}
					}

					if (count < 50)
						break;
				}
			}
		}

		private void ProcessPortfolioLookupMessage(PortfolioLookupMessage message)
		{
			foreach (var account in _restClient.GetAccounts())
			{
				_accountIds[account.Name] = account.Id;

				SendOutMessage(new PortfolioMessage
				{
					PortfolioName = account.Name,
					Currency = account.Currency.To<CurrencyTypes>(),
				});

				var details = _restClient.GetAccountDetails(account.Id);

				SendOutMessage(new PortfolioChangeMessage { PortfolioName = account.Name }
					.TryAdd(PositionChangeTypes.RealizedPnL, (decimal)details.RealizedPnL)
					.TryAdd(PositionChangeTypes.UnrealizedPnL, (decimal)details.UnrealizedPnL)
					.TryAdd(PositionChangeTypes.CurrentPrice, (decimal)details.Balance)
					.TryAdd(PositionChangeTypes.BlockedValue, (decimal)details.MarginUsed)
					.TryAdd(PositionChangeTypes.CurrentValue, (decimal)details.MarginAvailable));

				foreach (var position in _restClient.GetPositions(account.Id))
				{
					SendOutMessage(new PositionChangeMessage
					{
						PortfolioName = account.Name,
						SecurityId = position.Instrument.ToSecurityId(),
					}
					.TryAdd(PositionChangeTypes.CurrentValue, (decimal)(position.Side.To<Sides>() == Sides.Buy ? position.Units : -position.Units))
					.TryAdd(PositionChangeTypes.AveragePrice, (decimal)position.AveragePrice));		
				}
			}

			SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = message.TransactionId });
		}

		private void ProcessPortfolioMessage(PortfolioMessage message)
		{
			var accountId = GetAccountId(message.PortfolioName);

			if (message.IsSubscribe)
				_streamigClient.SubscribeEventsStreaming(accountId);
			else
				_streamigClient.UnSubscribeEventsStreaming(accountId);
		}

		private void SessionOnNewTransaction(Transaction transaction)
		{
			// http://developer.oanda.com/rest-live/transaction-history/#transactionTypes
			switch (transaction.Type.ToUpperInvariant())
			{
				case "MARKET_ORDER_CREATE":
					break;
				case "STOP_ORDER_CREATE":
					break;
				case "LIMIT_ORDER_CREATE":
					break;
				case "MARKET_IF_TOUCHED_ORDER_CREATE":
					break;
				case "ORDER_UPDATE":
					break;
				case "ORDER_CANCEL":
				{
					switch (transaction.Reason.ToUpperInvariant())
					{
						case "CLIENT_REQUEST":
							break;
						case "TIME_IN_FORCE_EXPIRED":
						{
							SendOutMessage(new ExecutionMessage
							{
								ExecutionType = ExecutionTypes.Transaction,
								OrderId = transaction.OrderId,
								ServerTime = transaction.Time.FromOanda(),
								SecurityId = transaction.Instrument.ToSecurityId(),
								PortfolioName = GetPortfolioName(transaction.AccountId),
								OrderState = OrderStates.Done,
								HasOrderInfo = true,
							});

							break;
						}
						case "ORDER_FILLED":
						{
							SendOutMessage(new ExecutionMessage
							{
								ExecutionType = ExecutionTypes.Transaction,
								OrderId = transaction.OrderId,
								ServerTime = transaction.Time.FromOanda(),
								SecurityId = transaction.Instrument.ToSecurityId(),
								PortfolioName = GetPortfolioName(transaction.AccountId),
								OrderState = OrderStates.Done,
								Balance = 0,
								HasOrderInfo = true,
							});

							break;
						}
						case "INSUFFICIENT_MARGIN":
							break;
						case "BOUNDS_VIOLATION":
							break;
						case "UNITS_VIOLATION":
							break;
						case "STOP_LOSS_VIOLATION":
							break;
						case "TAKE_PROFIT_VIOLATION":
							break;
						case "TRAILING_STOP_VIOLATION":
							break;
						case "MARKET_HALTED":
							break;
						case "ACCOUNT_NON_TRADABLE":
							break;
						case "NO_NEW_POSITION_ALLOWED":
							break;
						case "INSUFFICIENT_LIQUIDITY":
							break;
					}

					break;
				}
				case "ORDER_FILLED":
				{
					var trade = transaction.TradeOpened ?? transaction.TradeReduced;
					
					SendOutMessage(new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Transaction,
						OrderId = transaction.OrderId,
						TradeId = transaction.TradeId,
						ServerTime = trade.Time.FromOanda(),
						SecurityId = trade.Instrument.ToSecurityId(),
						PortfolioName = GetPortfolioName(trade.AccountId),
						TradePrice = (decimal)trade.Price,
						TradeVolume = trade.Units,
						HasTradeInfo = true,
					});

					break;
				}
				case "TRADE_UPDATE":
					break;
				case "TRADE_CLOSE":
					break;
				case "MIGRATE_TRADE_CLOSE":
					break;
				case "MIGRATE_TRADE_OPEN":
					break;
				case "TAKE_PROFIT_FILLED":
					break;
				case "STOP_LOSS_FILLED":
					break;
				case "TRAILING_STOP_FILLED":
					break;
				case "MARGIN_CALL_ENTER":
					break;
				case "MARGIN_CALL_EXIT":
					break;
				case "MARGIN_CLOSEOUT":
					break;
				case "SET_MARGIN_RATE":
					break;
				case "TRANSFER_FUNDS":
					break;
				case "DAILY_INTEREST":
					break;
				case "FEE":
					break;
			}
		}

		private int GetAccountId(string portfolioName = null)
		{
			var accountId = portfolioName == null ? _accountIds.CachedValues.FirstOr() : _accountIds.TryGetValue2(portfolioName);

			if (accountId != null)
				return accountId.Value;

			if (portfolioName == null)
				throw new InvalidOperationException(LocalizedStrings.Str3453);
			else
				throw new InvalidOperationException(LocalizedStrings.Str3454Params.Put(portfolioName));
		}

		private string GetPortfolioName(int accountId)
		{
			var pair = _accountIds.CachedPairs.FirstOrDefault(p => p.Value == accountId);

			if (pair.Key == null)
				throw new InvalidOperationException(LocalizedStrings.Str3455Params.Put(accountId));

			return pair.Key;
		}
	}
}