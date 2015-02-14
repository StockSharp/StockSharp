namespace StockSharp.Transaq.Native
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Transaq.Native.Commands;
	using StockSharp.Transaq.Native.Responses;

	using ConnectMessage = StockSharp.Transaq.Native.Commands.ConnectMessage;
	using DisconnectMessage = StockSharp.Transaq.Native.Commands.DisconnectMessage;

	static class XmlSerializeHelper
	{
		private static readonly SynchronizedDictionary<string, Func<XElement, BaseResponse>> _deserializers =
			new SynchronizedDictionary<string, Func<XElement, BaseResponse>>();

		private static readonly SynchronizedDictionary<Type, Action<BaseCommandMessage, XElement>> _serializers =
			new SynchronizedDictionary<Type, Action<BaseCommandMessage, XElement>>();

		static XmlSerializeHelper()
		{
			AddSerializer<ConnectMessage>(SerializeConnect);
			AddSerializer<DisconnectMessage>(SerializeDefault);
			AddSerializer<RequestHistoryDataMessage>(SerializeRequestHistoryData);
			AddSerializer<ServerStatusMessage>(SerializeDefault);
			AddSerializer<RequestSecuritiesMessage>(SerializeDefault);
			AddSerializer<SubscribeMessage>(SerializeSubscribe);
			AddSerializer<UnsubscribeMessage>(SerializeUnsubscribe);
			AddSerializer<NewOrderMessage>(SerializeNewOrder);
			AddSerializer<NewCondOrderMessage>(SerializeNewCondOrder);
			AddSerializer<NewStopOrderMessage>(SerializeNewStopOrder);
			AddSerializer<NewRepoOrderMessage>(SerializeNewRepoOrder);
			AddSerializer<NewMRepoOrderMessage>(SerializeNewMRepoOrder);
			AddSerializer<NewRpsOrderMessage>(SerializeNewRpsOrder);
			AddSerializer<CancelOrderMessage>(SerializeCancelOrder);
			AddSerializer<CancelStopOrderMessage>(SerializeCancelOrder);
			AddSerializer<CancelNegDealMessage>(SerializeCancelOrder);
			AddSerializer<CancelReportMessage>(SerializeCancelOrder);
			AddSerializer<RequestFortsPositionsMessage>(SerializeRequestFortsPositions);
			AddSerializer<RequestClientLimitsMessage>(SerializeRequestClientLimits);
			AddSerializer<RequestMarketsMessage>(SerializeDefault);
			AddSerializer<RequestServTimeDifferenceMessage>(SerializeDefault);
			AddSerializer<RequestLeverageControlMessage>(SerializeRequestLeverageControl);
			AddSerializer<ChangePassMessage>(SerializeChangePass);
			AddSerializer<SubscribeTicksMessage>(SerializeSubscribeTicks);
			AddSerializer<RequestConnectorVersionMessage>(SerializeDefault);
			AddSerializer<RequestSecuritiesInfoMessage>(SerializeRequestSecuritiesInfo);
			AddSerializer<RequestMaxBuySellTPlusMessage>(SerializeRequestMaxBuySellTPlus);
			AddSerializer<MoveOrderMessage>(SerializeMoveOrder);
			AddSerializer<RequestServerIdMessage>(SerializeDefault);
			AddSerializer<RequestOldNewsMessage>(SerializeRequestOldNews);
			AddSerializer<RequestNewsBodyMessage>(SerializeRequestNewsBody);
			AddSerializer<RequestPortfolioTPlusMessage>(SerializeRequestPortfolioTPlus);

			_deserializers.Add("result", DeserializeBaseMessage);
			_deserializers.Add("candlekinds", DeserializeCandleKinds);
			_deserializers.Add("markets", DeserializeMarkets);
			_deserializers.Add("securities", DeserializeSecurities);
			_deserializers.Add("client", DeserializeClient);
			_deserializers.Add("positions", DeserializePositions);
			_deserializers.Add("server_status", DeserializeServerStatus);
			_deserializers.Add("overnight", DeserializeOvernight);
			_deserializers.Add("candles", DeserializeCandles);
			_deserializers.Add("error", DeserializeError);
			_deserializers.Add("connector_version", DeserializeConnectorVersion);
			_deserializers.Add("sec_info", DeserializeSecInfo);
			_deserializers.Add("sec_info_upd", DeserializeSecInfo);
			_deserializers.Add("current_server", DeserializeCurrentServer);
			_deserializers.Add("news_header", DeserializeNewsHeader);
			_deserializers.Add("news_body", DeserializeNewsBody);
			_deserializers.Add("ticks", DeserializeTicks);
			_deserializers.Add("alltrades", DeserializeAllTrades);
			_deserializers.Add("quotes", DeserializeQuotes);
			_deserializers.Add("marketord", DeserializeMarketOrd);
			_deserializers.Add("leverage_control", DeserializeLeverageControl);
			_deserializers.Add("quotations", DeserializeQuotations);
			_deserializers.Add("trades", DeserializeTrades);
			_deserializers.Add("clientlimits", DeserializeClientLimits);
			_deserializers.Add("orders", DeserializeOrders);
			_deserializers.Add("boards", DeserializeBoards);
			_deserializers.Add("pits", DeserializePits);
			_deserializers.Add("portfolio_tplus", DeserializePortfolioTPlus);
			_deserializers.Add("portfolio_mct", DeserializePortfolioMct);
			_deserializers.Add("max_buy_sell_tplus", DeserializeMaxBuySellTPlus);
			_deserializers.Add("messages", DeserializeMessages);
		}

		private static void AddSerializer<T>(Action<T, XElement> handler)
			where T : BaseCommandMessage
		{
			_serializers.Add(typeof(T), (c, e) => handler((T)c, e));
		}

		private static Func<DateTime> _getNow = () => TimeHelper.Now; 

		public static Func<DateTime> GetNow
		{
			get
			{
				if (_getNow == null)
					throw new InvalidOperationException();

				return _getNow;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_getNow = value;
			}
		}

		public static string Serialize(BaseCommandMessage message)
		{
			var xCommand = new XElement("command");
			xCommand.Add(new XAttribute("id", message.Id));

			var type = message.GetType();

			var serializer = _serializers.TryGetValue(type);

			if (serializer == null)
				throw new NotSupportedException("unknown command: " + type);

			serializer.Invoke(message, xCommand);

			return xCommand.ToString();
		}

		public static BaseResponse Deserialize(string xmlString)
		{
			var xElement = XElement.Parse(xmlString);

			var name = xElement.Name;

			var deserializer = _deserializers.TryGetValue(name.ToString());

			if (deserializer == null)
				throw new NotSupportedException("unknown element: " + name);

			return deserializer.Invoke(xElement);
		}

		private static void SerializeConnect(ConnectMessage message, XElement rootElement)
		{
			rootElement.Add(new XElement("login", message.Login));
			rootElement.Add(new XElement("password", message.Password));
			rootElement.Add(new XElement("host", message.EndPoint.GetHost()));
			rootElement.Add(new XElement("port", message.EndPoint.GetPort()));

			if (!message.LogsDir.IsNull())
				rootElement.Add(new XElement("logsdir", message.LogsDir));

			rootElement.Add(new XElement("loglevel", (int)message.LogLevel));
			rootElement.Add(new XElement("autopos", message.Autopos.ToMyString()));
			rootElement.Add(new XElement("micex_registers", message.MicexRegisters.ToMyString()));
			rootElement.Add(new XElement("milliseconds", message.Milliseconds.ToMyString()));
			rootElement.Add(new XElement("utc_time", message.Utc.ToMyString()));
			rootElement.Add(new XElement("notes_file", message.NotesFile));

			if (message.Proxy != null)
			{
				var proxyType = string.Empty;

				switch (message.Proxy.Type)
				{
					case ProxyTypes.Http:
						proxyType = "HTTP-CONNECT";
						break;
					case ProxyTypes.Socks4:
						proxyType = "SOCKS4";
						break;
					case ProxyTypes.Socks5:
						proxyType = "SOCKS5";
						break;
				}

				var proxyElement = new XElement("proxy",
												new XAttribute("type", proxyType),
												new XAttribute("addr", message.Proxy.Address.GetHost()),
												new XAttribute("port", message.Proxy.Address.GetPort())
					);

				if (!message.Proxy.Login.IsEmpty())
				{
					proxyElement.Add(new XAttribute("login", message.Proxy.Login));
					proxyElement.Add(new XAttribute("password", message.Proxy.Password));
				}

				rootElement.Add(proxyElement);
			}

			if (message.RqDelay.HasValue)
				rootElement.Add(new XElement("rqdelay", message.RqDelay));

			if (message.SessionTimeout.HasValue)
				rootElement.Add(new XElement("session_timeout", message.SessionTimeout));

			if (message.RequestTimeout.HasValue)
				rootElement.Add(new XElement("request_timeout", message.RequestTimeout));
		}

		private static void SerializeRequestHistoryData(RequestHistoryDataMessage message, XElement rootElement)
		{
			rootElement.Add(
				new XAttribute("period", message.Period),
				new XAttribute("count", message.Count),
				new XAttribute("reset", message.Reset.ToMyString()));

			if (message.SecId != 0)
			{
				rootElement.Add(new XAttribute("secid", message.SecId));
			}
			else
			{
				rootElement.Add(new XElement("security",
					new XElement("seccode", message.SecCode),
					new XElement("board", message.Board)));
			}
		}

		private static void SerializeSubscribe(SubscribeMessage message, XElement rootElement)
		{
			Action<string, Func<IEnumerable<SecurityId>>> action = (s, secids) =>
			{
				var ids = secids().ToArray();

				if (ids.IsEmpty())
					return;

				var el = new XElement(s);

				foreach (var id in ids)
				{
					if (id.Native != null)
						el.Add(new XElement("secid", id.Native));
					else
					{
						el.Add(new XElement("security",
							new XElement("seccode", id.SecurityCode),
							new XElement("board", id.BoardCode)));
					}
				}

				rootElement.Add(el);
			};

			action("alltrades", () => message.AllTrades);
			action("quotations", () => message.Quotations);
			action("quotes", () => message.Quotes);
		}

		private static void SerializeUnsubscribe(UnsubscribeMessage message, XElement rootElement)
		{
			SerializeSubscribe(message, rootElement);
		}

		private static void SerializeNewOrder(NewOrderMessage message, XElement rootElement)
		{
			if (message.SecId != 0)
				rootElement.Add(new XElement("secid", message.SecId));
			else
			{
				rootElement.Add(new XElement("security",
					new XElement("seccode", message.SecCode),
					new XElement("board", message.Board)));	
			}

			rootElement.Add(new XElement("client", message.Client));
			rootElement.Add(new XElement("price", message.Price));
			rootElement.Add(new XElement("hidden", message.Hidden));
			rootElement.Add(new XElement("quantity", message.Quantity));
			rootElement.Add(new XElement("buysell", message.BuySell));
			
			if (message.ByMarket)
				rootElement.Add(new XElement("bymarket"));
			
			rootElement.Add(new XElement("brokerref", message.BrokerRef));
			rootElement.Add(new XElement("unfilled", message.Unfilled.ToString()));
			
			if (message.UseCredit)
				rootElement.Add(new XElement("usecredit"));
		
			if (message.NoSplit)
				rootElement.Add(new XElement("nosplit"));

			if (message.ExpDate.HasValue)
				rootElement.Add(new XElement("expdate", message.ExpDate.Value.ToMyString()));
		}

		private static void SerializeNewCondOrder(NewCondOrderMessage message, XElement rootElement)
		{
			if (message.SecId != 0)
				rootElement.Add(new XElement("secid", message.SecId));
			else
			{
				rootElement.Add(new XElement("security",
					new XElement("seccode", message.SecCode),
					new XElement("board", message.Board)));
			}

			rootElement.Add(new XElement("client", message.Client));
			rootElement.Add(new XElement("price", message.Price));
			rootElement.Add(new XElement("hidden", message.Hidden));
			rootElement.Add(new XElement("quantity", message.Quantity));
			rootElement.Add(new XElement("buysell", message.BuySell));
			
			if (message.ByMarket)
				rootElement.Add(new XElement("bymarket"));
		
			rootElement.Add(new XElement("brokerref", message.BrokerRef));
			rootElement.Add(new XElement("cond_type", message.CondType));
			rootElement.Add(new XElement("cond_value", message.CondValue));

			if (message.UseCredit)
				rootElement.Add(new XElement("usecredit"));
		
			if (message.NoSplit)
				rootElement.Add(new XElement("nosplit"));

			if (message.ExpDate.HasValue)
				rootElement.Add(new XElement("expdate", message.ExpDate.Value.ToMyString()));
			
			if (message.ValidAfterType == TransaqAlgoOrderValidTypes.Immediately)
				rootElement.Add(new XElement("validafter", 0));
			else if (message.ValidAfterType == TransaqAlgoOrderValidTypes.Date && message.ValidAfter.HasValue)
				rootElement.Add(new XElement("validafter", message.ValidAfter.Value.ToMyString()));

			if (message.ValidBeforeType == TransaqAlgoOrderValidTypes.Immediately)
				rootElement.Add(new XElement("validbefore", 0));
			else if (message.ValidBeforeType == TransaqAlgoOrderValidTypes.Date && message.ValidBefore.HasValue)
				rootElement.Add(new XElement("validbefore", message.ValidBefore.Value.ToMyString()));
			else if (message.ValidBeforeType == TransaqAlgoOrderValidTypes.TillCancelled)
				rootElement.Add(new XElement("validbefore", "till_canceled"));
		}

		private static void SerializeNewStopOrder(NewStopOrderMessage message, XElement rootElement)
		{
			if (message.SecId != 0)
				rootElement.Add(new XElement("secid", message.SecId));
			else
			{
				rootElement.Add(new XElement("security",
					new XElement("seccode", message.SecCode),
					new XElement("board", message.Board)));
			}

			rootElement.Add(new XElement("client", message.Client));
			rootElement.Add(new XElement("buysell", message.BuySell));
			rootElement.Add(new XElement("linkedorderno", message.LinkedOrderNo));

			if (message.ValidFor.HasValue)
				rootElement.Add(new XElement("validfor", message.ValidFor.Value.ToMyString()));
		
			if (message.ExpDate.HasValue)
				rootElement.Add(new XElement("expdate", message.ExpDate.Value.ToMyString()));

			Action<NewStopOrderElement, string> action = (e, name) =>
			{
				var element = new XElement(name);
				
				element.Add(new XElement("activationprice", e.ActivationPrice));

				if (e.OrderPrice != null)
					element.Add(new XElement("orderprice", e.OrderPrice)); // + (e.IsOrderPriceInPercents ? "%" : "")));

				if (e.ByMarket != null && e.ByMarket.Value)
					element.Add(new XElement("bymarket"));

				element.Add(new XElement("quantity", e.Quantity)); // + (e.IsQuantityInPercents ? "%" : "")));
				
				if (e.UseCredit != null)
					element.Add(new XElement("usecredit"));
				
				if (e.GuardTime.HasValue)
					element.Add(new XElement("guardtime", e.GuardTime));

				element.Add(new XElement("brokerref", e.BrokerRef));
				
				if (e.Correction != null)
					element.Add(new XElement("correction", e.Correction)); // + (e.IsCorrectionInPercents ? "%" : "")));
				
				if (e.Spread != null)
					element.Add(new XElement("spread", e.Spread)); // + (e.IsSpreadInPercents ? "%" : "")));

				rootElement.Add(element);
			};

			if (message.StopLoss != null)
				action(message.StopLoss, "stoploss");
			
			if (message.TakeProfit != null)
				action(message.TakeProfit, "takeprofit");
		}

		private static void SerializeNewRpsOrder(NewRpsOrderMessage message, XElement rootElement)
		{
			if (message.SecId != 0)
				rootElement.Add(new XElement("secid", message.SecId));
			else
			{
				rootElement.Add(new XElement("security",
					new XElement("seccode", message.SecCode),
					new XElement("board", message.Board)));
			}

			rootElement.Add(new XElement("client", message.Client));
			rootElement.Add(new XElement("buysell", message.BuySell));

			rootElement.Add(new XElement("cpfirmid", message.CpFirmId));
			rootElement.Add(new XElement("matchref", message.MatchRef));
			rootElement.Add(new XElement("brokerref", message.BrokerRef));
			rootElement.Add(new XElement("price", message.Price));
			rootElement.Add(new XElement("quantity", message.Quantity));
			rootElement.Add(new XElement("settlecode", message.SettleCode));

			if (message.RefundRate != null)
				rootElement.Add(new XElement("refundrate", message.RefundRate));
		}

		private static void SerializeNewRepoOrder(NewRepoOrderMessage message, XElement rootElement)
		{
			SerializeNewRpsOrder(message, rootElement);

			if (message.Rate != null)
				rootElement.Add(new XElement("reporate", message.Rate));
		}

		private static void SerializeNewMRepoOrder(NewMRepoOrderMessage message, XElement rootElement)
		{
			SerializeNewRepoOrder(message, rootElement);

			rootElement.Add(new XElement("value", message.Value));

			if (message.Term != null)
				rootElement.Add(new XElement("repoterm", message.Term));

			if (message.StartDiscount != null)
				rootElement.Add(new XElement("startdiscount", message.StartDiscount));

			if (message.LowerDiscount != null)
				rootElement.Add(new XElement("lowerdiscount", message.LowerDiscount));

			if (message.UpperDiscount != null)
				rootElement.Add(new XElement("upperdiscount", message.UpperDiscount));

			if (message.BlockSecurities != null)
				rootElement.Add(new XElement("blocksecurities", message.BlockSecurities.Value.ToYesNo()[0]));
		}

		private static void SerializeCancelOrder(CancelOrderMessage message, XElement rootElement)
		{
			rootElement.Add(new XElement("transactionid", message.TransactionId));
		}

		private static void SerializeRequestFortsPositions(RequestFortsPositionsMessage message, XElement rootElement)
		{
			rootElement.Add(new XAttribute("client", message.Client.IsEmpty() ? string.Empty : message.Client));
		}

		private static void SerializeRequestClientLimits(RequestClientLimitsMessage m, XElement rootElement)
		{
			SerializeRequestFortsPositions(m, rootElement);
		}

		private static void SerializeRequestLeverageControl(RequestLeverageControlMessage message, XElement rootElement)
		{
			rootElement.Add(new XAttribute("client", message.Client));

			foreach (var secId in message.SecIds)
			{
				if (secId.Native != null)
					rootElement.Add(new XElement("secid", secId.Native));
				else
				{
					rootElement.Add(new XElement("security",
							new XElement("seccode", secId.SecurityCode),
							new XElement("board", secId.BoardCode)));
				}
			}
		}

		private static void SerializeChangePass(ChangePassMessage message, XElement rootElement)
		{
			rootElement.Add(new XAttribute("oldpass", message.OldPass));
			rootElement.Add(new XAttribute("newpass", message.NewPass));
		}

		private static void SerializeSubscribeTicks(SubscribeTicksMessage message, XElement rootElement)
		{
			var isAttr = false;

			foreach (var item in message.Items)
			{
				if (item.SecId != 0)
				{
					isAttr = true;

					rootElement.Add(
						new XElement("security",
							new XAttribute("secid", item.SecId),
							new XAttribute("tradeno", item.TradeNo)));
				}
				else
				{
					rootElement.Add(
						new XElement("security",
							new XElement("seccode", item.SecCode),
							new XElement("board", item.Board),
							new XElement("tradeno", item.TradeNo)));
				}
			}

			if (isAttr)
				rootElement.Add(new XAttribute("filter", message.Filter.ToMyString()));
			else
				rootElement.Add(new XElement("filter", message.Filter.ToMyString()));
		}

		private static void SerializeRequestSecuritiesInfo(RequestSecuritiesInfoMessage message, XElement rootElement)
		{
			rootElement.Add(
				new XElement("security",
					new XElement("market", message.Market),
					new XElement("seccode", message.SecCode)));
		}

		private static void SerializeRequestMaxBuySellTPlus(RequestMaxBuySellTPlusMessage message, XElement rootElement)
		{
			rootElement.Add(
				new XElement("security",
					new XElement("market", message.Market),
					new XElement("seccode", message.SecCode)));
		}

		private static void SerializeMoveOrder(MoveOrderMessage message, XElement rootElement)
		{
			rootElement.Add(
				new XElement("transactionid", message.TransactionId),
				new XElement("price", message.Price),
				new XElement("moveflag", (int)message.MoveFlag),
				new XElement("quantity", message.Quantity));
		}

		private static void SerializeRequestOldNews(RequestOldNewsMessage message, XElement rootElement)
		{
			rootElement.Add(new XAttribute("count", message.Count));
		}

		private static void SerializeRequestNewsBody(RequestNewsBodyMessage message, XElement rootElement)
		{
			rootElement.Add(new XAttribute("news_id", message.NewsId));
		}

		private static void SerializeRequestPortfolioTPlus(RequestPortfolioTPlusMessage message, XElement rootElement)
		{
			rootElement.Add(new XAttribute("client", message.Client.IsEmpty() ? "" : message.Client));
		}

		private static BaseResponse DeserializeBaseMessage(XElement rootElement)
		{
			return new BaseResponse
			{
				Text = rootElement.Value,
				IsSuccess = rootElement.GetAttributeValue("success", true),
				Diff = rootElement.GetAttributeValue<int>("diff"),
				TransactionId = rootElement.GetAttributeValue<long>("transactionid")
			};
		}

		private static BaseResponse DeserializeCandleKinds(XElement rootElement)
		{
			return new CandleKindsResponse
			{
				Kinds = rootElement
					.Descendants("kind")
					.Select(node => new CandleKind
					{
						Id = node.GetElementValue<int>("id"),
						Period = node.GetElementValue<int>("period"),
						Name = node.GetElementValue("name")
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeMarkets(XElement rootElement)
		{
			return new MarketsResponse
			{
				Markets = rootElement
					.Descendants("market")
					.Select(node => new Market
					{
						Id = node.GetAttributeValue<int>("id"),
						Name = node.Value
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeSecurities(XElement rootElement)
		{
			var result = new SecuritiesResponse();

			var securities = new List<TransaqSecurity>();
			
			foreach (var node in rootElement.Descendants("security"))
			{
				var sec = new TransaqSecurity
				{
					SecId = node.GetAttributeValue<int>("secid"),
					Active = node.GetAttributeValue<bool>("active"),
					SecCode = node.GetElementValue("seccode"),
					Board = node.GetElementValue("board"),
					Market = node.GetElementValue("market"),
					ShortName = node.GetElementValue("shortname"),
					Decimals = node.GetElementValue("decimals"),
					MinStep = node.GetElementValue("minstep").To<decimal>(),
					LotSize = node.GetElementValue<int>("lotsize"),
					PointCost = node.GetElementValue("point_cost").To<decimal>()
				};

				var opmask = node.Element("opmask");
				if (opmask != null)
				{
					sec.OpMaskUseCredit = opmask.GetAttributeValue("usecredit").FromYesNo();
					sec.OpMaskByMarket = opmask.GetAttributeValue("bymarket").FromYesNo();
					sec.OpMaskNoSplit = opmask.GetAttributeValue("nosplit").FromYesNo();
					sec.OpMaskImmorCancel = opmask.GetAttributeValue("immorcancel").FromYesNo();
					sec.OpMaskCancelBalance = opmask.GetAttributeValue("cancelbalance").FromYesNo();
				}

				sec.Type = node.GetElementValue("sectype");

				// http://www.transaq.ru/forum/index.php?topic=2878.0
				sec.TimeZone = node.GetElementValue("sec_tz", string.Empty);

				securities.Add(sec);
			}

			result.Securities = securities;

			return result;
		}

		private static BaseResponse DeserializeClient(XElement rootElement)
		{
			return new ClientResponse
			{
				Id = rootElement.GetAttributeValue("id"),
				Remove = rootElement.GetAttributeValue<bool>("remove"),
				Type = rootElement.GetElementValue("type").To<ClientTypes>(),
				Currency = rootElement.GetElementValue("currency"),
				MlIntraDay = rootElement.GetElementValueNullable<decimal>("ml_intraday"),
				MlOverNight = rootElement.GetElementValueNullable<decimal>("ml_overnight"),
				MlRestrict = rootElement.GetElementValueNullable<decimal>("ml_restrict"),
				MlCall = rootElement.GetElementValueNullable<decimal>("ml_call"),
				MlClose = rootElement.GetElementValueNullable<decimal>("ml_close")
			};
		}

		private static BaseResponse DeserializePositions(XElement rootElement)
		{
			return new PositionsResponse
			{
				MoneyPositions = rootElement.Elements("money_position").Select(node => new MoneyPosition
				{
					Client = node.GetElementValue("client"),
					Markets = node
						.Descendants("markets")
						.Select(m => new Market { Id = m.GetElementValue<int>("market") })
						.ToArray(),
					Register = node.GetElementValue("register", null, false),
					Asset = node.GetElementValue("asset"),
					ShortName = node.GetElementValue("shortname"),
					SaldoIn = node.GetElementValue("saldoin").To<decimal>(),
					Bought = node.GetElementValue("bought").To<decimal>(),
					Sold = node.GetElementValue("sold").To<decimal>(),
					Saldo = node.GetElementValue("saldo").To<decimal>(),
					OrdBuy = node.GetElementValue("ordbuy").To<decimal>(),
					OrdBuyCond = node.GetElementValue("ordbuycond").To<decimal>(),
					Commission = node.GetElementValue("comission").To<decimal>()
				}).ToArray(),
				SecPositions = rootElement.Elements("sec_position").Select(node => new SecPosition
				{
					Client = node.GetElementValue("client"),
					SecId = node.GetElementValue<int>("secid"),
					Market = node.GetElementValue<int>("market"),
					SecCode = node.GetElementValue("seccode"),
					Register = node.GetElementValue("register", null, false),
					ShortName = node.GetElementValue("shortname"),
					SaldoIn = node.GetElementValue<long>("saldoin"),
					SaldoMin = node.GetElementValue<long>("saldomin"),
					Bought = node.GetElementValue<long>("bought"),
					Sold = node.GetElementValue<long>("sold"),
					Saldo = node.GetElementValue<long>("saldo"),
					OrdBuy = node.GetElementValue<long>("ordbuy"),
					OrdSell = node.GetElementValue<long>("ordsell")
				}).ToArray(),
				FortsPositions = rootElement.Elements("forts_position").Select(node => new FortsPosition
				{
					Markets = node
						.Descendants("markets")
						.Select(m => new Market { Id = m.GetElementValue<int>("market") })
						.ToArray(),
					SecId = node.GetElementValue<int>("secid"),
					SecCode = node.GetElementValue("seccode"),
					Client = node.GetElementValue("client"),
					StartNet = node.GetElementValue<int>("startnet"),
					OpenBuys = node.GetElementValue<int>("openbuys"),
					OpenSells = node.GetElementValue<int>("opensells"),
					TotalNet = node.GetElementValue<int>("totalnet"),
					TodayBuy = node.GetElementValue<int>("todaybuy"),
					TodaySell = node.GetElementValue<int>("todaysell"),
					OptMargin = node.GetElementValue("optmargin").To<decimal>(),
					VarMargin = node.GetElementValue("varmargin").To<decimal>(),
					ExpirationPos = node.GetElementValue<long>("expirationpos"),
					UsedSellSpotLimit = node.GetElementValueNullable<decimal>("usedsellspotlimit"),
					SellSpotLimit = node.GetElementValueNullable<decimal>("sellspotlimit"),
					Netto = node.GetElementValueNullable<decimal>("netto"),
					Kgo = node.GetElementValueNullable<decimal>("kgo")
				}).ToArray(),
				FortsMoneys = rootElement.Elements("forts_money").Select(node => new FortsMoney
				{
					Markets = node
						.Descendants("markets")
						.Select(m => new Market { Id = m.GetElementValue<int>("market") })
						.ToArray(),
					Client = node.GetElementValue("client"),
					ShortName = node.GetElementValue("shortname"),
					Current = node.GetElementValue("current").To<decimal>(),
					Blocked = node.GetElementValue("blocked").To<decimal>(),
					Free = node.GetElementValue("free").To<decimal>(),
					VarMargin = node.GetElementValue("varmargin").To<decimal>()
				}).ToArray(),
				FortsCollateralses = rootElement.Elements("forts_collaterals").Select(node => new FortsCollaterals
				{
					Markets = node
						.Descendants("markets")
						.Select(m => new Market { Id = m.GetElementValue<int>("market") })
						.ToArray(),
					Client = node.GetElementValue("client"),
					ShortName = node.GetElementValue("shortname"),
					Current = node.GetElementValue("current").To<decimal>(),
					Blocked = node.GetElementValue("blocked").To<decimal>(),
					Free = node.GetElementValue("free").To<decimal>()
				}).ToArray(),
				SpotLimits = rootElement.Elements("spot_limit").Select(node => new SpotLimit
				{
					Markets = node
						.Descendants("markets")
						.Select(m => new Market { Id = m.GetElementValue<int>("market") })
						.ToArray(),
					Client = node.GetElementValue("client"),
					ShortName = node.GetElementValue("shortname"),
					BuyLimit = node.GetElementValue("buylimit").To<decimal>(),
					BuyLimitUsed = node.GetElementValue("buylimitused").To<decimal>()
				}).ToArray()
			};
		}

		private static BaseResponse DeserializeError(XElement rootElement)
		{
			return new BaseResponse
			{
				IsSuccess = false,
				Exception = new ApiException(rootElement.Value)
			};
		}

		private static BaseResponse DeserializeServerStatus(XElement rootElement)
		{
			return new ServerStatusResponse
			{
				Connected = rootElement.GetAttributeValue("connected", string.Empty),
				Recover = rootElement.GetAttributeValue("recover", string.Empty),
				TimeZone = rootElement.GetAttributeValue("server_tz", string.Empty),
				Text = rootElement.Value
			};
		}

		private static BaseResponse DeserializeOvernight(XElement rootElement)
		{
			return new OvernightResponse
			{
				Status = rootElement.GetAttributeValue<bool>("status")
			};
		}

		private static BaseResponse DeserializeCandles(XElement rootElement)
		{
			return new CandlesResponse
			{
				SecId = rootElement.GetAttributeValue<int>("secid"),
				Board = rootElement.GetAttributeValue("board"),
				SecCode = rootElement.GetAttributeValue("seccode"),
				Period = rootElement.GetAttributeValue<int>("period"),
				Status = (CandleResponseStatus)rootElement.GetAttributeValue<int>("status"),
				Candles = rootElement
					.Descendants("candle")
					.Select(node => new TransaqCandle
					{
						Date = node.GetAttributeValue("date").ToDate(GetNow()),
						Open = node.GetAttributeValue("open").To<decimal>(),
						High = node.GetAttributeValue("high").To<decimal>(),
						Low = node.GetAttributeValue("low").To<decimal>(),
						Close = node.GetAttributeValue("close").To<decimal>(),
						Volume = node.GetAttributeValue<int>("volume"),
						Oi = node.GetAttributeValue<int>("oi")
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeConnectorVersion(XElement rootElement)
		{
			return new ConnectorVersionResponse
			{
				Version = rootElement.Value
			};
		}

		private static BaseResponse DeserializeSecInfo(XElement rootElement)
		{
			return new SecInfoResponse
			{
				SecId = rootElement.GetElementValue<int>("secid"),
				SecCode = rootElement.GetElementValue("seccode"),
				Market = rootElement.GetElementValue<int>("market"),
				SecName = rootElement.GetElementValue("secname", null, false),
				PName = rootElement.GetElementValue("pname", null, false),
				MatDate = rootElement.GetElementValueNullable<DateTime>("mat_date", GetNow),
				ClearingPrice = rootElement.GetElementValueNullable<decimal>("clearing_price"),
				MinPrice = rootElement.GetElementValueNullable<decimal>("minprice"),
				MaxPrice = rootElement.GetElementValueNullable<decimal>("maxprice"),
				BuyDeposit = rootElement.GetElementValueNullable<decimal>("buy_deposit"),
				SellDeposit = rootElement.GetElementValueNullable<decimal>("sell_deposit"),
				BgoC = rootElement.GetElementValueNullable<decimal>("bgo_c"),
				BgoNC = rootElement.GetElementValueNullable<decimal>("bgo_nc"),
				BgoBuy = rootElement.GetElementValueNullable<decimal>("bgo_buy"),
				Accruedint = rootElement.GetElementValueNullable<decimal>("accruedint"),
				CouponValue = rootElement.GetElementValueNullable<decimal>("coupon_value"),
				CouponDate = rootElement.GetElementValueNullable<DateTime>("coupon_date", GetNow),
				CouponPeriod = rootElement.GetAttributeValue<int>("coupon_period"),
				FaceValue = rootElement.GetElementValueNullable<decimal>("facevalue"),
				PutCall = rootElement.GetElementValueNullable<SecInfoPutCalls>("put_call"),
				OptType = rootElement.GetElementValueNullable<SecInfoOptTypes>("opt_type"),
				LotVolume = rootElement.GetElementValueNullable<int>("lot_volume")
			};
		}

		private static BaseResponse DeserializeCurrentServer(XElement rootElement)
		{
			return new CurrentServerResponse
			{
				Id = rootElement.GetAttributeValue<int>("id")
			};
		}

		private static BaseResponse DeserializeNewsHeader(XElement rootElement)
		{
			return new NewsHeaderResponse
			{
				Id = rootElement.GetAttributeValue<int>("id"),
				TimeStamp = rootElement.GetElementValueNullable<DateTime>("time_stamp", GetNow),
				Text = rootElement.GetElementValue("source", null, false),
				Title = rootElement.GetElementValue("title", null, false)
			};
		}

		private static BaseResponse DeserializeNewsBody(XElement rootElement)
		{
			return new NewsBodyResponse
			{
				Id = rootElement.GetAttributeValue<int>("id"),
				Text = rootElement.GetElementValue("text")
			};
		}

		private static BaseResponse DeserializeTicks(XElement rootElement)
		{
			return new TicksResponse
			{
				Ticks = rootElement
					.Descendants("tick")
					.Select(node => new Tick
					{
						SecId = node.GetElementValue<int>("secid"),
						SecCode = node.GetElementValue("seccode"),
						Board = node.GetElementValue("board"),
						TradeNo = node.GetElementValue<long>("tradeno"),
						TradeTime = node.GetElementValue("tradetime").ToDate(GetNow()),
						Price = node.GetElementValue("price").To<decimal>(),
						Quantity = node.GetElementValue<int>("quantity"),
						Period = node.GetElementValueNullable<TicksPeriods>("period"),
						BuySell = node.GetElementValue<BuySells>("buysell"),
						OpenInterest = node.GetAttributeValue<int>("openinterest")
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeAllTrades(XElement rootElement)
		{
			return new AllTradesResponse
			{
				AllTrades = rootElement
					.Descendants("trade")
					.Select(node => new Tick
					{
						SecId = node.GetAttributeValue<int>("secid"),
						SecCode = node.GetElementValue("seccode"),
						Board = node.GetElementValue("board"),
						TradeNo = node.GetElementValue<long>("tradeno"),
						TradeTime = node.GetElementValue("time").ToDate(GetNow()),
						Price = node.GetElementValue("price").To<decimal>(),
						Quantity = node.GetElementValue<int>("quantity"),
						Period = node.GetElementValueNullable<TicksPeriods>("period"),
						BuySell = node.GetElementValue<BuySells>("buysell"),
						OpenInterest = node.GetElementValue<int>("openinterest")
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeQuotes(XElement rootElement)
		{
			return new QuotesResponse
			{
				Quotes = rootElement
					.Descendants("quote")
					.Select(node => new TransaqQuote
					{
						SecId = node.GetAttributeValue<int>("secid"),
						SecCode = node.GetElementValue("seccode"),
						Source = node.GetElementValue("source", string.Empty),
						Board = node.GetElementValue("board"),
						Price = node.GetElementValue("price").To<decimal>(),
						Yield = node.GetElementValue<int>("yield"),
						Buy = node.GetElementValueNullable<int>("buy"),
						Sell = node.GetElementValueNullable<int>("sell")
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeMarketOrd(XElement rootElement)
		{
			return new MarketOrdResponse
			{
				SecId = rootElement.GetElementValue<int>("secid"),
				SecCode = rootElement.GetElementValue("seccode"),
				Permit = rootElement.GetAttributeValue("permit").FromYesNo()
			};
		}

		private static BaseResponse DeserializeLeverageControl(XElement rootElement)
		{
			return new LeverageControlResponse
			{
				Client = rootElement.GetAttributeValue("client"),
				LeveragePlan = rootElement.GetAttributeValue("leverage_plan").To<decimal?>(),
				LeverageFact = rootElement.GetAttributeValue("leverage_fact").To<decimal?>(),
				Items = rootElement
					.Descendants("security")
					.Select(node => new LeverageControlSecurity
					{
						SecCode = node.GetElementValue("seccode"),
						Board = node.GetElementValue("board"),
						MaxBuy = node.GetAttributeValue<long>("maxbuy"),
						MaxSell = node.GetAttributeValue<long>("maxsell")
					})
					.ToArray(),
			};
		}


		private static BaseResponse DeserializeQuotations(XElement rootElement)
		{
			return new QuotationsResponse
			{
				Quotations = rootElement
					.Descendants("quotation")
					.Select(node => new Quotation
					{
						SecId = node.GetAttributeValue<int>("secid"),
						SecCode = node.GetElementValue("seccode"),
						Board = node.GetElementValue("board"),
						AccruedIntValue = node.GetElementValueNullable<decimal>("accrueedintValue"),
						Open = node.GetElementValueNullable<decimal>("open"),
						WAPrice = node.GetElementValueNullable<decimal>("waprice"),
						BestBidVolume = node.GetElementValueNullable<int>("biddepth"),
						BidsVolume = node.GetElementValueNullable<int>("biddeptht"),
						BidsCount = node.GetElementValueNullable<int>("numbids"),
						BestAskVolume = node.GetElementValueNullable<int>("offerdepth"),
						AsksVolume = node.GetElementValueNullable<int>("offerdeptht"),
						BestBidPrice = node.GetElementValueNullable<decimal>("bid"),
						BestAskPrice = node.GetElementValueNullable<decimal>("offer"),
						AsksCount = node.GetElementValueNullable<int>("numoffers"),
						TradesCount = node.GetElementValueNullable<int>("numtrades"),
						VolToday = node.GetElementValueNullable<int>("voltoday"),
						OpenInterest = node.GetElementValueNullable<int>("openpositions"),
						DeltaPositions = node.GetElementValueNullable<int>("deltapositions"),
						LastTradePrice = node.GetElementValueNullable<decimal>("last"),
						LastTradeVolume = node.GetElementValueNullable<int>("quantity"),
						LastTradeTime = node.GetElementValueNullable<DateTime>("time", GetNow),
						Change = node.GetElementValueNullable<decimal>("change"),
						PriceMinusPrevWAPrice = node.GetElementValueNullable<decimal>("priceminusprevwaprice"),
						ValToday = node.GetElementValueNullable<decimal>("valtoday"),
						Yield = node.GetElementValueNullable<decimal>("yield"),
						YieldAtWAPrice = node.GetElementValueNullable<decimal>("yieldatwaprice"),
						MarketPriceToday = node.GetElementValueNullable<decimal>("marketpricetoday"),
						HighBid = node.GetElementValueNullable<decimal>("highbid"),
						LowAsk = node.GetElementValueNullable<decimal>("lowoffer"),
						High = node.GetElementValueNullable<decimal>("high"),
						Low = node.GetElementValueNullable<decimal>("low"),
						ClosePrice = node.GetElementValueNullable<decimal>("closeprice"),
						CloseYield = node.GetElementValueNullable<decimal>("closeyield"),
						Status = node.GetElementValueNullable<TransaqSecurityStatus>("status"),
						SessionStatus = node.GetElementValue("status", null, false),
						BuyDeposit = node.GetElementValueNullable<decimal>("buydeposit"),
						SellDeposit = node.GetElementValueNullable<decimal>("selldeposit"),
						Volatility = node.GetElementValueNullable<decimal>("volatility"),
						TheoreticalPrice = node.GetElementValueNullable<decimal>("theoreticalprice"),
						BgoBuy = node.GetElementValueNullable<decimal>("bgo_buy"),
						PointCost = node.GetElementValueNullable<decimal>("point_cost"),
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeTrades(XElement rootElement)
		{
			return new TradesResponse
			{
				Trades = rootElement
					.Descendants("trade")
					.Select(node => new TransaqMyTrade
					{
						SecId = node.GetElementValue<int>("secid"),
						SecCode = node.GetElementValue("seccode"),
						TradeNo = node.GetElementValue<long>("tradeno"),
						OrderNo = node.GetElementValue<long>("orderno"),
						Board = node.GetElementValue("board"),
						Client = node.GetElementValue("client"),
						BuySell = node.GetElementValue<BuySells>("buysell"),
						Time = node.GetElementValue("time").ToDate(GetNow()),
						BrokerRef = node.GetElementValue("brokerref"),
						Value = node.GetElementValue("value").To<decimal>(),
						Commission = node.GetElementValueNullable<decimal>("commission"),
						Price = node.GetElementValue("price").To<decimal>(),
						Quantity = node.GetElementValue<int>("quantity"),
						Yield = node.GetElementValue("yield").To<decimal>(),
						AccrueEdint = node.GetElementValueNullable<decimal>("accrueedint"),
						TradeType = node.GetElementValue<TradeTypes>("tradetype"),
						SettleCode = node.GetElementValue("settlecode"),
						CurrentPos = node.GetElementValue<long>("currentpos")
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeClientLimits(XElement rootElement)
		{
			return new ClientLimitsResponse
			{
				Client = rootElement.GetAttributeValue("client"),
				CBPLimit = rootElement.GetElementValue("cbplimit").To<decimal>(),
				CBPlused = rootElement.GetElementValue("cbplused").To<decimal>(),
				CBPLPlanned = rootElement.GetElementValue("cbplplanned").To<decimal>(),
				FobVarMargin = rootElement.GetElementValue("fob_varmargin").To<decimal>(),
				Coverage = rootElement.GetElementValue("coverage").To<decimal>(),
				LiquidityC = rootElement.GetElementValue("liquidity_c").To<decimal>(),
				Profit = rootElement.GetElementValue("profit").To<decimal>(),
				MoneyCurrent = rootElement.GetElementValue("money_current").To<decimal>(),
				MoneyBlocked = rootElement.GetElementValue("money_blocked").To<decimal>(),
				MoneyFree = rootElement.GetElementValue("money_free").To<decimal>(),
				OptionsPremium = rootElement.GetElementValue("options_premium").To<decimal>(),
				ExchangeFee = rootElement.GetElementValue("exchange_fee").To<decimal>(),
				FortsVarMargin = rootElement.GetElementValue("forts_varmargin").To<decimal>(),
				VarMargin = rootElement.GetElementValue("varmargin").To<decimal>(),
				PclMargin = rootElement.GetElementValue("pclmargin").To<decimal>(),
				OptionsVm = rootElement.GetElementValue("options_vm").To<decimal>(),
				SpotBuyLimit = rootElement.GetElementValue("spot_buy_limit").To<decimal>(),
				UsedStopBuyLimit = rootElement.GetElementValue("used_stop_buy_limit").To<decimal>(),
				CollatCurrent = rootElement.GetElementValue("collat_current").To<decimal>(),
				CollatBlocked = rootElement.GetElementValue("collat_blocked").To<decimal>(),
				CollatFree = rootElement.GetElementValue("collat_free").To<decimal>()
			};
		}

		private static BaseResponse DeserializeOrders(XElement rootElement)
		{
			return new OrdersResponse
			{
				Orders = rootElement
					.Descendants("order")
					.Select(orderElement => new TransaqOrder
					{
						TransactionId = orderElement.GetAttributeValue<int>("transactionid"),
						OrderNo = orderElement.GetElementValue<long>("orderno"),
						Board = orderElement.GetElementValue("board"),
						SecId = orderElement.GetElementValue<int>("secid"),
						SecCode = orderElement.GetElementValue("seccode"),
						Client = orderElement.GetElementValue("client"),
						Status = orderElement.GetElementValue<TransaqOrderStatus>("status"),
						BuySell = orderElement.GetElementValue<BuySells>("buysell"),
						Time = orderElement.GetElementValueNullable<DateTime>("time", GetNow),
						ExpDate = orderElement.GetElementValueNullable<DateTime>("expdate", GetNow),
						OriginOrderNo = orderElement.GetElementValueNullable<long>("origin_orderno"),
						AcceptTime = orderElement.GetElementValueNullable<DateTime>("accepttime", GetNow),
						BrokerRef = orderElement.GetElementValue("brokerref"),
						Value = orderElement.GetElementValue("value").To<decimal>(),
						AccruEdint = orderElement.GetElementValue("accruedint").To<decimal>(),
						SettleCode = orderElement.GetElementValue("settlecode"),
						Balance = orderElement.GetElementValue<int>("balance"),
						Price = orderElement.GetElementValue("price").To<decimal>(),
						Quantity = orderElement.GetElementValue<int>("quantity"),
						Hidden = orderElement.GetElementValue<int>("hidden"),
						Yield = orderElement.GetElementValue("yield").To<decimal>(),
						WithdrawTime = orderElement.GetElementValueNullable<DateTime>("withdrawtime", GetNow),
						ConditionType = orderElement.GetElementValue<TransaqAlgoOrderConditionTypes>("condition"),
						ConditionValue = orderElement.GetElementValueNullable<decimal>("conditionvalue"),
						ValidAfter = orderElement.GetElementValueNullable<DateTime>("validafter", GetNow),
						ValidBefore = orderElement.GetElementValueNullable<DateTime>("validbefore", GetNow),
						MaxCommission = orderElement.GetElementValue("maxcomission").To<decimal>(),
						Result = orderElement.GetElementValue("result")
					})
					.ToArray(),

				StopOrders = rootElement
					.Descendants("stoporder")
					.Select(sOrderElement =>
					{
						var stopOrder = new TransaqStopOrder
						{
							TransactionId = sOrderElement.GetAttributeValue<int>("transactionid"),
							ActiveOrderNo = sOrderElement.GetElementValueNullable<long>("activeorderno"),
							Board = sOrderElement.GetElementValue("board"),
							SecCode = sOrderElement.GetElementValue("seccode"),
							Client = sOrderElement.GetElementValue("client"),
							BuySell = sOrderElement.GetElementValue<BuySells>("buysell"),
							Canceller = sOrderElement.GetElementValue("canceller", null, false),
							AllTradeNo = sOrderElement.GetElementValueNullable<long>("alltradeno"),
							ValidBefore = sOrderElement.GetElementValueNullable<DateTime>("validbefore", GetNow),
							Author = sOrderElement.GetElementValue("author"),
							AcceptTime = sOrderElement.GetElementValue("accepttime").ToDate(GetNow()),
							LinkedOrderNo = sOrderElement.GetElementValueNullable<long>("linkedorderno"),
							ExpDate = sOrderElement.GetElementValueNullable<DateTime>("expdate", GetNow),
							Status = sOrderElement.GetElementValue<TransaqOrderStatus>("status")
						};

						var stopLossElement = sOrderElement.Element("stoploss");
						if (stopLossElement != null)
						{
							var stopLoss = new StopLoss
							{
								UseCredit = stopLossElement.GetElementValue("usecredit", null, false).FromYesNo(),
								ActivationPrice = stopLossElement.GetElementValue("activationprice").To<decimal>(),
								GuardTime = stopLossElement.GetElementValueNullable<DateTime>("guardtime", GetNow),
								BrokerRef = stopLossElement.GetElementValue("brokerref", null, false),
								Quantity = stopLossElement.GetElementValue("quantity").To<decimal>(),
								OrderPrice = stopLossElement.GetElementValueNullable<decimal>("orderprice")
							};

							stopOrder.StopLoss = stopLoss;
						}

						var takeProfitElement = sOrderElement.Element("takeprofit");
						if (takeProfitElement != null)
						{
							var takeProfit = new TakeProfit
							{
								ActivationPrice = takeProfitElement.GetElementValue("activationprice").To<decimal>(),
								GuardTime = takeProfitElement.GetElementValueNullable<DateTime>("guardtime", GetNow),
								BrokerRef = takeProfitElement.GetElementValue("brokerref", null, false),
								Quantity = takeProfitElement.GetElementValue("quantity").To<decimal>(),
								Extremum = takeProfitElement.GetElementValueNullable<decimal>("extremum"),
								Level = takeProfitElement.GetElementValueNullable<decimal>("level"),
								Correction = takeProfitElement.GetElementValueToUnit("correction"),
								GuardSpread = takeProfitElement.GetElementValueToUnit("guardspread")
							};

							stopOrder.TakeProfit = takeProfit;
						}

						return stopOrder;
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeBoards(XElement rootElement)
		{
			return new BoardsResponse
			{
				Boards = rootElement
					.Descendants("board")
					.Select(node => new Board
					{
						Id = node.GetAttributeValue<string>("id"),
						Name = node.GetElementValue("name"),
						Market = node.GetElementValue<int>("market"),
						Type = node.GetElementValue<int>("type"),
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializePits(XElement rootElement)
		{
			return new PitsResponse
			{
				Pits = rootElement
					.Descendants("pit")
					.Select(node => new Pit
					{
						SecCode = node.GetAttributeValue("seccode"),
						Board = node.GetAttributeValue("board"),
						Market = node.GetElementValue("market"),
						Decimals = node.GetElementValue("decimals"),
						MinStep = node.GetElementValue("minstep").To<decimal>(),
						LotSize = node.GetElementValue<int>("lotsize"),
						PointCost = node.GetElementValue("point_cost").To<decimal>()
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializePortfolioTPlus(XElement rootElement)
		{
			var result = new PortfolioTPlusResponse
			{
				Client = rootElement.GetAttributeValue("client"),
				CoverageFact = rootElement.GetElementValue<decimal>("coverage_fact"),
				CoveragePlan = rootElement.GetElementValue<decimal>("coverage_plan"),
				CoverageCrit = rootElement.GetElementValue<decimal>("coverage_crit"),
				OpenEquity = rootElement.GetElementValue<decimal>("open_equity"),
				Equity = rootElement.GetElementValue<decimal>("equity"),
				Cover = rootElement.GetElementValue<decimal>("cover"),
				MarginInit = rootElement.GetElementValue<decimal>("init_margin"),
				PnLIncome = rootElement.GetElementValue<decimal>("pnl_income"),
				PnLIntraday = rootElement.GetElementValue<decimal>("pnl_intraday"),
				Leverage = rootElement.GetElementValue<decimal>("leverage"),
				MarginActual = rootElement.GetElementValue<decimal>("margin_level"),
			};

			var moneyElement = rootElement.Element("money");

			result.Money = new Money
			{
				OpenBalance = moneyElement.GetElementValue<decimal>("open_balance"),
				Bought = moneyElement.GetElementValue<decimal>("bought"),
				Sold = moneyElement.GetElementValue<decimal>("sold"),
				Settled = moneyElement.GetElementValue<decimal>("settled"),
				Balance = moneyElement.GetElementValue<decimal>("balance"),
				Tax = moneyElement.GetElementValue<decimal>("tax"),
				MoneyValueParts = moneyElement
					.Descendants("value_part")
					.Select(node => new MoneyValuePart
					{
						Register = node.GetAttributeValue("register"),
						OpenBalance = node.GetElementValue<decimal>("open_balance"),
						Bought = node.GetElementValue<decimal>("bought"),
						Sold = node.GetElementValue<decimal>("sold"),
						Settled = node.GetElementValue<decimal>("settled"),
						Balance = node.GetElementValue<decimal>("balance"),
					})
					.ToArray()
			};

			result.Securities = rootElement
				.Descendants("security")
				.Select(node => new TPlusSecurity
				{
					SecId = node.GetAttributeValue("secid"),
					Market = node.GetElementValue<int>("market"),
					SecCode = node.GetElementValue("seccode"),
					Price = node.GetElementValue<decimal>("price"),
					OpenBalance = node.GetElementValue<int>("open_balance"),
					Bought = node.GetElementValue<int>("bought"),
					Sold = node.GetElementValue<int>("sold"),
					Balance = node.GetElementValue<int>("balance"),
					Buying = node.GetElementValue<int>("buying"),
					Selling = node.GetElementValue<int>("selling"),
					Cover = node.GetElementValue<decimal>("cover"),
					InitMargin = node.GetElementValue<decimal>("init_margin"),
					PnLIncome = node.GetElementValue<decimal>("pnl_income"),
					PnLIntraday = node.GetElementValue<decimal>("pnl_intraday"),
					RiskRateLong = node.GetElementValue<decimal>("riskrate_long"),
					RiskRateShort = node.GetElementValue<decimal>("riskrate_short"),
					MaxBuy = node.GetElementValue<int>("maxbuy"),
					MaxSell = node.GetElementValue<int>("maxsell"),
					TPlusSecurityValueParts = node
					    .Descendants("value_part")
					    .Select(n => new TPlusSecurityValuePart
					    {
							OpenBalance = n.GetElementValue<int>("open_balance"),
							Bought = n.GetElementValue<int>("bought"),
							Sold = n.GetElementValue<int>("sold"),
							Settled = n.GetElementValue<int>("settled"),
							Balance = n.GetElementValue<int>("balance"),
							Buying = n.GetElementValue<int>("buying"),
							Selling = n.GetElementValue<int>("selling"),
					    })
						.ToArray()
				})
				.ToArray();

			return result;
		}

		private static BaseResponse DeserializePortfolioMct(XElement rootElement)
		{
			return new PortfolioMctResponse
			{
				Client = rootElement.GetAttributeValue("client"),
				Capital = rootElement.GetElementValue<decimal>("capital"),
				UtilizationFact = rootElement.GetElementValue<decimal>("utilization_fact"),
				UtilizationPlan = rootElement.GetElementValue<decimal>("utilization_plan"),
				CoverageFact = rootElement.GetElementValue<decimal>("coverage_fact"),
				CoveragePlan = rootElement.GetElementValue<decimal>("coverage_plan"),
				OpenBalance = rootElement.GetElementValue<decimal>("open_balance"),
				Tax = rootElement.GetElementValue<decimal>("tax"),
				PnLIncome = rootElement.GetElementValue<decimal>("pnl_income"),
				PnLIntraday = rootElement.GetElementValue<decimal>("pnl_intraday"),

				Securities = rootElement
					.Descendants("security")
					.Select(node => new MctSecurity
					{
						SecId = node.GetAttributeValue("secid"),
						Market = node.GetElementValue<int>("market"),
						SecCode = node.GetElementValue("seccode"),
						GoRate = node.GetElementValue<decimal>("go_rate"),
						GoRateLong = node.GetElementValue<decimal>("go_rate_long"),
						GoRateShort = node.GetElementValue<decimal>("go_rate_short"),
						Price = node.GetElementValue<decimal>("price"),
						InitRate = node.GetElementValue<decimal>("init_rate"),
						CrossRate = node.GetElementValue<decimal>("cross_rate"),
						InitCrossRate = node.GetElementValue<decimal>("init_cross_rate"),
						OpenBalance = node.GetElementValue<int>("open_balance"),
						Bought = node.GetElementValue<int>("bought"),
						Sold = node.GetElementValue<int>("sold"),
						Balance = node.GetElementValue<int>("balance"),
						Buying = node.GetElementValue<int>("buying"),
						Selling = node.GetElementValue<int>("selling"),
						PosCost = node.GetElementValue<decimal>("pos_cost"),
						GoPosFact = node.GetElementValue<decimal>("go_pos_fact"),
						GoPosPlan = node.GetElementValue<decimal>("go_pos_plan"),
						Tax = rootElement.GetElementValue<decimal>("tax"),
						PnLIncome = node.GetElementValue<decimal>("pnl_income"),
						PnLIntraday = node.GetElementValue<decimal>("pnl_intraday"),
						MaxBuy = node.GetElementValue<long>("maxbuy"),
						MaxSell = node.GetElementValue<long>("maxsell"),
						BoughtAverage = node.GetElementValue<decimal>("bought_average"),
						SoldAverage = node.GetElementValue<decimal>("sold_average"),
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeMaxBuySellTPlus(XElement rootElement)
		{
			return new MaxBuySellTPlusResponse
			{
				Client = rootElement.GetAttributeValue("client"),

				Securities = rootElement
					.Descendants("security")
					.Select(node => new MaxBuySellTPlusSecurity
					{
						SecId = node.GetAttributeValue("secid"),
						Market = node.GetElementValue<int>("market"),
						SecCode = node.GetElementValue("seccode"),
						MaxBuy = node.GetElementValue<long>("maxbuy"),
						MaxSell = node.GetElementValue<long>("maxsell"),
					})
					.ToArray()
			};
		}

		private static BaseResponse DeserializeMessages(XElement rootElement)
		{
			return new MessagesResponse
			{
				Messages = rootElement
					.Descendants("message")
					.Select(node => new TransaqMessage
					{
						Date = node.GetElementValueNullable<DateTime>("date", GetNow),
						Urgent = node.GetElementValue("urgent").FromYesNo(),
						From = node.GetElementValue("from"),
						Text = node.GetElementValue("text")
					})
					.ToArray()
			};
		}

		private static void SerializeDefault(BaseCommandMessage message, XElement rootElement)
		{
		}
	}
}