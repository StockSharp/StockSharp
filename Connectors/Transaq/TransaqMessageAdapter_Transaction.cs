namespace StockSharp.Transaq
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Transaq.Native;
	using StockSharp.Transaq.Native.Commands;
	using StockSharp.Transaq.Native.Responses;
	using StockSharp.Localization;

	partial class TransaqMessageAdapter
	{
		private readonly SynchronizedPairSet<long, long> _orders = new SynchronizedPairSet<long, long>();
		private readonly SynchronizedDictionary<long, OrderTypes> _ordersTypes = new SynchronizedDictionary<long, OrderTypes>();

		private void ProcessRegisterMessage(OrderRegisterMessage regMsg)
		{
			DateTime? expDate;

			if (regMsg.TillDate == DateTimeOffset.MaxValue)
				expDate = null;
			else
				expDate = regMsg.TillDate.ToLocalTime(TimeHelper.Moscow);

			BaseCommandMessage command;

			switch (regMsg.OrderType)
			{
				case OrderTypes.Limit:
				case OrderTypes.Market:
				{
					command = new NewOrderMessage
					{
						ByMarket = regMsg.OrderType == OrderTypes.Market,
						Client = regMsg.PortfolioName,
						Quantity = regMsg.Volume.To<int>(),
						Unfilled = regMsg.TimeInForce.ToTransaq(),
						BuySell = regMsg.Side.ToTransaq(),
						Price = regMsg.Price,
						SecId = (int)regMsg.SecurityId.Native,
						ExpDate = expDate,
						BrokerRef = regMsg.Comment,
						Hidden = (int)(regMsg.Volume - regMsg.VisibleVolume),
					};

					break;
				}
				case OrderTypes.Conditional:
				{
					if (regMsg.Condition is TransaqAlgoOrderCondition)
					{
						var cond = (TransaqAlgoOrderCondition)regMsg.Condition;

						command = new NewCondOrderMessage
						{
							ByMarket = regMsg.OrderType == OrderTypes.Market,
							Client = regMsg.PortfolioName,
							Quantity = regMsg.Volume.To<int>(),
							BuySell = regMsg.Side.ToTransaq(),
							Price = regMsg.Price,
							SecId = (int)regMsg.SecurityId.Native,
							CondType = cond.Type,
							CondValue = cond.Value,
							ValidAfterType = cond.ValidAfterType,
							ValidAfter = cond.ValidAfter,
							ValidBeforeType = cond.ValidBeforeType,
							ValidBefore = cond.ValidBefore,
							ExpDate = expDate,
							BrokerRef = regMsg.Comment,
							Hidden = (int)(regMsg.Volume - regMsg.VisibleVolume),
						};
					}
					else if (regMsg.Condition is TransaqOrderCondition)
					{
						var cond = (TransaqOrderCondition)regMsg.Condition;

						if (!cond.CheckConditionUnitType())
							throw new InvalidOperationException(LocalizedStrings.Str3549);

						var stopOrder = new NewStopOrderMessage
						{
							SecId = (int)regMsg.SecurityId.Native,
							Client = regMsg.PortfolioName,
							BuySell = regMsg.Side.ToTransaq(),
							LinkedOrderNo = cond.LinkedOrderId.To<string>(),
							ExpDate = expDate,
							ValidFor = expDate,
						};

						switch (cond.Type)
						{
							case TransaqOrderConditionTypes.StopLoss:
								stopOrder.StopLoss = TransaqHelper.CreateStopLoss(cond);
								break;

							case TransaqOrderConditionTypes.TakeProfit:
								stopOrder.TakeProfit = TransaqHelper.CreateTakeProfit(cond);
								break;

							case TransaqOrderConditionTypes.TakeProfitStopLoss:
								stopOrder.StopLoss = TransaqHelper.CreateStopLoss(cond);
								stopOrder.TakeProfit = TransaqHelper.CreateTakeProfit(cond);
								break;
						}

						command = stopOrder;
					}
					else
						throw new InvalidOperationException(LocalizedStrings.Str3550Params.Put(regMsg.Condition, regMsg.TransactionId));

					break;
				}
				case OrderTypes.Repo:
				{
					command = new NewRepoOrderMessage
					{
						SecId = (int)regMsg.SecurityId.Native,
						Client = regMsg.PortfolioName,
						BuySell = regMsg.Side.ToTransaq(),
						CpFirmId = regMsg.RepoInfo.Partner,
						MatchRef = regMsg.RepoInfo.MatchRef,
						BrokerRef = regMsg.Comment,
						Price = regMsg.Price,
						Quantity = regMsg.Volume.To<int>(),
						SettleCode = regMsg.RepoInfo.SettleCode,
						RefundRate = regMsg.RepoInfo.RefundRate,
						Rate = regMsg.RepoInfo.Rate,
					};

					break;
				}
				case OrderTypes.ExtRepo:
				{
					command = new NewMRepoOrderMessage
					{
						SecId = (int)regMsg.SecurityId.Native,
						Client = regMsg.PortfolioName,
						BuySell = regMsg.Side.ToTransaq(),
						CpFirmId = regMsg.RepoInfo.Partner,
						MatchRef = regMsg.RepoInfo.MatchRef,
						BrokerRef = regMsg.Comment,
						Price = regMsg.Price,
						Quantity = regMsg.Volume.To<int>(),
						SettleCode = regMsg.RepoInfo.SettleCode,
						RefundRate = regMsg.RepoInfo.RefundRate,
						Value = regMsg.RepoInfo.Value,
						Term = regMsg.RepoInfo.Term,
						Rate = regMsg.RepoInfo.Rate,
						StartDiscount = regMsg.RepoInfo.StartDiscount,
						LowerDiscount = regMsg.RepoInfo.LowerDiscount,
						UpperDiscount = regMsg.RepoInfo.UpperDiscount,
						BlockSecurities = regMsg.RepoInfo.BlockSecurities,
					};

					break;
				}
				case OrderTypes.Rps:
				{
					command = new NewRpsOrderMessage
					{
						SecId = (int)regMsg.SecurityId.Native,
						Client = regMsg.PortfolioName,
						BuySell = regMsg.Side.ToTransaq(),
						CpFirmId = regMsg.RpsInfo.Partner,
						MatchRef = regMsg.RpsInfo.MatchRef,
						BrokerRef = regMsg.Comment,
						Price = regMsg.Price,
						Quantity = regMsg.Volume.To<int>(),
						SettleCode = regMsg.RpsInfo.SettleCode,
					};

					break;
				}
				case OrderTypes.Execute:
				{
					//command = new NewReportMessage
					//{
					//	BuySell = regMsg.Side.ToTransaq(),
					//};
					//break;
					throw new NotImplementedException();
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			var result = SendCommand(command);

			_orders.Add(result.TransactionId, regMsg.TransactionId);
			_ordersTypes.Add(regMsg.TransactionId, command is NewCondOrderMessage ? OrderTypes.Limit : regMsg.OrderType);
		}

		private void ProcessCancelMessage(OrderCancelMessage cancelMsg)
		{
			var id = _orders.TryGetKey(cancelMsg.OrderTransactionId);

			if (id == 0)
				throw new InvalidOperationException(LocalizedStrings.Str3551Params.Put(cancelMsg.OrderTransactionId));

			BaseCommandMessage command;

			switch (_ordersTypes[cancelMsg.OrderTransactionId])
			{
				case OrderTypes.Limit:
				case OrderTypes.Market:
					command = new CancelOrderMessage { TransactionId = id };
					break;
				case OrderTypes.Conditional:
					command = new CancelStopOrderMessage { TransactionId = id };
					break;
				case OrderTypes.Repo:
				case OrderTypes.ExtRepo:
				case OrderTypes.Rps:
					command = new CancelNegDealMessage { TransactionId = id };
					break;
				case OrderTypes.Execute:
					command = new CancelReportMessage { TransactionId = id };
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendCommand(command);
		}

		private void ProcessReplaceMessage(OrderReplaceMessage replaceMsg)
		{
			var id = _orders.TryGetKey(replaceMsg.OldTransactionId);

			if (id == 0)
				throw new InvalidOperationException(LocalizedStrings.Str3551Params.Put(replaceMsg.OldTransactionId));

			var result = SendCommand(new MoveOrderMessage
			{
				TransactionId = id,
				Price = replaceMsg.Price,
				Quantity = (int)replaceMsg.Volume,
				MoveFlag = replaceMsg.Volume == 0 ? MoveOrderFlag.ChangeQuantity : MoveOrderFlag.DontChangeQuantity,
			});

			_orders.Add(result.TransactionId, replaceMsg.TransactionId);
			_ordersTypes.Add(replaceMsg.TransactionId, OrderTypes.Limit);
		}

		private void OnClientLimitsResponse(ClientLimitsResponse response)
		{
		}

		private void OnClientResponse(ClientResponse response)
		{
			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = response.Id,
				Currency = TransaqHelper.ToCurrency(response.Currency),
			});

			if (response.MlIntraDay != null)
			{
				SendOutMessage(
					SessionHolder
						.CreatePortfolioChangeMessage(response.Id)
							.Add(PositionChangeTypes.Leverage, response.MlIntraDay.Value));	
			}
			
			//if (MicexRegisters)
			//    SendCommand(new RequestPortfolioTPlusMessage {Client = response.Id});
		}

		private void OnLeverageControlResponse(LeverageControlResponse response)
		{
			if (response.LeverageFact != null)
			{
				SendOutMessage(
					SessionHolder
						.CreatePortfolioChangeMessage(response.Client)
							.Add(PositionChangeTypes.Leverage, response.LeverageFact.Value));	
			}
		}

		private void OnMarketOrdResponse(MarketOrdResponse response)
		{
		}

		private void OnOrdersResponse(OrdersResponse response)
		{
			foreach (var order in response.Orders.Cast<TransaqBaseOrder>().Concat(response.StopOrders))
			{
				var stockSharpTransactionId = _orders.TryGetValue(order.TransactionId);

				if (stockSharpTransactionId == 0)
				{
					stockSharpTransactionId = order.TransactionId;

					// если заявка пришла от терминала, то просто номер транзакции ассоциируем как стокшарповский
					_orders.Add(order.TransactionId, order.TransactionId);

					_ordersTypes.Add(order.TransactionId, order is TransaqStopOrder ? OrderTypes.Conditional : OrderTypes.Limit);
				}

				var execMsg = new ExecutionMessage
				{
					SecurityId = new SecurityId { Native = order.SecId },
					Side = order.BuySell.FromTransaq(),
					OriginalTransactionId = stockSharpTransactionId,
					PortfolioName = order.Client,
					ExpiryDate = order.ExpDate == null ? DateTimeOffset.MaxValue : order.ExpDate.Value.ApplyTimeZone(TimeHelper.Moscow),
					ExecutionType = ExecutionTypes.Order,
				};

				var usualOrder = order as TransaqOrder;

				if (usualOrder != null)
				{
					execMsg.OrderId = usualOrder.OrderNo;
					execMsg.Balance = usualOrder.Balance;
					execMsg.ServerTime = usualOrder.WithdrawTime ?? usualOrder.Time ?? usualOrder.AcceptTime ?? DateTimeOffset.MinValue;
					execMsg.Comment = usualOrder.BrokerRef;
					execMsg.SystemComment = usualOrder.Result;
					execMsg.Price = usualOrder.Price;
					execMsg.Volume = usualOrder.Quantity;
					execMsg.OrderType = usualOrder.Price == 0 ? OrderTypes.Market : OrderTypes.Limit;
					execMsg.Commission = usualOrder.MaxCommission;

					if (usualOrder.ConditionType != TransaqAlgoOrderConditionTypes.None)
					{
						execMsg.OrderType = OrderTypes.Conditional;

						execMsg.Condition = new TransaqAlgoOrderCondition
						{
							Type = usualOrder.ConditionType,
							Value = usualOrder.ConditionValue.To<decimal>(),

							ValidAfter = usualOrder.ValidAfter,
							ValidBefore = usualOrder.ValidBefore,
						};
					}
				}
				else
				{
					var stopOrder = (TransaqStopOrder)order;

					execMsg.OrderId = stopOrder.ActiveOrderNo ?? 0;
					execMsg.OrderType = OrderTypes.Conditional;
					execMsg.ServerTime = stopOrder.AcceptTime == null
						? DateTimeOffset.MinValue
						: stopOrder.AcceptTime.Value.ApplyTimeZone(TimeHelper.Moscow);

					var stopCond = new TransaqOrderCondition
					{
						Type = stopOrder.StopLoss != null && stopOrder.TakeProfit != null ? TransaqOrderConditionTypes.TakeProfitStopLoss : (stopOrder.StopLoss != null ? TransaqOrderConditionTypes.StopLoss : TransaqOrderConditionTypes.TakeProfit),
						ValidFor = stopOrder.ValidBefore,
						LinkedOrderId = stopOrder.LinkedOrderNo,
					};

					if (stopOrder.StopLoss != null)
					{
						stopCond.StopLossActivationPrice = stopOrder.StopLoss.ActivationPrice;
						stopCond.StopLossOrderPrice = stopOrder.StopLoss.OrderPrice;
						stopCond.StopLossByMarket = stopOrder.StopLoss.OrderPrice == null;
						stopCond.StopLossVolume = stopOrder.StopLoss.Quantity;
						//stopCond.StopLossUseCredit = stopOrder.StopLoss.UseCredit.To<bool>();
						
						if (stopOrder.StopLoss.GuardTime != null)
							stopCond.StopLossGuardTime = (int)stopOrder.StopLoss.GuardTime.Value.TimeOfDay.TotalMinutes;
						
						stopCond.StopLossComment = stopOrder.StopLoss.BrokerRef;
					}

					if (stopOrder.TakeProfit != null)
					{
						stopCond.TakeProfitActivationPrice = stopOrder.TakeProfit.ActivationPrice;
						stopCond.TakeProfitByMarket = stopOrder.TakeProfit.GuardSpread == null;
						stopCond.TakeProfitVolume = stopOrder.TakeProfit.Quantity;
						//stopCond.TakeProfitUseCredit = stopOrder.TakeProfit.UseCredit.To<bool>();
						
						if (stopOrder.TakeProfit.GuardTime != null)
							stopCond.TakeProfitGuardTime = (int)stopOrder.TakeProfit.GuardTime.Value.TimeOfDay.TotalMinutes;
						
						stopCond.TakeProfitComment = stopOrder.TakeProfit.BrokerRef;
						stopCond.TakeProfitCorrection = stopOrder.TakeProfit.Correction;
						stopCond.TakeProfitGuardSpread = stopOrder.TakeProfit.GuardSpread;
					}
				}

				execMsg.OrderState = order.Status.ToStockSharpState();
				//execMsg.OrderStatus = order2.Status.ToStockSharpStatus();

				if (order.Status != TransaqOrderStatus.cancelled)
				{
					if (execMsg.OrderState == OrderStates.Failed && usualOrder != null)
						execMsg.Error = new InvalidOperationException(usualOrder.Result);
				}

				SendOutMessage(execMsg);

				//if (order.Condition != null && order.DerivedOrder == null && order2.ConditionType != TransaqAlgoOrderConditionTypes.None && order2.OrderNo != 0)
				//	AddDerivedOrder(security, order2.OrderNo, order, (stopOrder, limitOrder) => stopOrder.DerivedOrder = limitOrder);
			}
		}

		private void OnOvernightResponse(OvernightResponse response)
		{
		}

		private void OnPositionsResponse(PositionsResponse response)
		{
			foreach (var pos in response.MoneyPositions.GroupBy(p => p.Client))
			{
				SendOutMessage(
					SessionHolder.CreatePortfolioChangeMessage(pos.Key)
						.Add(PositionChangeTypes.BeginValue, pos.Sum(p => p.SaldoIn))
						.Add(PositionChangeTypes.CurrentValue, pos.Sum(p => p.Saldo))
						.Add(PositionChangeTypes.Commission, pos.Sum(p => p.Commission)));
			}

			foreach (var pos in response.SecPositions)
			{
				SendOutMessage(
					new PositionChangeMessage
					{
						PortfolioName = pos.Client,
						SecurityId = new SecurityId { Native = pos.SecId },
						DepoName = pos.Register,
						ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
					}
					.Add(PositionChangeTypes.BeginValue, pos.SaldoIn)
					.Add(PositionChangeTypes.CurrentValue, pos.Saldo)
					.Add(PositionChangeTypes.BlockedValue, pos.SaldoMin));
			}

			foreach (var fortsMoney in response.FortsMoneys)
			{
				SendOutMessage(
					SessionHolder
						.CreatePortfolioChangeMessage(fortsMoney.Client)
							.Add(PositionChangeTypes.BeginValue, fortsMoney.Free)
							.Add(PositionChangeTypes.CurrentValue, fortsMoney.Current)
							.Add(PositionChangeTypes.BlockedValue, fortsMoney.Blocked)
							.Add(PositionChangeTypes.VariationMargin, fortsMoney.VarMargin));
			}

			foreach (var pos in response.FortsPositions)
			{
				SendOutMessage(SessionHolder
					.CreatePositionChangeMessage(pos.Client, new SecurityId { Native = pos.SecId })
						.Add(PositionChangeTypes.BeginValue, (decimal)pos.StartNet)
						.Add(PositionChangeTypes.CurrentValue, (decimal)pos.TotalNet)
						.Add(PositionChangeTypes.VariationMargin, pos.VarMargin));
			}

			//foreach (var fortsCollaterals in response.FortsCollateralses)
			//{

			//}

			//foreach (var spotLimit in response.SpotLimits)
			//{

			//}
		}

		private void OnTradesResponse(TradesResponse response)
		{
			foreach (var trade in response.Trades)
			{
				SendOutMessage(new ExecutionMessage
				{
					SecurityId = new SecurityId { Native = trade.SecId },
					OrderId = trade.OrderNo,
					TradeId = trade.TradeNo,
					TradePrice = trade.Price,
					Side = trade.BuySell.FromTransaq(),
					ServerTime = trade.Time,
					Comment = trade.BrokerRef,
					Volume = trade.Quantity,
					PortfolioName = trade.Client,
					ExecutionType = ExecutionTypes.Trade,
					Commission = trade.Commission,
				});
			}
		}

		private void OnPortfolioTPlusResponse(PortfolioTPlusResponse response)
		{
			SendOutMessage(
				SessionHolder
					.CreatePortfolioChangeMessage(response.Client)
						.Add(PositionChangeTypes.RealizedPnL, response.PnLIntraday)
						.Add(PositionChangeTypes.UnrealizedPnL, response.PnLIncome)
						.Add(PositionChangeTypes.Leverage, response.Leverage)
						.Add(PositionChangeTypes.BeginValue, response.OpenEquity)
						.Add(PositionChangeTypes.CurrentValue, response.Equity));

			foreach (var security in response.Securities)
			{
				SendOutMessage(SessionHolder
					.CreatePositionChangeMessage(response.Client, new SecurityId { Native = security.SecId })
						.Add(PositionChangeTypes.CurrentPrice, security.Price)
						.Add(PositionChangeTypes.CurrentValue, (decimal)security.OpenBalance)
						.Add(PositionChangeTypes.RealizedPnL, security.PnLIntraday)
						.Add(PositionChangeTypes.UnrealizedPnL, security.PnLIncome));
			}
		}
	}
}