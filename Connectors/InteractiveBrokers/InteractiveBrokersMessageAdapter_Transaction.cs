namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.InteractiveBrokers.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class InteractiveBrokersMessageAdapter
	{
		private readonly Dictionary<string, SecurityId> _secIdByTradeIds = new Dictionary<string, SecurityId>();

		private void RegisterOrder(OrderRegisterMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (message.OrderType == OrderTypes.Execute)
			{
				var ibCon = (IBOrderCondition)message.Condition;

				if (ibCon == null)
					throw new ArgumentException(LocalizedStrings.Str2514Params.Put(message.TransactionId), "message");

				ExerciseOptions(message, ibCon.OptionExercise.IsExercise, message.Volume, message.PortfolioName, ibCon.OptionExercise.IsOverride);
				return;
			}

			var version = (Session.ServerVersion < ServerVersions.V44) ? ServerVersions.V27 : ServerVersions.V43;

			//var security = SessionHolder.Securities[regMsg.SecurityId];

			ProcessRequest(RequestMessages.RegisterOrder, 0, version,
				socket =>
				{
					socket.Send((int)message.TransactionId);

					socket
						.SendContractId(message.SecurityId)
						.SendSecurity(message)
						.SendIf(ServerVersions.V68, s => socket.Send(message.Class))
						.SendSecurityId(message.SecurityId);

					var condition = (IBOrderCondition)message.Condition ?? new IBOrderCondition();

					socket
						.SendSide(message.Side)
						.Send(message.Volume)
						.SendOrderType(message.OrderType, condition.ExtendedType)
						.Send(message.Price)
						.Send(condition.StopPrice)
						.SendOrderExpiration(message)
						.Send(condition.Oca.Group)
						.SendPortfolio(message.PortfolioName)
						.Send(condition.IsOpenOrClose ? "O" : "C")
						.Send((int)condition.Origin)
						.Send(message.Comment)
						.Send(condition.Transmit)
						.SendIf(ServerVersions.V4, s => s.Send(condition.ParentId))
						.SendIf(ServerVersions.V5, s =>
						{
							socket
								.Send(condition.BlockOrder)
								.Send(condition.SweepToFill)
								.Send(message.VisibleVolume)
								.Send((int)condition.TriggerMethod);

							if (socket.ServerVersion < ServerVersions.V38)
							{
								//will never happen
								socket.Send(false);
							}
							else
							{
								socket.Send(condition.OutsideRth);
							}
						})
						.SendIf(ServerVersions.V7, s => socket.Send(condition.Hidden));

					// TODO
					WeightedIndexSecurity indexSecurity = null;//security as WeightedIndexSecurity;

					if (indexSecurity != null)
					{
						// Send combo legs for BAG requests
						if (socket.ServerVersion >= ServerVersions.V8)
							socket.SendCombo(indexSecurity, condition);

						// Send order combo legs for BAG requests
						if (socket.ServerVersion >= ServerVersions.V57)
						{
							var legs = condition.Combo.Legs.ToArray();

							socket.Send(legs.Length);

							foreach (var leg in legs)
								socket.Send(leg);
						}

						if (socket.ServerVersion >= ServerVersions.V57)
						{
							var comboParams = condition.SmartRouting.ComboParams.ToArray();

							socket.Send(comboParams.Length);

							foreach (var comboParam in comboParams)
							{
								socket
									.Send(comboParam.Item1)
									.Send(comboParam.Item2);
							}
						}
					}

					if (socket.ServerVersion >= ServerVersions.V9)
					{
						socket.Send(string.Empty);
					}

					if (socket.ServerVersion >= ServerVersions.V10)
					{
						socket.Send(condition.SmartRouting.DiscretionaryAmount);
					}

					if (condition.GoodAfterTime == null || socket.ServerVersion < ServerVersions.V11)
						socket.Send(string.Empty);
					else
						socket.Send(condition.GoodAfterTime, IBSocketHelper.TimeFormat);

					if (message.TillDate == null || message.TillDate == DateTimeOffset.MaxValue || socket.ServerVersion < ServerVersions.V12)
						socket.Send(string.Empty);
					else
						socket.Send(message.TillDate.Value, IBSocketHelper.TimeFormat);

					if (socket.ServerVersion >= ServerVersions.V13)
					{
						socket
							.Send(condition.FinancialAdvisor.Group)
							.SendFinancialAdvisor(condition.FinancialAdvisor.Allocation)
							.Send(condition.FinancialAdvisor.Percentage)
							.Send(condition.FinancialAdvisor.Profile);
					}

					if (socket.ServerVersion >= ServerVersions.V18)
					{
						// institutional short sale slot fields.
						socket.SendShortSale(condition.ShortSale, true);
					}

					if (socket.ServerVersion >= ServerVersions.V19)
					{
						socket.Send((int)(condition.Oca.Type ?? 0));

						if (socket.ServerVersion < ServerVersions.V38)
						{
							//will never happen
							socket.Send(false);
						}

						socket
							.SendAgent(condition.Agent)
							.Send(condition.Clearing.SettlingFirm)
							.Send(condition.AllOrNone)
							.Send(condition.MinVolume)
							.Send(condition.PercentOffset)
							.Send(condition.SmartRouting.ETradeOnly)
							.Send(condition.SmartRouting.FirmQuoteOnly)
							.Send(condition.SmartRouting.NbboPriceCap)
							.Send((int?)condition.AuctionStrategy)
							.Send(condition.StartingPrice)
							.Send(condition.StockRefPrice)
							.Send(condition.Delta);

						decimal? lower = null;
						decimal? upper = null;

						if (socket.ServerVersion == ServerVersions.V26)
						{
							// Volatility orders had specific watermark price attribs in server version 26
							var isVol = condition.ExtendedType == IBOrderCondition.ExtendedOrderTypes.Volatility;
							lower = isVol ? condition.StockRangeLower : (decimal?)null;
							upper = isVol ? condition.StockRangeUpper : (decimal?)null;
						}

						socket
							.Send(lower)
							.Send(upper);
					}

					if (socket.ServerVersion >= ServerVersions.V22)
					{
						socket.Send(condition.OverridePercentageConstraints);
					}

					if (socket.ServerVersion >= ServerVersions.V26)
					{
						socket
							.Send(condition.Volatility.Volatility)
							.Send((int?)condition.Volatility.VolatilityTimeFrame)
							.SendDeltaNeutral(condition)
							.Send(condition.Volatility.ContinuousUpdate ? 1 : 0);

						if (socket.ServerVersion == ServerVersions.V26)
						{
							var isVol = condition.ExtendedType == IBOrderCondition.ExtendedOrderTypes.Volatility;

							socket
								.Send(isVol ? condition.StockRangeLower : (decimal?)null)
								.Send(isVol ? condition.StockRangeUpper : (decimal?)null);
						}

						socket.Send(condition.Volatility.IsAverageBestPrice);
					}

					if (socket.ServerVersion >= ServerVersions.V30)
					{
						// TRAIL_STOP_LIMIT stop price
						socket.Send(condition.TrailStopPrice);
					}

					if (socket.ServerVersion >= ServerVersions.V62)
					{
						socket.Send(condition.TrailStopVolumePercentage);
					}

					//Scale Orders require server version 35 or higher.
					if (socket.ServerVersion >= ServerVersions.V35)
					{
						if (socket.ServerVersion >= ServerVersions.V40)
						{
							socket
								.Send(condition.Scale.InitLevelSize)
								.Send(condition.Scale.SubsLevelSize);
						}
						else
						{
							socket
								.Send(string.Empty)
								.Send(condition.Scale.InitLevelSize);
						}

						socket.Send(condition.Scale.PriceIncrement);
					}

					if (socket.ServerVersion >= ServerVersions.V60 && condition.Scale.PriceIncrement > 0)
					{
						socket
							.Send(condition.Scale.PriceAdjustValue)
							.Send(condition.Scale.PriceAdjustInterval)
							.Send(condition.Scale.ProfitOffset)
							.Send(condition.Scale.AutoReset)
							.Send(condition.Scale.InitPosition)
							.Send(condition.Scale.InitFillQty)
							.Send(condition.Scale.RandomPercent);
					}

					if (socket.ServerVersion >= ServerVersions.V69)
					{
						socket
							.Send(condition.Scale.Table)
							.Send(condition.Active.Start, IBSocketHelper.TimeFormat)
							.Send(condition.Active.Stop, IBSocketHelper.TimeFormat);
					}

					socket.SendHedge(condition);

					if (socket.ServerVersion >= ServerVersions.V56)
					{
						socket.Send(condition.SmartRouting.OptOutSmartRouting);
					}

					if (socket.ServerVersion >= ServerVersions.V39)
					{
						socket
							.SendPortfolio(condition.Clearing.Portfolio)
							.SendIntent(condition.Clearing.Intent);
					}

					if (socket.ServerVersion >= ServerVersions.V44)
						socket.Send(condition.SmartRouting.NotHeld);

					if (socket.ServerVersion >= ServerVersions.V40)
					{
						//if (contract.UnderlyingComponent != null)
						//{
						//	UnderlyingComponent underComp = contract.UnderlyingComponent;
						//	send(true);
						//	send(underComp.ContractId);
						//	send(underComp.Delta);
						//	send(underComp.Price);
						//}
						//else
						{
							socket.Send(false);
						}
					}

					if (socket.ServerVersion >= ServerVersions.V41)
					{
						var algoStrategy = condition.Algo.Strategy;
						socket.Send(algoStrategy);

						var algoParams = condition.Algo.Params.ToArray();

						if (!algoStrategy.IsEmpty())
						{
							if (algoParams == null)
							{
								socket.Send(0);
							}
							else
							{
								socket.Send(algoParams.Length);

								foreach (var param in algoParams)
								{
									socket
										.Send(param.Item1)
										.Send(param.Item2);
								}
							}
						}
					}

					if (socket.ServerVersion >= ServerVersions.V71)
					{
						socket.Send(condition.AlgoId);
					}

					if (socket.ServerVersion >= ServerVersions.V36)
					{
						socket.Send(condition.WhatIf);
					}

					// send orderMiscOptions parameter
					if (socket.ServerVersion >= ServerVersions.V70)
					{
						socket.Send(condition.MiscOptions != null
							? condition.MiscOptions.Select(t => t.Item1 + "=" + t.Item2).Join(";")
							: string.Empty);
					}
				});
		}

		/// <summary>
		/// Call the exerciseOptions() method to exercise options. 
		/// “SMART” is not an allowed exchange in exerciseOptions() calls, and that TWS does a moneyness request for the position in question whenever any API initiated exercise or lapse is attempted.
		/// </summary>
		/// <param name="message">this structure contains a description of the contract to be exercised.  If no multiplier is specified, a default of 100 is assumed.</param>
		/// <param name="isExercise">this can have two values:
		/// 1 = specifies exercise
		/// 2 = specifies lapse
		/// </param>
		/// <param name="volume">the number of contracts to be exercised</param>
		/// <param name="portfolioName">specifies whether your setting will override the system's natural action. For example, if your action is "exercise" and the option is not in-the-money, by natural action the option would not exercise. If you have override set to "yes" the natural action would be overridden and the out-of-the money option would be exercised. Values are: 
		/// 0 = no
		/// 1 = yes
		/// </param>
		/// <param name="isOverride">
		/// specifies whether your setting will override the system's natural action. For example, if your action is "exercise" and the option is not in-the-money, by natural action the option would not exercise. If you have override set to "yes" the natural action would be overridden and the out-of-the money option would be exercised. Values are: 
		/// 0 = no
		/// 1 = yes
		/// </param>
		private void ExerciseOptions(OrderRegisterMessage message, bool isExercise, decimal volume, string portfolioName, bool isOverride)
		{
			//var option = SessionHolder.Securities[regMsg.SecurityId];

			ProcessRequest(RequestMessages.ExerciseOptions, ServerVersions.V21, ServerVersions.V2, socket =>
				socket
					.Send(message.TransactionId)
					.SendIf(ServerVersions.V68, s => socket.SendContractId(message.SecurityId))
					.SendSecurity(message, false)
					.SendIf(ServerVersions.V68, s => socket.Send(message.Class))
					.Send(isExercise ? 1 : 2)
					.Send((int)volume)
					.Send(portfolioName)
					.Send(isOverride));
		}

		/// <summary>
		/// Call this function to start getting account values, portfolio, and last update time information.
		/// </summary>
		/// <param name="isSubscribe">If set to <see langword="true"/>, the client will start receiving account and portfolio updates. If set to <see langword="false"/>, the client will stop receiving this information.</param>
		/// <param name="portfolioName">the account code for which to receive account and portfolio updates.</param>
		private void SubscribePortfolio(string portfolioName, bool isSubscribe)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException("portfolioName");

			ProcessRequest(RequestMessages.RequestAccountData, 0, ServerVersions.V2,
				socket =>
					socket
						.Send(isSubscribe)
						.SendPortfolio(portfolioName, false));
		}

		/// <summary>
		/// When this method is called, the execution reports that meet the filter criteria are downloaded to the client via the execDetails() method.
		/// </summary>
		/// <param name="requestId"></param>
		/// <param name="filter">the filter criteria used to determine which execution reports are returned.</param>
		private void ReqeustMyTrades(long requestId, MyTradeFilter filter)
		{
			if (filter == null)
				throw new ArgumentNullException("filter");

			ProcessRequest(RequestMessages.RequestTrades, 0, ServerVersions.V3, socket =>
			{
				if (socket.ServerVersion >= ServerVersions.V42)
					socket.Send(requestId);

				if (socket.ServerVersion >= ServerVersions.V9)
				{
					socket
						.Send(filter.ClientId)
						.Send(filter.Portfolio)
						.Send(filter.Time.ToUniversalTime().ToString("yyyyMMdd-HH:mm:ss"))
						.Send(filter.SecurityId.SecurityCode)
						.SendSecurityType(filter.SecurityType)
						.SendBoardCode(filter.BoardCode)
						.SendSide(filter.Side);
				}
			});
		}

		/// <summary>
		/// Call this method to request the open orders that were placed from this client. Each open order will be fed back through the openOrder() and orderStatus() functions on the EWrapper.
		/// 
		/// The client with a clientId of "0" will also receive the TWS-owned open orders. These orders will be associated with the client and a new orderId will be generated. This association will persist over multiple API and TWS sessions.
		/// </summary>
		private void RequestOpenOrders()
		{
			ProcessRequest(RequestMessages.RequestOpenOrders, 0, ServerVersions.V1, socket => { });
		}

		/// <summary>
		/// Call this method to request that newly created TWS orders be implicitly associated with the client. When a new TWS order is created, the order will be associated with the client and fed back through the openOrder() and orderStatus() methods on the EWrapper.
		/// 
		/// TWS orders can only be bound to clients with a clientId of “0”.
		/// </summary>
		/// <param name="autoBind">If set to <see langword="true"/>, newly created TWS orders will be implicitly associated with the client. If set to <see langword="false"/>, no association will be made.</param>
		private void RequestAutoOpenOrders(bool autoBind)
		{
			ProcessRequest(RequestMessages.RequestAllOpenOrders, 0, ServerVersions.V1, socket => socket.Send(autoBind));
		}

		/// <summary>
		/// Call this method to request the open orders that were placed from all clients and also from TWS. Each open order will be fed back through the openOrder() and orderStatus() functions on the EWrapper.
		/// 
		/// No association is made between the returned orders and the requesting client.
		/// </summary>
		private void RequestAllOpenOrders()
		{
			ProcessRequest(RequestMessages.RequestAllOpenOrders, 0, ServerVersions.V1, socket => { });
		}

		/// <summary>
		/// Call this method to request the list of managed accounts. The list will be returned by the managedAccounts() function on the EWrapper.
		/// 
		/// This request can only be made when connected to a Financial Advisor (FA) account.
		/// </summary>
		private void RequestPortfolios()
		{
			ProcessRequest(RequestMessages.RequestPortfolios, 0, ServerVersions.V1, socket => { });
		}

		private void SubscribePosition()
		{
			ProcessRequest(RequestMessages.SubscribePosition, ServerVersions.V67, ServerVersions.V1, socket => { });
		}

		private void UnSubscribePosition()
		{
			ProcessRequest(RequestMessages.UnSubscribePosition, ServerVersions.V67, ServerVersions.V1, socket => { });
		}

		private void SubscribeAccountSummary(long requestId, string group, IEnumerable<AccountSummaryTag> tags)
		{
			if (tags == null)
				throw new ArgumentNullException("tags");

			ProcessRequest(RequestMessages.SubscribeAccountSummary, ServerVersions.V67, ServerVersions.V1,
				socket =>
					socket
						.Send(requestId)
						.Send(group)
						.Send(tags.Select(t => t.To<string>()).Join(",")));
		}

		private void UnSubscribeAccountSummary(long requestId)
		{
			ProcessRequest(RequestMessages.UnSubscribeAccountSummary, ServerVersions.V67, ServerVersions.V1,
				socket => socket.Send(requestId));
		}

		private void RequestGlobalCancel()
		{
			ProcessRequest(RequestMessages.RequestGlobalCancel, ServerVersions.V53, ServerVersions.V1, socket => { });
		}

		private void ReadPortfolioName(IBSocket socket)
		{
			/*var str = */socket.ReadStr();

			//return str.IsEmpty() ? null : str;
		}

		private void ReadAccountSummaryEnd(IBSocket socket)
		{
			var requestId = socket.ReadInt();
			SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = requestId });
		}

		private void ReadOrderStatus(IBSocket socket, ServerVersions version)
		{
			var id = socket.ReadInt();

			var status = socket.ReadOrderStatus();
			/* filled */
			socket.ReadInt();
			var balance = socket.ReadInt();
			var avgPrice = socket.ReadDecimal();
			var permId = version >= ServerVersions.V2 ? socket.ReadInt() : (int?)null;
			var parentId = version >= ServerVersions.V3 ? socket.ReadInt() : (int?)null;
			var lastTradePrice = version >= ServerVersions.V4 ? socket.ReadDecimal() : (decimal?)null;
			var clientId = version >= ServerVersions.V5 ? socket.ReadInt() : (int?)null;
			var whyHeld = version >= ServerVersions.V6 ? socket.ReadStr() : null;

			var execMsg = new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				OriginalTransactionId = id,
				Balance = balance,
				OrderStatus = status,
				OrderState = status.ToOrderState(),
			};

			execMsg.SetAveragePrice(avgPrice);

			if (permId != null)
				execMsg.SetPermId(permId.Value);

			if (parentId != null)
				execMsg.Condition = new IBOrderCondition { ParentId = parentId.Value };

			if (lastTradePrice != null)
				execMsg.SetLastTradePrice(lastTradePrice.Value);

			if (clientId != null)
				execMsg.SetClientId(clientId.Value);

			if (whyHeld != null)
				execMsg.SetWhyHeld(whyHeld);

			SendOutMessage(execMsg);
		}

		private void ReadPortfolio(IBSocket socket, ServerVersions version)
		{
			var name = socket.ReadStr();
			var value = socket.ReadStr();
			var currency = socket.ReadStr();
			var port = version >= ServerVersions.V2 ? socket.ReadStr() : null;

			if (port == null || currency == "BASE")
				return;

			var pfMsg = this.CreatePortfolioChangeMessage(port);

			switch (name)
			{
				case "CashBalance":
					pfMsg.Add(PositionChangeTypes.CurrentValue, value.To<decimal>());
					break;
				case "Currency":
					pfMsg.Add(PositionChangeTypes.Currency, currency.To<CurrencyTypes>());
					break;
				case "RealizedPnL":
					pfMsg.Add(PositionChangeTypes.RealizedPnL, value.To<decimal>());
					break;
				case "UnrealizedPnL":
					pfMsg.Add(PositionChangeTypes.UnrealizedPnL, value.To<decimal>());
					break;
				case "NetLiquidation":
					pfMsg.Add(PositionChangeTypes.CurrentPrice, value.To<decimal>());
					break;
				case "Leverage-S":
					pfMsg.Add(PositionChangeTypes.Leverage, value.To<decimal>());
					break;
				default:
					var info = pfMsg.ExtensionInfo = new Dictionary<object, object>();

					if (currency.IsEmpty())
						info[name] = value;
					else
					{
						info[name] = new Currency
						{
							Type = currency.To<CurrencyTypes>(),
							Value = value.To<decimal>(),
						};
					}

					break;
			}

			SendOutMessage(pfMsg);
		}

		private void ReadPortfolioPosition(IBSocket socket, ServerVersions version)
		{
			var contractId = version >= ServerVersions.V6 ? socket.ReadInt() : -1;

			var secName = socket.ReadStr();
			var type = socket.ReadSecurityType();
			var expiryDate = socket.ReadExpiry();
			var strike = socket.ReadDecimal();
			var optionType = socket.ReadOptionType();
			var multiplier = version >= ServerVersions.V7 ? socket.ReadMultiplier() : null;
			var boardCode = version >= ServerVersions.V7 ? socket.ReadBoardCode() : null;
			var currency = socket.ReadCurrency();

			var secCode = (version >= ServerVersions.V2) ? socket.ReadStr() : secName;

			var secClass = (version >= ServerVersions.V8) ? socket.ReadStr() : null;

			var position = socket.ReadInt();
			var marketPrice = socket.ReadDecimal();
			var marketValue = socket.ReadDecimal();

			var averagePrice = 0m;
			var unrealizedPnL = 0m;
			var realizedPnL = 0m;
			if (version >= ServerVersions.V3)
			{
				averagePrice = socket.ReadDecimal();
				unrealizedPnL = socket.ReadDecimal();
				realizedPnL = socket.ReadDecimal();
			}

			var portfolio = version >= ServerVersions.V4 ? socket.ReadStr() : null;

			if (version == ServerVersions.V6 && socket.ServerVersion == ServerVersions.V39)
				boardCode = socket.ReadBoardCode();

			var secId = new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = GetBoardCode(boardCode),
				InteractiveBrokers = contractId,
			};

			SendOutMessage(new SecurityMessage
			{
				SecurityId = secId,
				Name = secName,
				SecurityType = type,
				ExpiryDate = expiryDate,
				Strike = strike,
				OptionType = optionType,
				Currency = currency,
				Multiplier = multiplier ?? 0,
				Class = secClass
			});

			if (portfolio.IsEmpty())
				return;

			SendOutMessage(
				this
					.CreatePositionChangeMessage(portfolio, secId)
						.Add(PositionChangeTypes.CurrentValue, (decimal)position)
						.Add(PositionChangeTypes.CurrentPrice, marketPrice)
						.Add(PositionChangeTypes.AveragePrice, averagePrice)
						.Add(PositionChangeTypes.UnrealizedPnL, unrealizedPnL)
						.Add(PositionChangeTypes.RealizedPnL, realizedPnL));

			// TODO
			//pos.SetMarketValue(marketValue);
		}

		private void ReadPortfolioUpdateTime(IBSocket socket)
		{
			/*var timeStamp = */socket.ReadStr();
			//updateAccountTime(timeStamp);
		}

		private void ReadOpenOrder(IBSocket socket, ServerVersions version)
		{
			var transactionId = socket.ReadInt();

			var contractId = version >= ServerVersions.V17 ? socket.ReadInt() : -1;

			var secCode = socket.ReadStr();
			var type = socket.ReadSecurityType();
			var expiryDate = socket.ReadExpiry();
			var strike = socket.ReadDecimal();
			var optionType = socket.ReadOptionType();
			var multiplier = version >= ServerVersions.V32 ? socket.ReadMultiplier() : null;
			var boardCode = socket.ReadBoardCode();
			var currency = socket.ReadCurrency();
			secCode = version >= ServerVersions.V2 ? socket.ReadLocalCode(secCode) : null;
			var secClass = (version >= ServerVersions.V32) ? socket.ReadStr() : null;

			var ibCon = new IBOrderCondition();

			// read order fields
			var direction = socket.ReadOrderSide();
			var volume = socket.ReadInt();

			OrderTypes orderType;
			IBOrderCondition.ExtendedOrderTypes? extendedType;
			socket.ReadOrderType(out orderType, out extendedType);
			ibCon.ExtendedType = extendedType;

			var price = socket.ReadDecimal();
			ibCon.StopPrice = socket.ReadDecimal();
			var expiration = socket.ReadStr();
			ibCon.Oca.Group = socket.ReadStr();
			var portfolio = socket.ReadStr();
			ibCon.IsOpenOrClose = socket.ReadStr() == "O";
			ibCon.Origin = (IBOrderCondition.OrderOrigins)socket.ReadInt();
			var comment = socket.ReadStr();

			var clientId = version >= ServerVersions.V3 ? socket.ReadInt() : (int?)null;
			int? permId = null;

			if (version >= ServerVersions.V4)
			{
				permId = socket.ReadInt();

				if (version < ServerVersions.V18)
				{
					// will never happen
					/* order.m_ignoreRth = */
					socket.ReadBool();
				}
				else
					ibCon.OutsideRth = socket.ReadBool();

				ibCon.Hidden = socket.ReadBool();
				ibCon.SmartRouting.DiscretionaryAmount = socket.ReadDecimal();
			}

			if (version >= ServerVersions.V5)
				ibCon.GoodAfterTime = socket.ReadNullDateTime(IBSocketHelper.TimeFormat);

			if (version >= ServerVersions.V6)
			{
				// skip deprecated sharesAllocation field
				socket.ReadStr();
			}

			if (version >= ServerVersions.V7)
			{
				ibCon.FinancialAdvisor.Group = socket.ReadStr();
				ibCon.FinancialAdvisor.Allocation = socket.ReadFinancialAdvisor();
				ibCon.FinancialAdvisor.Percentage = socket.ReadStr();
				ibCon.FinancialAdvisor.Profile = socket.ReadStr();
			}

			var orderExpiryDate = version >= ServerVersions.V8 ? socket.ReadNullDateTime(IBSocketHelper.TimeFormat) : null;
			var visibleVolume = volume;

			if (version >= ServerVersions.V9)
			{
				ibCon.Agent = socket.ReadAgent();
				ibCon.PercentOffset = socket.ReadDecimal();
				ibCon.Clearing.SettlingFirm = socket.ReadStr();
				ibCon.ShortSale.Slot = (IBOrderCondition.ShortSaleSlots)socket.ReadInt();
				ibCon.ShortSale.Location = socket.ReadStr();

				if (socket.ServerVersion == ServerVersions.V51)
					socket.ReadInt(); //exempt code
				else if (version >= ServerVersions.V23)
					ibCon.ShortSale.ExemptCode = socket.ReadInt();

				ibCon.AuctionStrategy = (IBOrderCondition.AuctionStrategies)socket.ReadInt();
				ibCon.StartingPrice = socket.ReadDecimal();
				ibCon.StockRefPrice = socket.ReadDecimal();
				ibCon.Delta = socket.ReadDecimal();
				ibCon.StockRangeLower = socket.ReadDecimal();
				ibCon.StockRangeUpper = socket.ReadDecimal();
				visibleVolume = socket.ReadInt();

				if (version < ServerVersions.V18)
				{
					// will never happen
					/* order.m_rthOnly = */
					socket.ReadBool();
				}

				ibCon.BlockOrder = socket.ReadBool();
				ibCon.SweepToFill = socket.ReadBool();
				ibCon.AllOrNone = socket.ReadBool();
				ibCon.MinVolume = socket.ReadInt();
				ibCon.Oca.Type = (IBOrderCondition.OcaTypes)socket.ReadInt();
				ibCon.SmartRouting.ETradeOnly = socket.ReadBool();
				ibCon.SmartRouting.FirmQuoteOnly = socket.ReadBool();
				ibCon.SmartRouting.NbboPriceCap = socket.ReadDecimal();
			}

			if (version >= ServerVersions.V10)
			{
				ibCon.ParentId = socket.ReadInt();
				ibCon.TriggerMethod = (IBOrderCondition.TriggerMethods)socket.ReadInt();
			}

			if (version >= ServerVersions.V11)
			{
				ibCon.Volatility.Volatility = socket.ReadDecimal();
				ibCon.Volatility.VolatilityTimeFrame = socket.ReadVolatilityType();

				if (version == ServerVersions.V11)
				{
					if (!socket.ReadBool())
						ibCon.Volatility.ExtendedOrderType = IBOrderCondition.ExtendedOrderTypes.Empty;
					else
						ibCon.Volatility.OrderType = OrderTypes.Market;
				}
				else
				{
					OrderTypes volOrdertype;
					IBOrderCondition.ExtendedOrderTypes? volExtendedType;
					socket.ReadOrderType(out volOrdertype, out volExtendedType);
					ibCon.Volatility.OrderType = volOrdertype;
					ibCon.Volatility.ExtendedOrderType = volExtendedType;

					ibCon.Volatility.StopPrice = socket.ReadDecimal();

					if (volExtendedType != IBOrderCondition.ExtendedOrderTypes.Empty)
					{
						if (version >= ServerVersions.V27)
						{
							ibCon.Volatility.ConId = socket.ReadInt();
							ibCon.Volatility.SettlingFirm = socket.ReadStr();

							var portfolioName = socket.ReadStr();
							if (!portfolioName.IsEmpty())
								ibCon.Volatility.ClearingPortfolio = portfolioName;

							ibCon.Volatility.ClearingIntent = socket.ReadStr();
						}

						if (version >= ServerVersions.V31)
						{
							var isOpenOrCloseStr = socket.ReadStr();
							ibCon.Volatility.ShortSale.IsOpenOrClose = isOpenOrCloseStr == "?" ? (bool?)null : isOpenOrCloseStr.To<int>() == 1;
							ibCon.Volatility.IsShortSale = socket.ReadBool();
							ibCon.Volatility.ShortSale.Slot = (IBOrderCondition.ShortSaleSlots)socket.ReadInt();
							ibCon.Volatility.ShortSale.Location = socket.ReadStr();
						}
					}
				}

				ibCon.Volatility.ContinuousUpdate = socket.ReadBool();

				if (socket.ServerVersion == ServerVersions.V26)
				{
					ibCon.StockRangeLower = socket.ReadDecimal();
					ibCon.StockRangeUpper = socket.ReadDecimal();
				}

				ibCon.Volatility.IsAverageBestPrice = socket.ReadBool();
			}

			if (version >= ServerVersions.V13)
				ibCon.TrailStopPrice = socket.ReadDecimal();

			if (version >= ServerVersions.V30)
				ibCon.TrailStopVolumePercentage = socket.ReadNullDecimal();

			if (version >= ServerVersions.V14)
			{
				ibCon.Combo.BasisPoints = socket.ReadDecimal();
				ibCon.Combo.BasisPointsType = socket.ReadInt();
				ibCon.Combo.LegsDescription = socket.ReadStr();
			}

			if (version >= ServerVersions.V29)
			{
				var comboLegsCount = socket.ReadInt();
				if (comboLegsCount > 0)
				{
					//contract.m_comboLegs = new Vector(comboLegsCount);
					for (var i = 0; i < comboLegsCount; ++i)
					{
						//int conId = 
						socket.ReadInt();
						//int ratio = 
						socket.ReadInt();
						//String action = 
						socket.ReadStr();
						//String exchange = 
						socket.ReadStr();
						//int openClose = 
						socket.ReadInt();
						//int shortSaleSlot = 
						socket.ReadInt();
						//String designatedLocation = 
						socket.ReadStr();
						//int exemptCode = 
						socket.ReadInt();

						//ComboLeg comboLeg = new ComboLeg(conId, ratio, action, exchange, openClose,
						//		shortSaleSlot, designatedLocation, exemptCode);
						//contract.m_comboLegs.add(comboLeg);
					}
				}

				var orderComboLegsCount = socket.ReadInt();
				if (orderComboLegsCount > 0)
				{
					//order.m_orderComboLegs = new Vector(orderComboLegsCount);
					for (var i = 0; i < orderComboLegsCount; ++i)
					{
						//var comboPrice = 
						socket.ReadNullDecimal();
						//OrderComboLeg orderComboLeg = new OrderComboLeg(comboPrice);
						//order.m_orderComboLegs.add(orderComboLeg);
					}
				}
			}

			if (version >= ServerVersions.V26)
			{
				var smartComboRoutingParamsCount = socket.ReadInt();
				if (smartComboRoutingParamsCount > 0)
				{
					var @params = new List<Tuple<string, string>>();

					for (var i = 0; i < smartComboRoutingParamsCount; ++i)
						@params.Add(Tuple.Create(socket.ReadStr(), socket.ReadStr()));

					ibCon.SmartRouting.ComboParams = @params;
				}
			}

			if (version >= ServerVersions.V15)
			{
				if (version >= ServerVersions.V20)
				{
					ibCon.Scale.InitLevelSize = socket.ReadNullInt();
					ibCon.Scale.SubsLevelSize = socket.ReadNullInt();
				}
				else
				{
					/* int notSuppScaleNumComponents = */
					socket.ReadNullInt();
					ibCon.Scale.InitLevelSize = socket.ReadNullInt();
				}

				ibCon.Scale.PriceIncrement = socket.ReadNullDecimal();
			}

			if (version >= ServerVersions.V28 && ibCon.Scale.PriceIncrement > 0)
			{
				ibCon.Scale.PriceAdjustValue = socket.ReadNullDecimal();
				ibCon.Scale.PriceAdjustInterval = socket.ReadInt();
				ibCon.Scale.ProfitOffset = socket.ReadNullDecimal();
				ibCon.Scale.AutoReset = socket.ReadBool();
				ibCon.Scale.InitPosition = socket.ReadNullInt();
				ibCon.Scale.InitFillQty = socket.ReadNullInt();
				ibCon.Scale.RandomPercent = socket.ReadBool();
			}

			if (version >= ServerVersions.V24)
				socket.ReadHedge(ibCon);

			if (version >= ServerVersions.V25)
				ibCon.SmartRouting.OptOutSmartRouting = socket.ReadBool();

			if (version >= ServerVersions.V19)
			{
				var portfolioName = socket.ReadStr();

				if (!portfolioName.IsEmpty())
					ibCon.Clearing.ClearingPortfolio = portfolioName;

				ibCon.Clearing.Intent = socket.ReadIntent();
			}

			if (version >= ServerVersions.V22)
				ibCon.SmartRouting.NotHeld = socket.ReadBool();

			if (version >= ServerVersions.V20)
			{
				if (socket.ReadBool())
				{
					//UnderlyingComponent underComp = new UnderlyingComponent();
					//underComp.ContractId = 
					socket.ReadInt();
					//underComp.Delta = 
					socket.ReadDecimal();
					//underComp.Price = 
					socket.ReadDecimal();
					//contract.UnderlyingComponent = underComp;
				}
			}

			if (version >= ServerVersions.V21)
			{
				ibCon.Algo.Strategy = socket.ReadStr();

				if (!ibCon.Algo.Strategy.IsEmpty())
				{
					var algoParamsCount = socket.ReadInt();

					if (algoParamsCount > 0)
					{
						var algoParams = new List<Tuple<string, string>>();

						for (var i = 0; i < algoParamsCount; i++)
							algoParams.Add(Tuple.Create(socket.ReadStr(), socket.ReadStr()));

						ibCon.Algo.Params = algoParams;
					}
				}
			}

			//OrderState orderState = new OrderState();

			OrderStatus? status = null;

			if (version >= ServerVersions.V16)
			{
				socket.ReadStr();
				//order.WhatIf = !(string.IsNullOrEmpty(rstr) || rstr == "0");

				status = socket.ReadOrderStatus();
				//orderState.InitMargin = 
				socket.ReadStr();
				//orderState.MaintMargin = 
				socket.ReadStr();
				//orderState.EquityWithLoan = 
				socket.ReadStr();
				//orderState.IbCommission = 
				socket.ReadNullDecimal();
				//orderState.MinCommission = 
				socket.ReadNullDecimal();
				//orderState.MaxCommission = 
				socket.ReadNullDecimal();
				//orderState.CommissionCurrency = 
				socket.ReadStr();
				//orderState.WarningText = 
				socket.ReadStr();
			}

			var secId = new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = GetBoardCode(boardCode),
				InteractiveBrokers = contractId,
			};

			SendOutMessage(new SecurityMessage
			{
				SecurityId = secId,
				ExpiryDate = expiryDate,
				Strike = strike,
				OptionType = optionType,
				Class = secClass,
				SecurityType = type,
				Currency = currency,
				Multiplier = multiplier ?? 0,
			});

			var orderMsg = new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				SecurityId = secId,
				OriginalTransactionId = transactionId,
				OrderType = orderType,
				Side = direction,
				Volume = volume,
				Price = price,
				Condition = ibCon,
				ExpiryDate = orderExpiryDate,
				VisibleVolume = visibleVolume,
				PortfolioName = portfolio,
				Comment = comment,
				OrderStatus = status,
				OrderState = status == null ? (OrderStates?)null : status.Value.ToOrderState(),
			};

			if (orderMsg.OrderState == OrderStates.Active || orderMsg.OrderState == OrderStates.Done)
				orderMsg.OrderId = transactionId;

			switch (expiration)
			{
				case "DAY":
					orderMsg.TimeInForce = TimeInForce.PutInQueue;
					break;
				case "GTC":
					//orderMsg.ExpiryDate = DateTimeOffset.MaxValue;
					break;
				case "IOC":
					orderMsg.TimeInForce = TimeInForce.CancelBalance;
					break;
				case "FOK":
					orderMsg.TimeInForce = TimeInForce.MatchOrCancel;
					break;
				case "GTD":
					break;
				case "OPG":
					ibCon.IsMarketOnOpen = true;
					break;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2515Params.Put(expiration));
			}

			if (clientId != null)
				orderMsg.SetClientId(clientId.Value);

			if (permId != null)
				orderMsg.SetPermId(permId.Value);

			SendOutMessage(orderMsg);
		}

		private void ReadNextOrderId(IBSocket socket)
		{
			((IncrementalIdGenerator)TransactionIdGenerator).Current = socket.ReadInt();
		}

		private void ReadManagedAccounts(IBSocket socket)
		{
			var names = socket.ReadStr().Split(',');
			//managedAccounts(accountsList);

			names.ForEach(pf => SendOutMessage(new PortfolioMessage { PortfolioName = pf }));
		}

		private void ReadMyTrade(IBSocket socket, ServerVersions version)
		{
			/* requestId */
			if (version >= ServerVersions.V7)
				socket.ReadInt();

			// http://www.interactivebrokers.com/en/software/api/apiguide/java/execution.htm

			var transactionId = socket.ReadInt();

			//Handle the 2^31-1 == 0 bug
			if (transactionId == int.MaxValue)
				transactionId = 0;

			//Read Contract Fields
			var contractId = version >= ServerVersions.V5 ? socket.ReadInt() : -1;

			var secName = socket.ReadStr();
			var type = socket.ReadSecurityType();
			var expiryDate = socket.ReadExpiry();
			var strike = socket.ReadDecimal();
			var optionType = socket.ReadOptionType();
			var multiplier = version >= ServerVersions.V9 ? socket.ReadMultiplier() : null;
			var boardCode = socket.ReadBoardCode();
			var currency = socket.ReadCurrency();
			var secCode = socket.ReadLocalCode(secName);
			var secClass = (version >= ServerVersions.V10) ? socket.ReadStr() : null;

			var tradeId = socket.ReadStr();
			var time = socket.ReadDateTime("yyyyMMdd  HH:mm:ss");
			var portfolio = socket.ReadStr();
			/* exchange */
			socket.ReadStr();
			var side = socket.ReadTradeSide();
			var volume = socket.ReadInt();
			var price = socket.ReadDecimal();
			var permId = version >= ServerVersions.V2 ? socket.ReadInt() : (int?)null;
			var clientId = version >= ServerVersions.V3 ? socket.ReadInt() : (int?)null;
			var liquidation = version >= ServerVersions.V4 ? socket.ReadInt() : (int?)null;
			var cumulativeQuantity = version >= ServerVersions.V6 ? socket.ReadInt() : (int?)null;
			var averagePrice = version >= ServerVersions.V6 ? socket.ReadDecimal() : (decimal?)null;
			var orderRef = version >= ServerVersions.V8 ? socket.ReadStr() : null;
			var evRule = version >= ServerVersions.V9 ? socket.ReadStr() : null;
			var evMultiplier = version >= ServerVersions.V9 ? socket.ReadDecimal() : (decimal?)null;

			var secId = new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = GetBoardCode(boardCode),
				InteractiveBrokers = contractId,
			};

			SendOutMessage(new SecurityMessage
			{
				SecurityId = secId,
				Name = secName,
				SecurityType = type,
				ExpiryDate = expiryDate,
				Strike = strike,
				OptionType = optionType,
				Currency = currency,
				Multiplier = multiplier ?? 0,
				Class = secClass
			});

			// заявка была создана руками
			if (transactionId == 0)
				return;

			_secIdByTradeIds[tradeId] = secId;

			var execMsg = new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Trade,
				OriginalTransactionId = transactionId,
				TradeStringId = tradeId,
				OriginSide = side,
				TradePrice = price,
				Volume = volume,
				PortfolioName = portfolio,
				ServerTime = time,
				SecurityId = secId,
			};

			if (permId != null)
				execMsg.SetPermId(permId.Value);

			if (clientId != null)
				execMsg.SetClientId(clientId.Value);

			if (liquidation != null)
				execMsg.SetLiquidation(liquidation.Value);

			if (cumulativeQuantity != null)
				execMsg.SetCumulativeQuantity(cumulativeQuantity.Value);

			if (averagePrice != null)
				execMsg.SetAveragePrice(averagePrice.Value);

			if (orderRef != null)
				execMsg.SetOrderRef(orderRef);

			if (evRule != null)
				execMsg.SetEvRule(evRule);

			if (evMultiplier != null)
				execMsg.SetEvMultiplier(evMultiplier.Value);

			SendOutMessage(execMsg);
		}

		private void ReadMyTradeEnd(IBSocket socket)
		{
			/* requestId */
			socket.ReadInt();
			//executionDataEnd(reqId);
		}

		private void ReadCommissionReport(IBSocket socket)
		{
			var tradeId = socket.ReadStr();
			var value = socket.ReadDecimal();
			var currency = socket.ReadCurrency();
			var pnl = socket.ReadNullDecimal();
			var yield = socket.ReadNullDecimal();
			var redemptionDate = socket.ReadNullDateTime("yyyyMMdd");

			var secId = _secIdByTradeIds.TryGetValue2(tradeId);

			if (secId == null)
				return;

			// TODO
			//SendOutMessage(new ExecutionMessage
			//{
			//	ExecutionType = ExecutionTypes.Trade,
			//	TradeStringId = tradeId,
			//	Commission = value,
			//	SecurityId = secId.Value,
			//});
		}

		private void ReadPosition(IBSocket socket, ServerVersions version)
		{
			var account = socket.ReadStr();

			var contractId = socket.ReadInt();
			var secName = socket.ReadStr();
			var type = socket.ReadSecurityType();
			var expiryDate = socket.ReadExpiry();
			var strike = socket.ReadDecimal();
			var optionType = socket.ReadOptionType();
			var multiplier = socket.ReadMultiplier();
			var boardCode = socket.ReadBoardCode();
			var currency = socket.ReadCurrency();
			var secCode = socket.ReadLocalCode(secName);
			var secClass = (version >= ServerVersions.V2) ? socket.ReadStr() : null;

			var pos = socket.ReadInt();

			var avgCost = 0m;
			if (version >= ServerVersions.V3)
				avgCost = socket.ReadDecimal();

			var secId = new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = GetBoardCode(boardCode),
				InteractiveBrokers = contractId,
			};

			SendOutMessage(new SecurityMessage
			{
				SecurityId = secId,
				Name = secName,
				SecurityType = type,
				ExpiryDate = expiryDate,
				Strike = strike,
				OptionType = optionType,
				Currency = currency,
				Multiplier = multiplier ?? 0,
				Class = secClass
			});

			SendOutMessage(this
				.CreatePositionChangeMessage(account, secId)
					.Add(PositionChangeTypes.CurrentValue, (decimal)pos)
					.Add(PositionChangeTypes.AveragePrice, avgCost));
		}

		private void ReadPositionEnd(IBSocket socket)
		{
		}

		private void ReadAccountSummary(IBSocket socket)
		{
			/* requestId */
			socket.ReadInt();

			var account = socket.ReadStr();
			var tag = socket.ReadStr().To<AccountSummaryTag>();
			var value = socket.ReadStr();
			var currency = socket.ReadCurrency();

			var msg = this.CreatePortfolioChangeMessage(account);

			msg.Add(PositionChangeTypes.Currency, currency);

			switch (tag)
			{
				case AccountSummaryTag.TotalCashValue:
					msg.Add(PositionChangeTypes.CurrentValue, value.To<decimal>());
					break;
				case AccountSummaryTag.SettledCash:
					msg.Add(PositionChangeTypes.BlockedValue, value.To<decimal>());
					break;
				case AccountSummaryTag.AccruedCash:
					msg.Add(PositionChangeTypes.VariationMargin, value.To<decimal>());
					break;
				case AccountSummaryTag.InitMarginReq:
					msg.Add(PositionChangeTypes.BeginValue, value.To<decimal>());
					break;
				case AccountSummaryTag.Leverage:
					msg.Add(PositionChangeTypes.Leverage, value.To<decimal>());
					break;
			}

			SendOutMessage(msg);
		}

		private bool ProcessTransactionResponse(IBSocket socket, ResponseMessages message, ServerVersions version)
		{
			switch (message)
			{
				case ResponseMessages.OrderStatus:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/orderstatus.htm
					ReadOrderStatus(socket, version);
					return true;
				}
				case ResponseMessages.Portfolio:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/updateaccountvalue.htm
					ReadPortfolio(socket, version);
					return true;
				}
				case ResponseMessages.PortfolioPosition:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/updateportfolio.htm
					ReadPortfolioPosition(socket, version);
					return true;
				}
				case ResponseMessages.PortfolioUpdateTime:
				{
					ReadPortfolioUpdateTime(socket);
					return true;
				}
				case ResponseMessages.OpenOrder:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/openorder.htm
					ReadOpenOrder(socket, version);
					return true;
				}
				case ResponseMessages.NextOrderId:
				{
					ReadNextOrderId(socket);
					return true;
				}
				case ResponseMessages.MyTrade:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/execdetails.htm
					ReadMyTrade(socket, version);
					return true;
				}
				case ResponseMessages.ManagedAccounts:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/managedaccounts.htm
					ReadManagedAccounts(socket);
					return true;
				}
				case ResponseMessages.OpenOrderEnd:
				{
					//openOrderEnd(socket);
					return true;
				}
				case ResponseMessages.AccountDownloadEnd:
				{
					ReadPortfolioName(socket);
					return true;
				}
				case ResponseMessages.MyTradeEnd:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/execdetailsend.htm
					ReadMyTradeEnd(socket);
					return true;
				}
				case ResponseMessages.CommissionReport:
				{
					ReadCommissionReport(socket);
					return true;
				}
				case ResponseMessages.Position:
				{
					ReadPosition(socket, version);
					return true;
				}
				case ResponseMessages.PositionEnd:
				{
					ReadPositionEnd(socket);
					return true;
				}
				case ResponseMessages.AccountSummary:
				{
					ReadAccountSummary(socket);
					return true;
				}
				case ResponseMessages.AccountSummaryEnd:
				{
					ReadAccountSummaryEnd(socket);
					return true;
				}
				default:
					return false;
			}
		}

		private void OnProcessOrderCancelled(int transactionId)
		{
			//SendOutMessage(new ExecutionMessage
			//{
			//	ExecutionType = ExecutionTypes.Order,
			//	OriginalTransactionId = transactionId,
			//	OrderState = OrderStates.Done
			//});
		}

		private void OnProcessOrderError(int transactionId, string errorMsg)
		{
			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				OriginalTransactionId = transactionId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(errorMsg),
			});
		}
	}
}