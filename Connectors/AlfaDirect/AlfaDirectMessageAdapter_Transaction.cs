namespace StockSharp.AlfaDirect
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.AlfaDirect.Native;
	using StockSharp.Algo;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class AlfaDirectMessageAdapter
	{
		/// <summary>&lt;s# transactionId, alfaDirectTransactionId&gt;</summary>
		private readonly SynchronizedPairSet<long, int> _localIds = new SynchronizedPairSet<long, int>();
		/// <summary>&lt;alfaDirectTransactionId, orderId&gt;</summary>
		private readonly SynchronizedPairSet<int, int> _alfaIds = new SynchronizedPairSet<int, int>();

		private int _lastFakeAlfaTransactionId = -1000000;

		private void OnProcessOrderConfirmed(int alfaTransactionId, int orderId)
		{
			var added = false;
			var oldAlfaTransId = _alfaIds.TryGetKey(orderId);

			if (oldAlfaTransId == 0)
				oldAlfaTransId = _alfaIds.SafeAdd(alfaTransactionId, k => orderId, out added);

			if (!added)
				SessionHolder.AddWarningLog("element exists: _alfaIds[{0}]={1}, new orderId={2}", alfaTransactionId, oldAlfaTransId, orderId);
		}

		private void OnProcessOrders(string[] data)
		{
			var f = Wrapper.FieldsOrders;

			if (data.Length > 0)
				SessionHolder.AddLog(LogLevels.Debug, () => "OnProcessOrders:\n" + data.Join("\n"));

			foreach (var str in data)
			{
				var cols = str.ToColumns();

				// 83487901|41469-000|42550|M|B|8356|1|1|0:00:00||FORTS|0|
				// ...
				// 83487901|41469-000|42550|M|B|8356|1|0|12.12.2011 16:31:41|2198931532|FORTS|0|
				// NOTE: когда в первый раз приходит апдейт по заявке в нем нет ни времени, ни комментария.

				// 84352688|41469-000|12910|M|S|80.13|10|0|22.12.2011 17:16:58||MICEX_SHR|0|0|
				// 84352688|41469-000|12910|M|S|80.13|10|0|22.12.2011 17:16:58|2968835969|MICEX_SHR|0|80.408|
				// NOTE: на ММВБ заявка сперва приходит с пустым комментарием!

				var orderId = f.OrdNo.GetValue(cols);
				var transId = f.Comments.GetValue(cols);

				if (transId == 0)
				{
					var alfaTransactionId = _alfaIds.TryGetKey(orderId);
					transId = _localIds.TryGetKey(alfaTransactionId);

					if (transId == 0)
					{
						SessionHolder.AddWarningLog("transaction id for order #{0} not found. creating new one.", orderId);
						transId = TransactionIdGenerator.GetNextId();
						if (alfaTransactionId == 0)
						{
							alfaTransactionId = --_lastFakeAlfaTransactionId;
							_alfaIds.Add(alfaTransactionId, orderId);
						}
						_localIds.Add(transId, alfaTransactionId);
					}
				}

				var msg = new ExecutionMessage
				{
					SecurityId = new SecurityId { Native = f.PaperNo.GetValue(cols) },
					PortfolioName = GetPortfolioName(f.AccCode.GetValue(cols), SessionHolder.GetBoardCode(f.PlaceCode.GetValue(cols))),
					Side = f.BuySellStr.GetValue(cols),
					Price = f.Price.GetValue(cols),
					Volume = f.Qty.GetValue(cols),
					Balance = f.Rest.GetValue(cols),
					OriginalTransactionId = transId,
					OrderId = orderId,
					ExecutionType = ExecutionTypes.Order,
				};

				var orderTime = f.TsTime.GetValue(cols);
				if (orderTime.TimeOfDay != TimeSpan.Zero)
					msg.ServerTime = orderTime.ApplyTimeZone(TimeHelper.Moscow);

				var stopPrice = f.StopPrice.GetValue(cols);
				var orderType = f.Blank.GetValue(cols);

				switch (orderType)
				{
					case "S":
					{
						var updateToPrice = f.UpdateNewPrice.GetValue(cols);

						if (updateToPrice != 0) // Stop + TargetProfit
						{
							var updGrowPrice = f.UpdateGrowPrice.GetValue(cols);
							var updDownPrice = f.UpdateDownPrice.GetValue(cols);

							msg.Condition = new AlfaOrderCondition
							{
								StopPrice = stopPrice,
								TargetPrice = msg.Price,
								Slippage = msg.Side == Sides.Buy
											   ? updateToPrice - updGrowPrice
											   : updDownPrice - updateToPrice
							};
						}
						else // Stop
						{
							msg.Condition = new AlfaOrderCondition
							{
								StopPrice = stopPrice,
								Slippage = stopPrice == 0
											   ? 0
											   : msg.Side == Sides.Buy
													 ? msg.Price - stopPrice
													 : stopPrice - msg.Price
							};
						}

						msg.OrderType = OrderTypes.Conditional;

						break;
					}
					case "T":
					{
						var level = f.TrailingLevel.GetValue(cols);
						var slippage = f.TrailingSlippage.GetValue(cols);

						msg.Condition = new AlfaOrderCondition
						{
							Level = level,
							Slippage = slippage,
							StopPrice = stopPrice
						};

						msg.OrderType = OrderTypes.Conditional;

						break;
					}
					case "L":
						msg.OrderType = OrderTypes.Limit;
						break;
					default:
						SessionHolder.AddWarningLog("Unknown order type '{0}' (id={1})", orderType, orderId);
						break;
				}

				var status = f.OrderStatus.GetValue(cols);
				switch (status)
				{
					case "O": // активная
					case "G": // с условием
						msg.OrderState = OrderStates.Active;
						break;

					case "M": // исполнена
						msg.OrderState = msg.Balance != 0 ? OrderStates.Active : OrderStates.Done;
						break;

					case "W": // удалена
						msg.OrderState = OrderStates.Done;
						break;

					case "N": // принята сервером АД, но пока не на бирже
						msg.OrderState = OrderStates.Pending;
						break;

					default:
						SessionHolder.AddInfoLog("Order status {0} is not taken into account", status);
						break;
				}

				SendOutMessage(msg);
			}
		}

		private void OnProcessOrderFailed(int alfaTransactionId, string message)
		{
			var transactionId = _localIds.TryGetKey(alfaTransactionId);

			if (transactionId == 0)
			{
				SessionHolder.AddWarningLog("ProcessOrderFailed: unknown alfaTransactionId={0}", alfaTransactionId);
				return;
			}

			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = transactionId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(LocalizedStrings.Str2258Params.Put(transactionId, message)),
				OrderStatus = OrderStatus.RejectedBySystem,
				ExecutionType = ExecutionTypes.Order,
			});
		}

		private void OnProcessPositions(long transactionId, string[] data)
		{
			var f = Wrapper.FieldsPositions;
			var portfChangeMessages = new Dictionary<string, PortfolioChangeMessage>();

			foreach (var str in data)
			{
				var cols = str.ToColumns();

				var portfolioName = GetPortfolioName(f.AccCode.GetValue(cols), SessionHolder.GetBoardCode(f.PlaceCode.GetValue(cols)));
				var secCode = f.PaperCode.GetValue(cols);

				if (secCode == "money")
				{
					var changesMsg = portfChangeMessages.SafeAdd(portfolioName, name => SessionHolder.CreatePortfolioChangeMessage(name));

					changesMsg.Add(PositionChangeTypes.RealizedPnL, f.PnL.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.UnrealizedPnL, f.ProfitVol.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.CurrentPrice, f.RealVol.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.BeginValue, f.IncomeRest.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.CurrentValue, f.RealRest.GetValue(cols));
				}
				else
				{
					var secId = new SecurityId { Native = f.PaperNo.GetValue(cols) };

					SendOutMessage(new PositionMessage
					{
						PortfolioName = portfolioName,
						SecurityId = secId
					});

					var changesMsg = SessionHolder.CreatePositionChangeMessage(portfolioName, secId);

					changesMsg.Add(PositionChangeTypes.RealizedPnL, f.PnL.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.UnrealizedPnL, f.ProfitVol.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.CurrentPrice, f.RealVol.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.CurrentValue, f.ForwordRest.GetValue(cols));
					changesMsg.Add(PositionChangeTypes.AveragePrice, f.BalancePrice.GetValue(cols));

					var varMargin = f.VarMargin.GetValue(cols);
					changesMsg.Add(PositionChangeTypes.VariationMargin, varMargin);

					if (varMargin != 0)
					{
						var portfChangesMsg = portfChangeMessages.SafeAdd(portfolioName, name => SessionHolder.CreatePortfolioChangeMessage(name));
						var oldVm = portfChangesMsg.Changes.TryGetValue(PositionChangeTypes.VariationMargin);

						if(oldVm == null)
							portfChangesMsg.Changes[PositionChangeTypes.VariationMargin] = varMargin;
						else
							portfChangesMsg.Changes[PositionChangeTypes.VariationMargin] = (decimal)oldVm + varMargin;
					}

					SendOutMessage(changesMsg);
				}	
			}

			portfChangeMessages.Values.ForEach(SendOutMessage);

			if (transactionId > 0)
			{
				SendOutMessage(new PortfolioLookupResultMessage
				{
					OriginalTransactionId = transactionId,
				});	
			}
		}

		private void OnProcessMyTrades(string[] data)
		{
			var f = Wrapper.FieldsMyTrades;

			foreach (var str in data)
			{
				var cols = str.ToColumns();

				var tradeId = f.TrdNo.GetValue(cols);
				var orderId = f.OrdNo.GetValue(cols);

				if (orderId == 0)
				{
					SessionHolder.AddWarningLog(LocalizedStrings.Str2259Params, tradeId);
					continue;
				}

				SendOutMessage(new ExecutionMessage
				{
					SecurityId = new SecurityId { Native = f.PaperNo.GetValue(cols) },
					ExecutionType = ExecutionTypes.Trade,
					OrderId = orderId,
					TradeId = tradeId,
					TradePrice = f.Price.GetValue(cols),
					ServerTime = f.TsTime.GetValue(cols).ApplyTimeZone(TimeHelper.Moscow),
					Volume = f.Qty.GetValue(cols),
					OriginSide = f.BuySellStr.GetValue(cols),
				});
			}
		}

		private string GetPortfolioName(string accCode, string placeCode)
		{
			return "{0}@{1}".Put(accCode, placeCode);
		}
	}
}