namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.InteractiveBrokers.Native;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class InteractiveBrokersMessageAdapter
	{
		private readonly Dictionary<SecurityId, Tuple<SortedDictionary<decimal, decimal>, SortedDictionary<decimal, decimal>>> _depths = new Dictionary<SecurityId, Tuple<SortedDictionary<decimal, decimal>, SortedDictionary<decimal, decimal>>>();

		/// <summary>
		/// Запустить сканер инструментов на основе заданных параметров.
		/// </summary>
		/// <param name="message">Настройки фильтра сканера инструментов.</param>
		private void SubscribeScanner(ScannerMarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			ProcessRequest(RequestMessages.SubscribeScanner, ServerVersions.V24, ServerVersions.V4,
				socket =>
				{
					socket
						.Send(message.TransactionId)
						.Send(message.Filter.RowCount)
						.Send(message.Filter.SecurityType)
						.Send(message.Filter.BoardCode)
						.Send(message.Filter.ScanCode)
						.Send(message.Filter.AbovePrice)
						.Send(message.Filter.BelowPrice)
						.Send(message.Filter.AboveVolume)
						.Send(message.Filter.MarketCapAbove)
						.Send(message.Filter.MarketCapBelow)
						.Send(message.Filter.MoodyRatingAbove)
						.Send(message.Filter.MoodyRatingBelow)
						.Send(message.Filter.SpRatingAbove)
						.Send(message.Filter.SpRatingBelow)
						.Send(message.Filter.MaturityDateAbove, "yyyyMMdd")
						.Send(message.Filter.MaturityDateBelow, "yyyyMMdd")
						.Send(message.Filter.CouponRateAbove)
						.Send(message.Filter.CouponRateBelow)
						.Send(message.Filter.ExcludeConvertibleBonds);

					if (socket.ServerVersion >= ServerVersions.V25)
					{
						socket
							.Send(message.Filter.AverageOptionVolumeAbove)
							.Send(message.Filter.ScannerSettingPairs);
					}

					if (socket.ServerVersion >= ServerVersions.V27)
					{
						switch (message.Filter.StockTypeExclude)
						{
							case ScannerFilterStockExcludes.All:
								socket.Send("ALL");
								break;
							case ScannerFilterStockExcludes.Stock:
								socket.Send("STOCK");
								break;
							case ScannerFilterStockExcludes.Etf:
								socket.Send("ETF");
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}

					// send scannerSubscriptionOptions parameter
					if (socket.ServerVersion >= ServerVersions.V70)
					{
						//StringBuilder scannerSubscriptionOptionsStr = new StringBuilder();
						//int scannerSubscriptionOptionsCount = scannerSubscriptionOptions == null ? 0 : scannerSubscriptionOptions.size();
						//if (scannerSubscriptionOptionsCount > 0)
						//{
						//	for (int i = 0; i < scannerSubscriptionOptionsCount; ++i)
						//	{
						//		TagValue tagValue = (TagValue)scannerSubscriptionOptions.get(i);
						//		scannerSubscriptionOptionsStr.append(tagValue.m_tag);
						//		scannerSubscriptionOptionsStr.append("=");
						//		scannerSubscriptionOptionsStr.append(tagValue.m_value);
						//		scannerSubscriptionOptionsStr.append(";");
						//	}
						//}
						//send(scannerSubscriptionOptionsStr.toString());
						socket.Send(string.Empty);
					}
				});
		}

		/// <summary>
		/// Остановить сканер инструментов, запущенный ранее через <see cref="SubscribeScanner"/>.
		/// </summary>
		private void UnSubscribeScanner(long requestId)
		{
			ProcessRequest(RequestMessages.UnSubscribeScanner, ServerVersions.V24, ServerVersions.V1,
				socket => socket.Send(requestId));
		}

		/// <summary>
		/// Call the reqScannerParameters() method to receive an XML document that describes the valid parameters that a scanner subscription can have.
		/// </summary>
		private void RequestScannerParameters()
		{
			ProcessRequest(RequestMessages.RequestScannerParameters, ServerVersions.V24, ServerVersions.V1, socket => { });
		}

		/// <summary>
		/// Call this method to request market data. The market data will be returned by the tickPrice, tickSize, tickOptionComputation(), tickGeneric(), tickString() and tickEFP() methods.
		/// </summary>
		/// <param name="message">this structure contains a description of the contract for which market data is being requested.</param>
		/// <param name="genericFields">comma delimited list of generic tick types. Tick types can be found here: (new Generic Tick Types page) </param>
		/// <param name="snapshot">Allows client to request snapshot market data.</param>
		/// <param name="marketDataOff">Market Data Off - used in conjunction with RTVolume Generic tick type causes only volume data to be sent.</param>
		private void SubscribeMarketData(MarketDataMessage message, IEnumerable<GenericFieldTypes> genericFields, bool snapshot, bool marketDataOff)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			//var security = SessionHolder.Securities[message.SecurityId];

			ProcessRequest(RequestMessages.SubscribeMarketData, ServerVersions.V47, ServerVersions.V11,
				socket =>
				{
					socket
						.Send(message.TransactionId)
						.SendContractId(message.SecurityId)
						.SendSecurity(message)
						.SendIf(ServerVersions.V68, s => socket.Send(message.Class));

					if (socket.ServerVersion >= ServerVersions.V8 && message is WeightedIndexSecurity)
					{
						// TODO
						//socket.SendCombo((WeightedIndexSecurity)security);
					}

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
						//{
						socket.Send(false);
						//}
					}

					if (socket.ServerVersion >= ServerVersions.V31)
					{
						/*
							* Even though SHORTABLE tick type supported only
							* starting server version 33 it would be relatively
							* expensive to expose this restriction here.
							* 
							* Therefore we are relying on TWS doing validation.
							*/

						var genList = new StringBuilder();

						genList.Append(genericFields.Select(t => ((int)t).To<string>()).Join(","));

						if (marketDataOff)
						{
							if (genList.Length > 0)
								genList.Append(",");

							genList.Append("mdoff");
						}

						socket.Send(genList.ToString());
					}

					if (socket.ServerVersion >= ServerVersions.V35)
					{
						socket.Send(snapshot);
					}

					if (socket.ServerVersion >= ServerVersions.V70)
					{
						//StringBuilder mktDataOptionsStr = new StringBuilder();
						//int mktDataOptionsCount = mktDataOptions == null ? 0 : mktDataOptions.size();
						//if( mktDataOptionsCount > 0) {
						//	for( int i = 0; i < mktDataOptionsCount; ++i) {
						//		TagValue tagValue = (TagValue)mktDataOptions.get(i);
						//		mktDataOptionsStr.append( tagValue.m_tag);
						//		mktDataOptionsStr.append( "=");
						//		mktDataOptionsStr.append( tagValue.m_value);
						//		mktDataOptionsStr.append( ";");
						//	}
						//}
						//send( mktDataOptionsStr.toString());
						socket.Send(string.Empty);
					}
				});
		}

		/// <summary>
		/// After calling this method, market data for the specified Id will stop flowing.
		/// </summary>
		private void UnSubscribeMarketData(long requestId)
		{
			ProcessRequest(RequestMessages.UnSubscribeMarketData, 0, ServerVersions.V1,
				socket => socket.Send(requestId));
		}

		//public void RequestHistoricalData(Security security, DateTime endDateTime, TimeSpan duration,
		//							  BarSize barSizeSetting, HistoricalDataType whatToShow, int useRth)
		//{
		//	DateTime beginDateTime = endDateTime.Subtract(duration);

		//	string dur = ConvertPeriodtoIb(beginDateTime, endDateTime);
		//	RequestHistoricalData(tickerId, contract, endDateTime, dur, barSizeSetting, whatToShow, useRth);
		//}

		/// <summary>
		/// used for reqHistoricalData
		/// </summary>
		private static string ConvertPeriodtoIb(DateTimeOffset startTime, DateTimeOffset endTime)
		{
			var period = endTime.Subtract(startTime);
			var secs = period.TotalSeconds;
			long unit;

			if (secs < 1)
				throw new ArgumentOutOfRangeException("endTime", "Period cannot be less than 1 second.");

			if (secs < 86400)
			{
				unit = (long)Math.Ceiling(secs);
				return unit + " S";
			}

			var days = secs / 86400;

			unit = (long)Math.Ceiling(days);

			if (unit <= 34)
				return unit + " D";

			var weeks = days / 7;
			unit = (long)Math.Ceiling(weeks);

			if (unit > 52)
				throw new ArgumentOutOfRangeException("endTime", "Period cannot be bigger than 52 weeks.");

			return unit + " W";
		}

		/// <summary>
		/// Подписаться на получение исторических значения инструмента с заданной периодичностью.
		/// </summary>
		/// <param name="message">Сообщение о подписке или отписки на маркет-данные.</param>
		/// <param name="field">Поле маркет-данных. Поддерживаются следующие значения:
		/// <list type="number">
		/// <item>
		/// <description><see cref="CandleDataTypes.Trades"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.Bid"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.Ask"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.Midpoint"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.BidAsk"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.ImpliedVolatility"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.HistoricalVolatility"/>.</description>
		/// </item>
		/// </list></param>
		/// <param name="useRth">Получать данные только по торговому времени. По-умолчанию используется торговое время.</param>
		private void SubscribeHistoricalCandles(MarketDataMessage message, Level1Fields field, bool useRth = true)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			//if (message.CandleType != typeof(TimeFrameCandle))
			//	throw new ArgumentException("Interactive Brokers не поддерживает свечи типа {0}.".Put(series.CandleType), "series");

			if (!(message.Arg is TimeSpan))
				throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(message.Arg), "message");

			var timeFrame = (TimeSpan)message.Arg;

			//var security = SessionHolder.Securities[message.SecurityId];

			ProcessRequest(RequestMessages.SubscribeHistoricalData, ServerVersions.V16, ServerVersions.V6,
				socket =>
				{
					socket
						.Send(message.TransactionId)
						.SendIf(ServerVersions.V68, s => socket.SendContractId(message.SecurityId))
						.SendSecurity(message)
						.SendIf(ServerVersions.V68, s => socket.Send(message.Class))
						.SendIncludeExpired(message.ExpiryDate)
						.SendEndDate(message.To)
						.SendTimeFrame(timeFrame)
						.Send(ConvertPeriodtoIb(message.From, message.To))
						.Send(useRth)
						.SendLevel1Field(field);

					if (socket.ServerVersion > ServerVersions.V16)
					{
						//Send date times as seconds since 1970
						socket.Send(2);
					}

					// TODO
					WeightedIndexSecurity indexSecurity = null;//security as WeightedIndexSecurity;
					if (indexSecurity != null)
						socket.SendCombo(indexSecurity);

					if (socket.ServerVersion >= ServerVersions.V70)
					{
						//StringBuilder chartOptionsStr = new StringBuilder();
						//int chartOptionsCount = chartOptions == null ? 0 : chartOptions.size();
						//if (chartOptionsCount > 0)
						//{
						//	for (int i = 0; i < chartOptionsCount; ++i)
						//	{
						//		TagValue tagValue = (TagValue)chartOptions.get(i);
						//		chartOptionsStr.append(tagValue.m_tag);
						//		chartOptionsStr.append("=");
						//		chartOptionsStr.append(tagValue.m_value);
						//		chartOptionsStr.append(";");
						//	}
						//}
						//send(chartOptionsStr.toString());
						socket.Send(string.Empty);
					}
				});
		}

		/// <summary>
		/// Подписаться на получение свечек реального времени.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="field">Поля маркет-данных, на основе которых будут строяться свечи. Поддерживаются следующие значения:
		/// <list type="number">
		/// <item>
		/// <description><see cref="CandleDataTypes.Trades"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.Bid"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.Ask"/>.</description>
		/// </item>
		/// <item>
		/// <description><see cref="CandleDataTypes.Midpoint"/>.</description>
		/// </item>
		/// </list>
		/// </param>
		/// <param name="useRth">Строить свечи только по торговому времени. По-умолчанию используется торговое время.</param>
		private void SubscribeRealTimeCandles(MarketDataMessage message, Level1Fields field = CandleDataTypes.Trades, bool useRth = true)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			//var security = SessionHolder.Securities[message.SecurityId];

			ProcessRequest(RequestMessages.SubscribeRealTimeCandles, ServerVersions.V34, ServerVersions.V3,
				socket =>
					socket
						.Send(message.TransactionId)
						.SendIf(ServerVersions.V68, s => socket.SendContractId(message.SecurityId))
						.SendSecurity(message)
						.SendIf(ServerVersions.V68, s => socket.Send(message.Class))
						.Send(5) // Поддерживается только 5 секундный тайм-фрейм.
						.SendLevel1Field(field)
						.Send(useRth)
						.SendIf(ServerVersions.V70, s =>
						{
							//StringBuilder realTimeBarsOptionsStr = new StringBuilder();
							//int realTimeBarsOptionsCount = realTimeBarsOptions == null ? 0 : realTimeBarsOptions.size();
							//if (realTimeBarsOptionsCount > 0)
							//{
							//	for (int i = 0; i < realTimeBarsOptionsCount; ++i)
							//	{
							//		TagValue tagValue = (TagValue)realTimeBarsOptions.get(i);
							//		realTimeBarsOptionsStr.append(tagValue.m_tag);
							//		realTimeBarsOptionsStr.append("=");
							//		realTimeBarsOptionsStr.append(tagValue.m_value);
							//		realTimeBarsOptionsStr.append(";");
							//	}
							//}
							//send(realTimeBarsOptionsStr.toString());
							socket.Send(string.Empty);
						}));
		}

		/// <summary>
		/// Остановить подписку получения свечек, ранее созданную через <see cref="SubscribeRealTimeCandles"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="requestId"></param>
		private void UnSubscribeRealTimeCandles(MarketDataMessage message, long requestId)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			ProcessRequest(RequestMessages.UnSubscribeRealTimeCandles, ServerVersions.V34, ServerVersions.V1,
				socket => socket.Send(requestId));
		}

		/// <summary>
		/// Call this method to request market depth for a specific contract. The market depth will be returned by the updateMktDepth() and updateMktDepthL2() methods.
		/// </summary>
		/// <param name="message">this structure contains a description of the contract for which market depth data is being requested.</param>
		private void SubscribeMarketDepth(MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			//var security = SessionHolder.Securities[message.SecurityId];

			ProcessRequest(RequestMessages.SubscribeMarketDepth, ServerVersions.V6, ServerVersions.V5,
				socket => socket
					.Send(message.TransactionId)
					.SendIf(ServerVersions.V68, s => socket.SendContractId(message.SecurityId))
					.SendSecurity(message, false)
					.SendIf(ServerVersions.V68, s => socket.Send(message.Class))
					.SendIf(ServerVersions.V19, s => socket.Send(message.MaxDepth))
					.SendIf(ServerVersions.V70, s =>
					{
						//StringBuilder realTimeBarsOptionsStr = new StringBuilder();
						//int realTimeBarsOptionsCount = realTimeBarsOptions == null ? 0 : realTimeBarsOptions.size();
						//if (realTimeBarsOptionsCount > 0)
						//{
						//	for (int i = 0; i < realTimeBarsOptionsCount; ++i)
						//	{
						//		TagValue tagValue = (TagValue)realTimeBarsOptions.get(i);
						//		realTimeBarsOptionsStr.append(tagValue.m_tag);
						//		realTimeBarsOptionsStr.append("=");
						//		realTimeBarsOptionsStr.append(tagValue.m_value);
						//		realTimeBarsOptionsStr.append(";");
						//	}
						//}
						//send(realTimeBarsOptionsStr.toString());
						socket.Send(string.Empty);
					}));
		}

		/// <summary>
		/// After calling this method, market depth data for the specified Id will stop flowing.
		/// </summary>
		private void UnSubscriveMarketDepth(long requestId)
		{
			ProcessRequest(RequestMessages.UnSubscribeMarketDepth, ServerVersions.V6, ServerVersions.V1,
				socket => socket.Send(requestId));
		}

		/// <summary>
		/// Call this method to start receiving news bulletins. Each bulletin will be returned by the updateNewsBulletin() method.
		/// </summary>
		/// <param name="allMessages">if set to TRUE, returns all the existing bulletins for the current day and any new ones. IF set to FALSE, will only return new bulletins.</param>
		private void SubscribeNewsBulletins(bool allMessages)
		{
			ProcessRequest(RequestMessages.SubscribeNewsBulletins, 0, ServerVersions.V1, socket => socket.Send(allMessages));
		}

		/// <summary>
		/// Call this method to stop receiving news bulletins.
		/// </summary>
		private void UnSubscribeNewsBulletins()
		{
			ProcessRequest(RequestMessages.UnSubscribeNewsBulletins, 0, ServerVersions.V1, socket => { });
		}

		/// <summary>
		/// Call this function to download all details for a particular underlying. the contract details will be received via the contractDetails() function on the EWrapper.
		/// </summary>
		/// <param name="criteria">summary description of the contract being looked up.</param>
		private void RequestSecurityInfo(SecurityLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException("criteria");

			ProcessRequest(RequestMessages.RequestContractData, ServerVersions.V4, ServerVersions.V7, socket =>
			{
				//MIN_SERVER_VER_CONTRACT_DATA_CHAIN = 40
				if (socket.ServerVersion >= ServerVersions.V40)
				{
					socket.Send(criteria.TransactionId);
				}

				if (socket.ServerVersion >= ServerVersions.V37)
				{
					socket.SendContractId(criteria.SecurityId);
				}

				socket
					.SendSecurity(criteria, false)
					.SendIf(ServerVersions.V68, s => socket.Send(criteria.Class))
					.SendIncludeExpired(criteria.ExpiryDate)
					.SendSecurityId(criteria.SecurityId);
			});
		}

		/// <summary>
		/// Подписаться на получение отчетов по рынку для заданного инструмента.
		/// </summary>
		/// <param name="message"></param>
		private void SubscribeFundamentalReport(FundamentalReportMarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			//var security = SessionHolder.Securities[message.SecurityId];

			ProcessRequest(RequestMessages.SubscribeFundamentalData, ServerVersions.V40, ServerVersions.V2,
				socket => socket
					.Send(message.TransactionId)
					.SendIf(ServerVersions.V68, s => socket.SendContractId(message.SecurityId))
					.Send(message.Name)
					.SendSecurityType(message.SecurityType)
					.SendBoardCode(message.SecurityId.BoardCode)
					.SendPrimaryExchange(message)
					.SendCurrency(message.Currency)
					.SendSecurityCode(message.SecurityId.SecurityCode)
					.SendFundamental(message.Report));
		}

		/// <summary>
		/// Остановить подписку на получение отчетов по рынку для заданного инструмента, созданная ранее через <see cref="SubscribeFundamentalReport"/>.
		/// </summary>
		/// <param name="requestId"></param>
		private void UnSubscribeFundamentalReport(long requestId)
		{
			ProcessRequest(RequestMessages.UnSubscribeFundamentalData, ServerVersions.V40, ServerVersions.V1,
				socket => socket.Send(requestId));
		}

		/// <summary>
		/// Подписаться на получение подразумеваемой волатильности для заданного инструмента.
		/// </summary>
		/// <param name="message"></param>
		private void SubscribeCalculateImpliedVolatility(OptionCalcMarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			//var security = SessionHolder.Securities[message.SecurityId];

			ProcessRequest(RequestMessages.SubscribeCalcImpliedVolatility, ServerVersions.V49, ServerVersions.V2,
				socket => socket
					.Send(message.TransactionId)
					.SendSecurity(message)
					.SendIf(ServerVersions.V68, s => socket.Send(message.Class))
					.Send(message.OptionPrice)
					.Send(message.AssetPrice));
		}

		/// <summary>
		/// Остановить подписку на получение подразумеваемой волатильности для заданного инструмента,
		/// созданная ранее через <see cref="SubscribeCalculateImpliedVolatility"/>.
		/// </summary>
		/// <param name="requestId"></param>
		private void UnSubscribeCalculateImpliedVolatility(long requestId)
		{
			ProcessRequest(RequestMessages.UnSubscribeCalcImpliedVolatility, ServerVersions.V50, ServerVersions.V1,
				socket => socket.Send(requestId));
		}

		/// <summary>
		/// Подписаться на получение греков для заданного инструмента.
		/// </summary>
		/// <param name="message">Инструмент.</param>
		private void SubscribeCalculateOptionPrice(OptionCalcMarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			//var security = SessionHolder.Securities[message.SecurityId];

			ProcessRequest(RequestMessages.SubscribeCalcOptionPrice, ServerVersions.V50, ServerVersions.V2,
				socket =>
					socket
						.Send(message.TransactionId)
						.SendContractId(message.SecurityId)
						.SendSecurity(message)
						.SendIf(ServerVersions.V68, s => socket.Send(message.Class))
						.Send(message.ImpliedVolatility)
						.Send(message.AssetPrice));
		}

		/// <summary>
		/// Остановить подписку на получение греков для заданного инструмента,
		/// созданная ранее через <see cref="SubscribeCalculateOptionPrice"/>.
		/// </summary>
		/// <param name="requestId"></param>
		private void UnSubscribeCalculateOptionPrice(long requestId)
		{
			ProcessRequest(RequestMessages.UnSubscribeCalcOptionPrice, ServerVersions.V50, ServerVersions.V1,
					socket => socket.Send(requestId));
		}

		private void SetMarketDataType()
		{
			ProcessRequest(RequestMessages.SetMarketDataType, ServerVersions.V55, ServerVersions.V1,
				socket => socket.Send(SessionHolder.IsRealTimeMarketData ? 1 : 2));
		}

		///// <summary>
		///// Call this method to request FA configuration information from TWS. The data returns in an XML string via the receiveFA() method.
		///// </summary>
		///// <param name="faDataType">
		///// faDataType - specifies the type of Financial Advisor configuration data being requested. Valid values include:
		///// 1 = GROUPS
		///// 2 = PROFILE
		///// 3 =ACCOUNT ALIASES
		///// </param>
		//public void RequestFA(FADataType faDataType)
		//{
		//	// This feature is only available for versions of TWS >= 13
		//	if (_serverVersion < 13)
		//	{
		//		error(ErrorMessage.UpdateTws, "Does not support request FA.");
		//		return;
		//	}

		//	int version = 1;

		//	send((int) RequestMessages.RequestFA);
		//	send(version);
		//	send((int) faDataType);
		//}

		///// <summary>
		///// Call this method to request FA configuration information from TWS. The data returns in an XML string via a "receiveFA" ActiveX event.  
		///// </summary>
		///// <param name="faDataType">
		///// specifies the type of Financial Advisor configuration data being requested. Valid values include:
		///// 1 = GROUPS
		///// 2 = PROFILE
		///// 3 = ACCOUNT ALIASES</param>
		///// <param name="xml">the XML string containing the new FA configuration information.</param>
		//public void ReplaceFA(FADataType faDataType, string xml)
		//{
		//	// This feature is only available for versions of TWS >= 13
		//	if (_serverVersion < 13)
		//	{
		//		error(ErrorMessage.UpdateTws, "Does not support Replace FA.");
		//		return;
		//	}

		//	int version = 1;

		//	send((int) RequestMessages.ReplaceFA);
		//	send(version);
		//	send((int) faDataType);
		//	send(xml);
		//}

		private SecurityId GetSecurityId(int requestId)
		{
			return _requestIds.GetKey(requestId).Item2;
		}

		private Level1ChangeMessage GetLevel1Message(int requestId)
		{
			return new Level1ChangeMessage
			{
				SecurityId = GetSecurityId(requestId),
				ServerTime = SessionHolder.CurrentTime.Convert(TimeZoneInfo.Utc),
			};
		}

		private void ProcessTick(int requestId, FieldTypes field, decimal price, decimal? volume)
		{
			var l1Msg = GetLevel1Message(requestId);

			switch (field)
			{
				case FieldTypes.BidPrice:
				case FieldTypes.BidVolume:
					l1Msg.TryAdd(Level1Fields.BestBidPrice, price);
					l1Msg.TryAdd(Level1Fields.BestBidVolume, volume);
					break;
				case FieldTypes.AskPrice:
				case FieldTypes.AskVolume:
					l1Msg.TryAdd(Level1Fields.BestAskPrice, price);
					l1Msg.TryAdd(Level1Fields.BestAskVolume, volume);
					break;
				case FieldTypes.LastPrice:
				case FieldTypes.LastVolume:
					l1Msg.TryAdd(Level1Fields.LastTradePrice, price);
					l1Msg.TryAdd(Level1Fields.LastTradeVolume, volume);
					break;
				case FieldTypes.OpenPrice:
					l1Msg.TryAdd(Level1Fields.OpenPrice, price);
					break;
				case FieldTypes.HighPrice:
					l1Msg.TryAdd(Level1Fields.HighPrice, price);
					break;
				case FieldTypes.LowPrice:
					l1Msg.TryAdd(Level1Fields.LowPrice, price);
					break;
				case FieldTypes.ClosePrice:
					l1Msg.TryAdd(Level1Fields.ClosePrice, price);
					break;
				case FieldTypes.Volume:
					l1Msg.TryAdd(Level1Fields.Volume, volume);
					break;
				case FieldTypes.OpenInterest:
					l1Msg.TryAdd(Level1Fields.OpenInterest, volume);
					break;
				case FieldTypes.OptionHistoricalVolatility:
					l1Msg.TryAdd(Level1Fields.HistoricalVolatility, price);
					break;
				case FieldTypes.OptionImpliedVolatility:
					l1Msg.TryAdd(Level1Fields.ImpliedVolatility, price);
					break;
				case FieldTypes.LastYield:
					l1Msg.TryAdd(Level1Fields.Yield, price);
					break;
				case FieldTypes.CustOptionComputation:
					break;
				case FieldTypes.TradeCount:
					l1Msg.TryAdd(Level1Fields.TradesCount, (int)(volume ?? 0));
					break;
				//default:
				//	throw new InvalidOperationException("Неизвестный тип поля {0} для инструмента {1}.".Put(priceField, security));
			}

			SendOutMessage(l1Msg);
		}

		private void ReadTickPrice(IBSocket socket, ServerVersions version)
		{
			var requestId = socket.ReadInt();
			var priceField = (FieldTypes)socket.ReadInt();
			var price = socket.ReadDecimal();
			var volume = version >= ServerVersions.V2 ? socket.ReadInt() : (decimal?)null;
			var canAutoExecute = version >= ServerVersions.V3 ? socket.ReadBool() : (bool?)null;

			ProcessTick(requestId, priceField, price, volume);
		}

		private void ReadTickVolume(IBSocket socket)
		{
			var requestId = socket.ReadInt();
			var volumeType = (FieldTypes)socket.ReadInt();
			var volume = socket.ReadInt();

			ProcessTick(requestId, volumeType, 0, volume);
		}

		private void ReadTickOptionComputation(IBSocket socket, ServerVersions version)
		{
			var requestId = socket.ReadInt();
			var fieldType = (FieldTypes)socket.ReadInt();

			decimal? impliedVol = socket.ReadDecimal();
			if (impliedVol < 0)
			{
				// -1 is the "not yet computed" indicator
				impliedVol = null;
			}

			decimal? delta = socket.ReadDecimal();
			if (Math.Abs(delta.Value) > 1)
			{
				// -2 is the "not yet computed" indicator
				delta = null;
			}

			decimal? optPrice = null;
			decimal? pvDividend = null;
			decimal? gamma = null;
			decimal? vega = null;
			decimal? theta = null;
			decimal? undPrice;

			if (version >= ServerVersions.V6 || fieldType == FieldTypes.ModelOption)
			{
				// introduced in version == 5
				optPrice = socket.ReadDecimal();
				if (optPrice < 0)
				{
					// -1 is the "not yet computed" indicator
					optPrice = null;
				}

				pvDividend = socket.ReadDecimal();
				if (pvDividend < 0)
				{
					// -1 is the "not yet computed" indicator
					pvDividend = null;
				}
			}

			if (version >= ServerVersions.V6)
			{
				gamma = socket.ReadDecimal();
				if (Math.Abs(gamma.Value) > 1)
				{
					// -2 is the "not yet computed" indicator
					gamma = null;
				}

				vega = socket.ReadDecimal();
				if (Math.Abs(vega.Value) > 1)
				{
					// -2 is the "not yet computed" indicator
					vega = null;
				}

				theta = socket.ReadDecimal();
				if (Math.Abs(theta.Value) > 1)
				{
					// -2 is the "not yet computed" indicator
					theta = null;
				}

				undPrice = socket.ReadDecimal();
				if (undPrice < 0)
				{
					// -1 is the "not yet computed" indicator
					undPrice = null;
				}
			}

			var l1Msg = GetLevel1Message(requestId)
				.TryAdd(Level1Fields.Delta, delta)
				.TryAdd(Level1Fields.Gamma, gamma)
				.TryAdd(Level1Fields.Vega, vega)
				.TryAdd(Level1Fields.Theta, theta)
				.TryAdd(Level1Fields.ImpliedVolatility, impliedVol)
				.TryAdd(Level1Fields.TheorPrice, optPrice)
				.TryAdd(Level1Fields.Yield, pvDividend);

			SendOutMessage(l1Msg);

			//tickOptionComputation(tickerId, tickType, impliedVol, delta, optPrice, pvDividend, gamma, vega, theta,
			//                      undPrice);
		}

		private void ReadTickGeneric(IBSocket socket)
		{
			var requestId = socket.ReadInt();
			var field = (FieldTypes)socket.ReadInt();
			var valueRenamed = socket.ReadDecimal();

			ProcessTick(requestId, field, valueRenamed, null);

			//tickGeneric(tickerId, (FieldTypes) tickType, valueRenamed);
		}

		private void ReadTickString(IBSocket socket)
		{
			var requestId = socket.ReadInt();
			var field = (FieldTypes)socket.ReadInt();
			var valueRenamed = socket.ReadStr();

			//GetLevel1Message(requestId);
			//tickString(requestId, fieldType, valueRenamed);
		}

		private void ReadTickEfp(IBSocket socket)
		{
			var requestId = socket.ReadInt();
			var fieldType = (FieldTypes)socket.ReadInt();
			var basisPoints = socket.ReadDecimal();
			var formattedBasisPoints = socket.ReadStr();
			var impliedFuturesPrice = socket.ReadDecimal();
			var holdDays = socket.ReadInt();
			var futureExpiry = socket.ReadStr();
			var dividendImpact = socket.ReadDecimal();
			var dividendsToExpiry = socket.ReadDecimal();
			//tickEfp(requestId, fieldType, basisPoints, formattedBasisPoints, impliedFuturesPrice,
			//        holdDays, futureExpiry, dividendImpact, dividendsToExpiry);

			var l1Msg = GetLevel1Message(requestId)
				.TryAdd(Level1Fields.StepPrice, basisPoints)
				.TryAdd(Level1Fields.Yield, dividendsToExpiry);

			SendOutMessage(l1Msg);
		}

		private void ReadScannerData(IBSocket socket, ServerVersions version)
		{
			var requestId = socket.ReadInt();
			var count = socket.ReadInt();

			var tmp = Enumerable
				.Range(0, count)
				.Select(s =>
				{
					var rank = socket.ReadInt();
					var contractId = version >= ServerVersions.V3 ? socket.ReadInt() : -1;

					var secName = socket.ReadStr();
					var type = socket.ReadSecurityType();
					var expiryDate = socket.ReadExpiry();
					var strike = socket.ReadDecimal();
					var optionType = socket.ReadOptionType();
					var boardCode = socket.ReadBoardCode();
					var currency = socket.ReadCurrency();
					var secCode = socket.ReadLocalCode(secName);
					var marketName = socket.ReadStr();
					var secClass = socket.ReadStr();

					var distance = socket.ReadStr();
					var benchmark = socket.ReadStr();
					var projection = socket.ReadStr();
					var legs = version >= ServerVersions.V2 ? socket.ReadStr() : null;

					return new
					{
						Rank = rank,
						ContractId = contractId,
						SecName = secName,
						SecCode = secCode,
						Type = type,
						ExpiryDate = expiryDate,
						Strike = strike,
						OptionType = optionType,
						BoardCode = boardCode,
						Currency = currency,
						MarketName = marketName,
						SecClass = secClass,
						Distance = distance,
						Benchmark = benchmark,
						Projection = projection,
						Legs = legs,
					};
				})
				.ToArray();

			var results = tmp.Select(t =>
			{
				var secId = new SecurityId
				{
					SecurityCode = t.SecCode,
					BoardCode = GetBoardCode(t.BoardCode),
					InteractiveBrokers = t.ContractId,
				};

				SendOutMessage(new SecurityMessage
				{
					SecurityId = secId,
					Name = t.SecName,
					SecurityType = t.Type,
					ExpiryDate = t.ExpiryDate,
					Strike = t.Strike,
					OptionType = t.OptionType,
					Currency = t.Currency,
					Class = t.SecClass
				});

				var result = new ScannerResult
				{
					Rank = t.Rank,
					SecurityId = secId,
					Distance = t.Distance,
					Benchmark = t.Benchmark,
					Projection = t.Projection,
					Legs = t.Legs
				};

				return result;
			}).ToArray();

			SendOutMessage(new ScannerResultMessage
			{
				Results = results,
				OriginalTransactionId = requestId,
			});
		}

		private void ReadSecurityInfo(IBSocket socket, ServerVersions version)
		{
			var requestId = version >= ServerVersions.V3 ? socket.ReadInt() : -1;

			var secName = socket.ReadStr();
			var type = socket.ReadSecurityType();
			var expiryDate = socket.ReadExpiry();
			var strike = socket.ReadDecimal();
			var optionType = socket.ReadOptionType();
			var boardCode = socket.ReadBoardCode();
			var currency = socket.ReadCurrency();
			var secCode = version >= ServerVersions.V2 ? socket.ReadLocalCode(secName) : null;
			var marketName = socket.ReadStr();
			var secClass = socket.ReadStr();
			var contractId = socket.ReadInt();
			var priceStep = socket.ReadDecimal();
			var multiplier = socket.ReadMultiplier();
			var orderTypes = socket.ReadStr();
			var validExchanges = socket.ReadStr();
			var priceMagnifier = version >= ServerVersions.V2 ? socket.ReadInt() : (int?)null;
			var underlyingSecurityNativeId = version >= ServerVersions.V4 ? socket.ReadInt() : (int?)null;
			var name = version >= ServerVersions.V4 ? socket.ReadStr() : null;
			var routingExchange = version >= ServerVersions.V4 ? socket.ReadBoardCode() : null;
			var contractMonth = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var industry = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var category = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var subCategory = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var timeZoneId = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var tradingHours = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var liquidHours = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var evRule = version >= ServerVersions.V8 ? socket.ReadStr() : null;
			var evMultiplier = version >= ServerVersions.V8 ? socket.ReadDecimal() : (decimal?)null;

			var secId = new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = GetBoardCode(boardCode),
				InteractiveBrokers = contractId,
			};

			if (version >= ServerVersions.V7)
				socket.ReadSecurityId(secId);

			var secMsg = new SecurityMessage
			{
				SecurityId = secId,
				Name = secName,
				SecurityType = type,
				ExpiryDate = expiryDate,
				Strike = strike,
				OptionType = optionType,
				Currency = currency,
				Multiplier = multiplier ?? 0,
				Class = secClass,
				OriginalTransactionId = requestId,
				PriceStep = priceStep,
			};

			secMsg.SetMarketName(marketName);
			secMsg.SetOrderTypes(orderTypes);
			secMsg.SetValidExchanges(validExchanges);

			if (priceMagnifier != null)
				secMsg.SetPriceMagnifier(priceMagnifier.Value);

			if (!routingExchange.IsEmpty())
				secMsg.SetRoutingBoard(routingExchange);

			if (contractMonth != null)
				secMsg.SetContractMonth(contractMonth);

			if (industry != null)
				secMsg.SetIndustry(industry);

			if (category != null)
				secMsg.SetCategory(category);

			if (subCategory != null)
				secMsg.SetSubCategory(subCategory);

			if (timeZoneId != null)
				secMsg.SetTimeZoneId(timeZoneId);

			if (tradingHours != null)
				secMsg.SetTradingHours(tradingHours);

			if (liquidHours != null)
				secMsg.SetLiquidHours(liquidHours);

			if (evRule != null)
				secMsg.SetEvRule(evRule);

			if (evMultiplier != null)
				secMsg.SetEvMultiplier(evMultiplier.Value);

			// TODO
			//if (underlyingSecurityNativeId != null)
			//	ProcessSecurityAction(null, SecurityIdGenerator.GenerateId(underlyingSecurityNativeId.Value.To<string>(), exchangeBoard), underSec => security.UnderlyingSecurityId = underSec.Id);

			SendOutMessage(secMsg);
		}

		private void ReadBondInfo(IBSocket socket, ServerVersions version)
		{
			var requestId = version >= ServerVersions.V3 ? socket.ReadInt() : -1;

			var secCode = socket.ReadStr();
			var type = socket.ReadSecurityType();
			var cusip = socket.ReadStr();
			var coupon = socket.ReadDecimal();
			var maturity = socket.ReadStr();
			var issueDate = socket.ReadStr();
			var ratings = socket.ReadStr();
			var bondType = socket.ReadStr();
			var couponType = socket.ReadStr();
			var convertible = socket.ReadBool();
			var callable = socket.ReadBool();
			var putable = socket.ReadBool();
			var description = socket.ReadStr();
			var boardCode = socket.ReadBoardCode();
			var currency = socket.ReadCurrency();
			var marketName = socket.ReadStr();
			var secClass = socket.ReadStr();
			var contractId = socket.ReadInt();
			var priceStep = socket.ReadDecimal();
			var orderTypes = socket.ReadStr();
			var validExchanges = socket.ReadStr();

			var nextOptionDate = version >= ServerVersions.V2 ? socket.ReadStr() : null;
			var nextOptionType = version >= ServerVersions.V2 ? socket.ReadStr() : null;
			var nextOptionPartial = version >= ServerVersions.V2 ? socket.ReadBool() : (bool?)null;
			var notes = version >= ServerVersions.V2 ? socket.ReadStr() : null;

			var name = version >= ServerVersions.V4 ? socket.ReadStr() : null;
			var evRule = version >= ServerVersions.V6 ? socket.ReadStr() : null;
			var evMultiplier = version >= ServerVersions.V6 ? socket.ReadDecimal() : (decimal?)null;
			
			var secId = new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = GetBoardCode(boardCode),
				InteractiveBrokers = contractId,
			};

			if (version >= ServerVersions.V5)
				socket.ReadSecurityId(secId);

			var secMsg = new SecurityMessage
			{
				SecurityId = secId,
				//Name = secName,
				SecurityType = type,
				Currency = currency,
				Class = secClass,
				PriceStep = priceStep,
			};

			secMsg.SetMarketName(marketName);
			secMsg.SetOrderTypes(orderTypes);
			secMsg.SetValidExchanges(validExchanges);

			// TODO
			//s.SetBondCusip(cusip);
			//s.SetCoupon(coupon);
			//s.SetMaturity(maturity);
			//s.SetIssueDate(issueDate);
			//s.SetRatings(ratings);
			//s.SetBondType(bondType);
			//s.SetCouponType(couponType);
			//s.SetConvertible(convertible);
			//s.SetCallable(callable);
			//s.SetPutable(putable);
			//s.SetDescription(description);

			//if (nextOptionDate != null)
			//	s.SetNextOptionDate(nextOptionDate);

			//if (nextOptionType != null)
			//	s.SetNextOptionType(nextOptionType);

			//if (nextOptionPartial != null)
			//	s.SetNextOptionPartial(nextOptionPartial.Value);

			//if (notes != null)
			//	s.SetNotes(notes);

			if (evRule != null)
				secMsg.SetEvRule(evRule);

			if (evMultiplier != null)
				secMsg.SetEvMultiplier(evMultiplier.Value);

			SendOutMessage(secMsg);
		}

		private void ReadMarketDepth(IBSocket socket, ResponseMessages message)
		{
			var requestId = socket.ReadInt();

			var secId = GetSecurityId(requestId);

			/* position */
			var pos = socket.ReadInt();

			if (message == ResponseMessages.MarketDepthL2)
			{
				/* marketMaker */
				secId.BoardCode = socket.ReadBoardCode();
			}

			var operation = socket.ReadInt();

			var side = socket.ReadBool() ? Sides.Buy : Sides.Sell;
			var price = socket.ReadDecimal();
			var volume = socket.ReadInt();

			var prevQuotes = _depths.SafeAdd(secId, key =>
				Tuple.Create(new SortedDictionary<decimal, decimal>(new BackwardComparer<decimal>()), new SortedDictionary<decimal, decimal>()));

			var quotes = side == Sides.Buy ? prevQuotes.Item1 : prevQuotes.Item2;

			SessionHolder.AddDebugLog("MD {0} {1} POS {2} PRICE {3} VOL {4}", secId, operation, pos, price, volume);

			switch (operation)
			{
				case 0: // insert
				{
					if (!CollectionHelper.TryAdd(quotes, price, volume))
						quotes[price] += volume;

					break;
				}
				case 1: // update
				{
					if (quotes.Count > (pos + 1))
					{
						var sign = side == Sides.Buy ? 1 : -1;

						if (quotes[pos + 1] * sign >= price * sign)
						{
							for (var i = quotes.Count - 1; i >= pos + 1; i--)
								quotes.Remove(quotes[quotes.Keys.ElementAt(i)]);
						}
					}

					if (quotes.Count > pos)
					{
						//if (quotes[quotes.Keys.ElementAt(pos)] == price)
						//	quotes[price] = volume;
						//else
						//{
						//	depth.Remove(quotes[pos]);
						//	depth.AddQuote(quote);
						//}

						quotes[price] = volume;
					}
					else
					{
						if (!CollectionHelper.TryAdd(quotes, price, volume))
							quotes[price] += volume;
					}

					break;
				}
				case 2: // delete
				{
					if (quotes.Count > pos)
						quotes.Remove(quotes.Keys.ElementAt(pos));

					break;
				}
			}

			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = secId,
				Bids = prevQuotes.Item1.Select(p => new QuoteChange(Sides.Buy, p.Key, p.Value)).ToArray(),
				Asks = prevQuotes.Item2.Select(p => new QuoteChange(Sides.Sell, p.Key, p.Value)).ToArray(),
				ServerTime = SessionHolder.CurrentTime.Convert(TimeZoneInfo.Utc),
			});
		}

		private void ReadNewsBulletins(IBSocket socket)
		{
			var newsId = socket.ReadInt();
			var newsType = (ExchangeNewsTypes)socket.ReadInt();
			var newsMessage = socket.ReadStr();
			var originatingExch = socket.ReadBoardCode();

			SendOutMessage(new NewsMessage
			{
				Id = newsId.To<string>(),
				BoardCode = originatingExch,
				Headline = newsMessage,
				ExtensionInfo = new Dictionary<object, object> { { "Type", newsType } },
				ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Est)
			});
		}

		private void ReadHistoricalData(IBSocket socket, ServerVersions version)
		{
			var requestId = socket.ReadInt();

			if (version >= ServerVersions.V2)
			{
				//Read Start Date String
				/*String startDateStr = */
				socket.ReadStr();
				/*String endDateStr   = */
				socket.ReadStr();
				//completedIndicator += ("-" + startDateStr + "-" + endDateStr);
			}

			var secId = GetSecurityId(requestId);

			var itemCount = socket.ReadInt();
			for (var i = 0; i < itemCount; i++)
			{
				//Comes in as seconds
				//2 - dates are returned as a long integer specifying the number of seconds since 1/1/1970 GMT.
				var time = socket.ReadLongDateTime();

				var open = socket.ReadDecimal();
				var high = socket.ReadDecimal();
				var low = socket.ReadDecimal();
				var close = socket.ReadDecimal();
				var volume = socket.ReadInt();
				var wap = socket.ReadDecimal();
				/* hasGaps */
				socket.ReadStr().To<bool>();

				var barCount = -1;

				if (version >= ServerVersions.V3)
					barCount = socket.ReadInt();

				SendOutMessage(new TimeFrameCandleMessage
				{
					OpenPrice = open,
					HighPrice = high,
					LowPrice = low,
					ClosePrice = close,
					TotalVolume = volume,
					OpenTime = time,
					CloseVolume = barCount,
					SecurityId = secId,
					OriginalTransactionId = requestId,
					State = CandleStates.Finished,
					IsFinished = i == (itemCount - 1)
				});
			}
		}

		private void ReadScannerParameters(IBSocket socket)
		{
			var xml = socket.ReadStr();
			SendOutMessage(new ScannerParametersMessage { Parameters = xml });
		}

		private void ReadRealTimeBars(IBSocket socket)
		{
			var requestId = socket.ReadInt();
			var time = socket.ReadLongDateTime();
			var open = socket.ReadDecimal();
			var high = socket.ReadDecimal();
			var low = socket.ReadDecimal();
			var close = socket.ReadDecimal();
			var volume = socket.ReadLong();
			var wap = socket.ReadDecimal();
			var count = socket.ReadInt();

			SendOutMessage(new TimeFrameCandleMessage
			{
				OpenPrice = open,
				HighPrice = high,
				LowPrice = low,
				ClosePrice = close,
				TotalVolume = volume,
				OpenTime = time,
				CloseVolume = count,
				SecurityId = GetSecurityId(requestId),
				OriginalTransactionId = requestId,
			});

			//realTimeBar(reqId, time, open, high, low, close, volume, wap, count);
		}

		private void ReadFundamentalData(IBSocket socket)
		{
			var requestId = socket.ReadInt();
			var data = socket.ReadStr();

			SendOutMessage(new FundamentalReportMessage
			{
				Data = data,
				OriginalTransactionId = requestId,
			});
		}

		private void ReadSecurityInfoEnd(IBSocket socket)
		{
			SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = socket.ReadInt() });
		}

		private void ReadDeltaNuetralValidation(IBSocket socket)
		{
			/* requestId */
			socket.ReadInt();

			//UnderComp underComp = new UnderComp();
			//underComp.ConId = 
			socket.ReadInt();
			//underComp.Delta = 
			socket.ReadDecimal();
			//underComp.Price = 
			socket.ReadDecimal();

			//deltaNuetralValidation(reqId, underComp);
		}

		private void ReadTickSnapshotEnd(IBSocket socket)
		{
			/*var requestId = */socket.ReadInt();
			//SendOutMessage(_level1Messages.GetAndRemove(requestId));
		}

		private void ReadMarketDataType(IBSocket socket)
		{
			/* requestId */
			socket.ReadInt();

			SessionHolder.IsRealTimeMarketData = socket.ReadBool();

			//marketDataType(reqId, mdt);
		}

		private void ReadFinancialAdvice(IBSocket socket)
		{
			var type = socket.ReadInt();
			var xml = socket.ReadStr();

			SendOutMessage(new FinancialAdviseMessage
			{
				AdviseType = type,
				Data = xml
			});
		}

		private void OnProcessMarketDataResponse(IBSocket socket, ResponseMessages message, ServerVersions version)
		{
			switch (message)
			{
				case ResponseMessages.TickPrice:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/tickprice.htm
					ReadTickPrice(socket, version);
					break;
				}
				case ResponseMessages.TickVolume:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/ticksize.htm
					ReadTickVolume(socket);
					break;
				}
				case ResponseMessages.TickOptionComputation:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/tickoptioncomputation.htm
					ReadTickOptionComputation(socket, version);
					break;
				}
				case ResponseMessages.TickGeneric:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/tickgeneric.htm
					ReadTickGeneric(socket);
					break;
				}
				case ResponseMessages.TickString:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/tickstring.htm
					ReadTickString(socket);
					break;
				}
				case ResponseMessages.TickEfp:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/tickefp.htm
					ReadTickEfp(socket);
					break;
				}
				case ResponseMessages.ScannerData:
				{
					ReadScannerData(socket, version);
					break;
				}
				case ResponseMessages.SecurityInfo:
				{
					ReadSecurityInfo(socket, version);
					break;
				}
				case ResponseMessages.BondInfo:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/bondcontractdetails.htm
					ReadBondInfo(socket, version);
					break;
				}
				case ResponseMessages.MarketDepth:
				case ResponseMessages.MarketDepthL2:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/updatemktdepth.htm
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/updatemktdepthl2.htm
					ReadMarketDepth(socket, message);
					break;
				}
				case ResponseMessages.NewsBulletins:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/updatenewsbulletin.htm
					ReadNewsBulletins(socket);
					break;
				}
				case ResponseMessages.FinancialAdvice:
				{
					ReadFinancialAdvice(socket);
					break;
				}
				case ResponseMessages.HistoricalData:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/historicaldata.htm
					ReadHistoricalData(socket, version);
					break;
				}
				case ResponseMessages.ScannerParameters:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/scannerparameters.htm
					ReadScannerParameters(socket);
					break;
				}
				case ResponseMessages.RealTimeBars:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/realtimebar.htm
					ReadRealTimeBars(socket);
					break;
				}
				case ResponseMessages.FundamentalData:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/fundamentaldata.htm
					ReadFundamentalData(socket);
					break;
				}
				case ResponseMessages.SecurityInfoEnd:
				{
					ReadSecurityInfoEnd(socket);
					break;
				}
				case ResponseMessages.DeltaNuetralValidation:
				{
					ReadDeltaNuetralValidation(socket);
					break;
				}
				case ResponseMessages.TickSnapshotEnd:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/ticksnapshotend.htm
					ReadTickSnapshotEnd(socket);
					break;
				}
				case ResponseMessages.MarketDataType:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/marketdatatype.htm
					ReadMarketDataType(socket);
					break;
				}
			}
		}

		private void OnProcessMarketDataError(string message)
		{
			SendOutError(message);
		}

		private void OnProcessSecurityLookupNoFound(int transactionId)
		{
			SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = transactionId });
		}
	}
}