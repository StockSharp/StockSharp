namespace StockSharp.InteractiveBrokers.Native
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	static class IBSocketHelper
	{
		private const string _expiryFormat = "yyyyMMdd";
		public const string TimeFormat = "yyyyMMdd hh:MM:ss";

		public static IBSocket SendIf(this IBSocket socket, ServerVersions minVersion, Action<IBSocket> handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			if (socket.ServerVersion >= minVersion)
				handler(socket);

			return socket;
		}

		public static IBSocket SendSecurityType(this IBSocket socket, SecurityTypes? securityType)
		{
			if (securityType == null)
				return socket.Send(string.Empty);

			switch (securityType)
			{
				case SecurityTypes.Stock:
					return socket.Send("STK");
				case SecurityTypes.Future:
					return socket.Send("FUT");
				case SecurityTypes.Option:
					return socket.Send("OPT");
				case SecurityTypes.Index:
					return socket.Send("IND");
				case SecurityTypes.Currency:
					return socket.Send("CASH");
				case SecurityTypes.Bond:
					return socket.Send("BOND");
				case SecurityTypes.Warrant:
					return socket.Send("WAR");
				case SecurityTypes.Forward:
				case SecurityTypes.Swap:
					throw new NotSupportedException(LocalizedStrings.Str2499Params.Put(securityType));
				default:
					throw new ArgumentOutOfRangeException("securityType");
			}
		}

		public static IBSocket SendOptionType(this IBSocket socket, OptionTypes? type)
		{
			return socket.Send(type == null
				            ? string.Empty
							: type == OptionTypes.Call ? "C" : "P");
		}

		public static IBSocket SendBoardCode(this IBSocket socket, string boardCode)
		{
			return socket.Send(boardCode);
		}

		public static IBSocket SendPrimaryExchange(this IBSocket socket, SecurityMessage security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (socket.ServerVersion < ServerVersions.V14)
				return socket;

			return socket.SendBoardCode(security.GetRoutingBoard());
		}

		public static IBSocket SendCurrency(this IBSocket socket, CurrencyTypes? currency)
		{
			return socket.Send((currency ?? CurrencyTypes.USD).ToString());
		}

		public static IBSocket SendSecurityCode(this IBSocket socket, string code)
		{
			return socket.ServerVersion < ServerVersions.V2 ? socket : socket.Send(code);
		}

		public static IBSocket SendIncludeExpired(this IBSocket socket, DateTimeOffset? expiryDate)
		{
			if (socket.ServerVersion < ServerVersions.V31)
				return socket;

			return socket.Send((expiryDate != null && !(expiryDate < DateTimeOffset.Now)));
		}

		public static IBSocket SendContractId(this IBSocket socket, SecurityId security)
		{
			return socket.Send(security.InteractiveBrokers);
		}

		public static IBSocket SendSecurityId(this IBSocket socket, SecurityId id)
		{
			if (socket.ServerVersion < ServerVersions.V45)
				return socket;

			if (!id.Cusip.IsEmpty())
			{
				socket.Send("CUSIP");
				socket.Send(id.Cusip);
			}
			else if (!id.Isin.IsEmpty())
			{
				socket.Send("ISIN");
				socket.Send(id.Isin);
			}
			else if (!id.Sedol.IsEmpty())
			{
				socket.Send("SEDOL");
				socket.Send(id.Sedol);
			}
			else if (!id.Ric.IsEmpty())
			{
				socket.Send("RIC");
				socket.Send(id.Ric);
			}
			else
			{
				socket.Send(string.Empty);
				socket.Send(string.Empty);
			}

			return socket;
		}

		public static IBSocket SendSecurity(this IBSocket socket, SecurityMessage security, bool sendPrimExchange = true)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var multiplier = security.Multiplier;

			socket
				.Send(security.Name)
				.SendSecurityType(security.SecurityType)
				.Send(security.ExpiryDate, _expiryFormat)
				.Send(security.Strike)
				.SendOptionType(security.OptionType)
				.SendIf(ServerVersions.V15, s => s.Send(multiplier == 1 ? string.Empty : multiplier.To<string>()))
				.SendBoardCode(security.SecurityId.BoardCode);

			if (sendPrimExchange)
				socket.SendPrimaryExchange(security);

			return socket
				.SendCurrency(security.Currency)
				.SendSecurityCode(security.SecurityId.SecurityCode);
		}

		public static IBSocket SendSide(this IBSocket socket, Sides? side)
		{
			if (side == null)
				return socket.Send(string.Empty);

			switch (side.Value)
			{
				case Sides.Buy:
					return socket.Send("BUY");
				case Sides.Sell:
					return socket.Send("SELL");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static IBSocket SendOrderType(this IBSocket socket, OrderTypes orderType, IBOrderCondition.ExtendedOrderTypes? extendedOrderType)
		{
			switch (orderType)
			{
				case OrderTypes.Limit:
					return socket.Send("LMT");
				case OrderTypes.Market:
					return socket.Send("MKT");
				case OrderTypes.Conditional:
				{
					if (extendedOrderType == null)
						return socket.Send("NONE");

					switch (extendedOrderType)
					{
						case IBOrderCondition.ExtendedOrderTypes.MarketOnClose:
							return socket.Send("MOC");
						case IBOrderCondition.ExtendedOrderTypes.LimitOnClose:
							return socket.Send("LMTCLS");
						case IBOrderCondition.ExtendedOrderTypes.PeggedToMarket:
							return socket.Send("PEGMKT");
						case IBOrderCondition.ExtendedOrderTypes.Stop:
							return socket.Send("STP");
						case IBOrderCondition.ExtendedOrderTypes.StopLimit:
							return socket.Send("STP LMT");
						case IBOrderCondition.ExtendedOrderTypes.TrailingStop:
							return socket.Send("TRAIL");
						case IBOrderCondition.ExtendedOrderTypes.Relative:
							return socket.Send("REL");
						case IBOrderCondition.ExtendedOrderTypes.VolumeWeightedAveragePrice:
							return socket.Send("VWAP");
						case IBOrderCondition.ExtendedOrderTypes.TrailingStopLimit:
							return socket.Send("TRAILLIMIT");
						case IBOrderCondition.ExtendedOrderTypes.Volatility:
							return socket.Send("VOL");
						case IBOrderCondition.ExtendedOrderTypes.Empty:
							return socket.Send("");
						case IBOrderCondition.ExtendedOrderTypes.Default:
							return socket.Send("Default");
						case IBOrderCondition.ExtendedOrderTypes.Scale:
							return socket.Send("SCALE");
						case IBOrderCondition.ExtendedOrderTypes.MarketIfTouched:
							return socket.Send("MIT");
						case IBOrderCondition.ExtendedOrderTypes.LimitIfTouched:
							return socket.Send("LIT");
						default:
							throw new ArgumentOutOfRangeException("extendedOrderType", extendedOrderType, LocalizedStrings.Str2500);
					}
				}
				default:
					throw new ArgumentOutOfRangeException("orderType", orderType, LocalizedStrings.Str1600);
			}
		}

		public static IBSocket SendOrderExpiration(this IBSocket socket, OrderRegisterMessage msg)
		{
			if (msg == null)
				throw new ArgumentNullException("msg");

			if (msg.OrderType != OrderTypes.Conditional)
			{
				switch (msg.TimeInForce)
				{
					case TimeInForce.PutInQueue:
					case null:
					{
						if (msg.TillDate == null || msg.TillDate == DateTimeOffset.MaxValue)
							return socket.Send("GTC");
						else if (msg.TillDate != DateTimeOffset.Now.Date)
							return socket.Send("GTD");
						else
							return socket.Send("DAY");
					}
					case TimeInForce.MatchOrCancel:
						return socket.Send("FOK");
					case TimeInForce.CancelBalance:
						return socket.Send("IOC");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else if (msg.OrderType == OrderTypes.Conditional)
			{
				var ibCon = (IBOrderCondition)msg.Condition;

				return socket.Send(ibCon.IsMarketOnOpen ? "OPG" : "DAY");
			}
			else
				throw new ArgumentException(LocalizedStrings.Str2501Params.Put(msg.Type), "msg");
		}

		public static IBSocket SendFinancialAdvisor(this IBSocket socket, IBOrderCondition.FinancialAdvisorAllocations? allocation)
		{
			if (allocation == null)
				return socket.Send(string.Empty);

			switch (allocation.Value)
			{
				case IBOrderCondition.FinancialAdvisorAllocations.PercentChange:
					return socket.Send("PctChange");
				case IBOrderCondition.FinancialAdvisorAllocations.AvailableEquity:
					return socket.Send("AvailableEquity");
				case IBOrderCondition.FinancialAdvisorAllocations.NetLiquidity:
					return socket.Send("NetLiq");
				case IBOrderCondition.FinancialAdvisorAllocations.EqualQuantity:
					return socket.Send("EqualQuantity");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static IBSocket SendAgent(this IBSocket socket, IBOrderCondition.AgentDescriptions? description)
		{
			if (description == null)
				return socket.Send(string.Empty);

			switch (description.Value)
			{
				case IBOrderCondition.AgentDescriptions.Individual:
					return socket.Send("I");
				case IBOrderCondition.AgentDescriptions.Agency:
					return socket.Send("A");
				case IBOrderCondition.AgentDescriptions.AgentOtherMember:
					return socket.Send("W");
				case IBOrderCondition.AgentDescriptions.IndividualPTIA:
					return socket.Send("J");
				case IBOrderCondition.AgentDescriptions.AgencyPTIA:
					return socket.Send("U");
				case IBOrderCondition.AgentDescriptions.AgentOtherMemberPTIA:
					return socket.Send("M");
				case IBOrderCondition.AgentDescriptions.IndividualPT:
					return socket.Send("K");
				case IBOrderCondition.AgentDescriptions.AgencyPT:
					return socket.Send("Y");
				case IBOrderCondition.AgentDescriptions.AgentOtherMemberPT:
					return socket.Send("N");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static IBSocket SendDeltaNeutral(this IBSocket socket, IBOrderCondition condition)
		{
			if (condition == null)
				throw new ArgumentNullException("condition");

			var volatility = condition.Volatility;

			if (socket.ServerVersion < ServerVersions.V28)
			{
				return socket.Send(volatility.OrderType == OrderTypes.Market);
			}
			else
			{
				socket.SendOrderType(volatility.OrderType, volatility.ExtendedOrderType);
				socket.Send(volatility.StopPrice);

				if (volatility.ExtendedOrderType != IBOrderCondition.ExtendedOrderTypes.Empty)
				{
					if (socket.ServerVersion >= ServerVersions.V58)
					{
						socket
							.Send(volatility.ConId)
							.Send(volatility.SettlingFirm)
							.SendPortfolio(volatility.ClearingPortfolio)
							.Send(volatility.ClearingIntent);
					}

					if (socket.ServerVersion >= ServerVersions.V66)
					{
						socket
							.Send(volatility.ShortSale.IsOpenOrClose)
							.Send(volatility.IsShortSale)
							.SendShortSale(volatility.ShortSale);
					}
				}

				return socket;
			}
		}

		public static IBSocket SendPortfolio(this IBSocket socket, string portfolioName, bool allowNull = true)
		{
			if (!portfolioName.IsEmpty())
				return socket.ServerVersion < ServerVersions.V9 ? socket : socket.Send(portfolioName);

			if (allowNull)
				return socket.Send(string.Empty);

			throw new ArgumentNullException("portfolioName");
		}

		public static IBSocket SendIntent(this IBSocket socket, IBOrderCondition.ClearingIntents? intent)
		{
			if (intent == null)
				return socket;

			switch (intent.Value)
			{
				case IBOrderCondition.ClearingIntents.Broker:
					return socket.Send("IB");
				case IBOrderCondition.ClearingIntents.Away:
					return socket.Send("Away");
				case IBOrderCondition.ClearingIntents.PostTradeAllocation:
					return socket.Send("PTA");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static IBSocket SendEndDate(this IBSocket socket, DateTimeOffset endDate)
		{
			if (socket.ServerVersion < ServerVersions.V20)
				return socket;

			//yyyymmdd hh:mm:ss tmz
			return socket.Send(endDate.ToUniversalTime().ToString("yyyyMMdd HH:mm:ss") + " GMT");
		}

		public static IBSocket SendTimeFrame(this IBSocket socket, IBTimeFrames timeFrame)
		{
			if (timeFrame == null)
				throw new ArgumentNullException("timeFrame");

			if (socket.ServerVersion < ServerVersions.V20)
				return socket;

			return socket.Send(timeFrame.Interval);
		}

		public static IBSocket SendLevel1Field(this IBSocket socket, Level1Fields field)
		{
			switch (field)
			{
				case CandleDataTypes.Trades:
					return socket.Send("TRADES");
				case CandleDataTypes.Midpoint:
					return socket.Send("MIDPOINT");
				case CandleDataTypes.Bid:
					return socket.Send("BID");
				case CandleDataTypes.Ask:
					return socket.Send("ASK");
				case CandleDataTypes.BidAsk:
					return socket.Send("BID_ASK");
				case CandleDataTypes.HistoricalVolatility:
					return socket.Send("HISTORICAL_VOLATILITY");
				case CandleDataTypes.ImpliedVolatility:
					return socket.Send("OPTION_IMPLIED_VOLATILITY");
				case CandleDataTypes.YieldAsk:
					return socket.Send("YIELD_ASK");
				case CandleDataTypes.YieldBid:
					return socket.Send("YIELD_BID");
				case CandleDataTypes.YieldBidAsk:
					return socket.Send("YIELD_BID_ASK");
				case CandleDataTypes.YieldLast:
					return socket.Send("YIELD_LAST");
				default:
					throw new ArgumentOutOfRangeException("field");
			}
		}

		public static IBSocket SendHedge(this IBSocket socket, IBOrderCondition condition)
		{
			if (condition == null)
				throw new ArgumentNullException("condition");

			if (socket.ServerVersion < ServerVersions.V54)
				return socket;

			if (condition.Hedge.Type == null)
				return socket.Send(string.Empty);
			else
			{
				switch (condition.Hedge.Type.Value)
				{
					case IBOrderCondition.HedgeTypes.Delta:
						socket.Send("D");
						break;
					case IBOrderCondition.HedgeTypes.Beta:
						socket.Send("B");
						break;
					case IBOrderCondition.HedgeTypes.FX:
						socket.Send("F");
						break;
					case IBOrderCondition.HedgeTypes.Pair:
						socket.Send("P");
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				return socket.Send(condition.Hedge.Param);
			}
		}

		public static IBSocket SendShortSale(this IBSocket socket, IBOrderCondition.ShortSaleCondition shortSale, bool extendCode = false)
		{
			socket.Send((int)shortSale.Slot);
			socket.Send(shortSale.Location);

			if (extendCode)
			{
				if (socket.ServerVersion >= ServerVersions.V51)
					socket.Send(shortSale.ExemptCode);
			}

			return socket;
		}

		public static IBSocket SendCombo(this IBSocket socket, WeightedIndexSecurity security, IBOrderCondition condition = null)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var innerSecurities = security.InnerSecurities.ToArray();

			socket.Send(innerSecurities.Length);

			foreach (var innerSecurity in innerSecurities)
			{
				var weight = security.Weights[innerSecurity];

				socket
					.SendContractId(innerSecurity.ToSecurityId())
					.Send((int)weight.Abs())
					.SendSide(weight >= 0 ? Sides.Buy : Sides.Sell)
					.SendBoardCode(innerSecurity.Board.Code);

				if (condition == null)
					continue;

				var shortSale = condition.Combo.ShortSales[innerSecurity.ToSecurityId()];

				socket
					.Send(shortSale.IsOpenOrClose)
					.SendShortSale(shortSale, true);
			}

			return socket;
		}

		public static IBSocket SendFundamental(this IBSocket socket, FundamentalReports report)
		{
			switch (report)
			{
				case FundamentalReports.Overview:
					return socket.Send("ReportSnapshot");
				case FundamentalReports.Statements:
					return socket.Send("ReportsFinStatements");
				case FundamentalReports.Summary:
					return socket.Send("ReportsFinSummary");
				case FundamentalReports.Ratio:
					return socket.Send("ReportRatios");
				case FundamentalReports.Estimaes:
					return socket.Send("RESC");
				case FundamentalReports.Calendar:
					return socket.Send("CalendarReport");
				default:
					throw new ArgumentOutOfRangeException("report");
			}
		}

		public static DateTimeOffset? ReadExpiry(this IBSocket socket)
		{
			return socket.ReadNullDateTime(_expiryFormat);
		}

		public static CurrencyTypes ReadCurrency(this IBSocket socket)
		{
			return socket.ReadStr().To<CurrencyTypes>();
		}

		public static string ReadLocalCode(this IBSocket socket, string secCode)
		{
			if (secCode.IsEmpty())
				throw new ArgumentNullException("secCode");

			var localCode = socket.ReadStr();

			if (localCode.IsEmpty())
				return secCode;

			return localCode;
		}

		public static string ReadBoardCode(this IBSocket socket)
		{
			return socket.ReadStr();
		}

		public static OptionTypes? ReadOptionType(this IBSocket socket)
		{
			var str = socket.ReadStr();
			return (str.IsEmpty() || str.Equals("?")) ? (OptionTypes?)null : (str == "C" ? OptionTypes.Call : OptionTypes.Put);
		}

		public static Sides? ReadTradeSide(this IBSocket socket)
		{
			switch (socket.ReadStr())
			{
				case "BOT":
					return Sides.Buy;
				case "SLD":
					return Sides.Sell;
				default:
					return null;
			}
		}

		public static SecurityTypes ReadSecurityType(this IBSocket socket)
		{
			var str = socket.ReadStr();

			switch (str)
			{
				case "STK":
				case "SLB": // short stock
					return SecurityTypes.Stock;
				case "FUT":
				case "ICU":
				case "ICS":
					return SecurityTypes.Future;
				case "FOP": // option on fut
				case "OPT":
				case "IOPT": // interactive option
					return SecurityTypes.Option;
				case "IND":
				case "BAG":
				case "BSK":
					return SecurityTypes.Index;
				case "CASH":
					return SecurityTypes.Currency;
				case "BOND":
				case "BILL":
				case "FIXED":
					return SecurityTypes.Bond;
				case "FUND":
					return SecurityTypes.Fund;
				case "WAR":
					return SecurityTypes.Warrant;
				case "CFD":
					return SecurityTypes.Cfd;
				case "FWD":
					return SecurityTypes.Forward;
				case "NEWS":
					return SecurityTypes.News;
				case "CMDTY":
					return SecurityTypes.Commodity;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2502Params.Put(str));
			}
		}

		public static decimal? ReadMultiplier(this IBSocket socket)
		{
			var str = socket.ReadStr();
			return str.IsEmpty() ? (decimal?)null : str.To<decimal>();
		}

		public static OrderStatus ReadOrderStatus(this IBSocket socket)
		{
			var str = socket.ReadStr();

			switch (str)
			{
				case "PendingSubmit":
					return OrderStatus.SentToServer;
				case "PendingCancel":
					return OrderStatus.SentToCanceled;
				case "PreSubmitted":
					return OrderStatus.ReceiveByServer;
				case "Submitted":
					return OrderStatus.Accepted;
				case "Cancelled":
					return OrderStatus.Cancelled;
				case "Filled":
					return OrderStatus.Matched;
				case "Inactive":
					return OrderStatus.GateError;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2503Params.Put(str));
			}
		}

		public static void ReadOrderType(this IBSocket socket, out OrderTypes type, out IBOrderCondition.ExtendedOrderTypes? extendedType)
		{
			var str = socket.ReadStr();

			switch (str.ToUpperInvariant())
			{
				case "LMT":
					type = OrderTypes.Limit;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Empty;
					break;
				case "MKT":
					type = OrderTypes.Market;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Empty;
					break;
				case "MOC":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.MarketOnClose;
					break;
				case "LMTCLS":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.LimitOnClose;
					break;
				case "PEGMKT":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.PeggedToMarket;
					break;
				case "STP":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Stop;
					break;
				case "STP LMT":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.StopLimit;
					break;
				case "TRAIL":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.TrailingStop;
					break;
				case "REL":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Relative;
					break;
				case "VWAP":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.VolumeWeightedAveragePrice;
					break;
				case "TRAILLIMIT":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.TrailingStopLimit;
					break;
				case "VOL":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Volatility;
					break;
				case "NONE":
					type = OrderTypes.Conditional;
					extendedType = null;
					break;
				case "":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Empty;
					break;
				case "Default":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Default;
					break;
				case "SCALE":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.Scale;
					break;
				case "MIT":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.MarketIfTouched;
					break;
				case "LIT":
					type = OrderTypes.Conditional;
					extendedType = IBOrderCondition.ExtendedOrderTypes.LimitIfTouched;
					break;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2504Params.Put(str));
			}
		}

		public static IBOrderCondition.VolatilityTimeFrames? ReadVolatilityType(this IBSocket socket)
		{
			var str = socket.ReadStr();

			if (str.IsEmpty())
				return null;

			var value = str.To<int>();

			if (value == 0)
				return null;

			return (IBOrderCondition.VolatilityTimeFrames)value;
		}

		public static IBOrderCondition.FinancialAdvisorAllocations? ReadFinancialAdvisor(this IBSocket socket)
		{
			var str = socket.ReadStr();

			if (str.IsEmpty())
				return null;

			switch (str)
			{
				case "PctChange":
					return IBOrderCondition.FinancialAdvisorAllocations.PercentChange;
				case "AvailableEquity":
					return IBOrderCondition.FinancialAdvisorAllocations.AvailableEquity;
				case "EqualQuantity":
					return IBOrderCondition.FinancialAdvisorAllocations.EqualQuantity;
				case "NetLiq":
					return IBOrderCondition.FinancialAdvisorAllocations.NetLiquidity;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2505Params.Put(str));
			}
		}

		public static Sides ReadOrderSide(this IBSocket socket)
		{
			var str = socket.ReadStr();

			switch (str)
			{
				case "BUY":
					return Sides.Buy;
				case "SELL":
					return Sides.Sell;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2506Params.Put(str));
			}
		}

		public static IBOrderCondition.AgentDescriptions? ReadAgent(this IBSocket socket)
		{
			var str = socket.ReadStr();

			if (str.IsEmpty())
				return null;

			switch (str)
			{
				case "I":
					return IBOrderCondition.AgentDescriptions.Individual;
				case "A":
					return IBOrderCondition.AgentDescriptions.Agency;
				case "W":
					return IBOrderCondition.AgentDescriptions.AgentOtherMember;
				case "J":
					return IBOrderCondition.AgentDescriptions.IndividualPTIA;
				case "U":
					return IBOrderCondition.AgentDescriptions.AgencyPTIA;
				case "M":
					return IBOrderCondition.AgentDescriptions.AgentOtherMemberPTIA;
				case "K":
					return IBOrderCondition.AgentDescriptions.IndividualPT;
				case "Y":
					return IBOrderCondition.AgentDescriptions.AgencyPT;
				case "N":
					return IBOrderCondition.AgentDescriptions.AgentOtherMemberPT;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2507Params.Put(str));
			}
		}

		public static void ReadHedge(this IBSocket socket, IBOrderCondition ibCon)
		{
			var str = socket.ReadStr();

			if (str.IsEmpty())
			{
				ibCon.Hedge.Type = null;
				return;
			}

			switch (str)
			{
				case "D":
					ibCon.Hedge.Type = IBOrderCondition.HedgeTypes.Delta;
					break;
				case "B":
					ibCon.Hedge.Type = IBOrderCondition.HedgeTypes.Beta;
					break;
				case "F":
					ibCon.Hedge.Type = IBOrderCondition.HedgeTypes.FX;
					break;
				case "P":
					ibCon.Hedge.Type = IBOrderCondition.HedgeTypes.Pair;
					break;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2508Params.Put(str));
			}

			ibCon.Hedge.Param = socket.ReadStr();
		}

		public static void ReadSecurityId(this IBSocket socket, SecurityId securityId)
		{
			var count = socket.ReadInt();

			for (var i = 0; i < count; i++)
			{
				var idType = socket.ReadStr();
				switch (idType)
				{
					case "CUSIP":
						securityId.Cusip = socket.ReadStr();
						break;
					case "ISIN":
						securityId.Isin = socket.ReadStr();
						break;
					case "SEDOL":
						securityId.Sedol = socket.ReadStr();
						break;
					case "RIC":
						securityId.Ric = socket.ReadStr();
						break;
					default:
						throw new InvalidOperationException(LocalizedStrings.Str2509Params.Put(idType));
				}
			}
		}

		public static IBOrderCondition.ClearingIntents ReadIntent(this IBSocket socket)
		{
			var str = socket.ReadStr();

			switch (str.ToUpperInvariant())
			{
				case "IB":
					return IBOrderCondition.ClearingIntents.Broker;
				case "AWAY":
					return IBOrderCondition.ClearingIntents.Away;
				case "PTA":
					return IBOrderCondition.ClearingIntents.PostTradeAllocation;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2510Params.Put(str));
			}
		}
	}
}