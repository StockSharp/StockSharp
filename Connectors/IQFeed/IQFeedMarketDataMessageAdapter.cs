#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.IQFeed.IQFeed
File: IQFeedMarketDataMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.IQFeed
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The messages adapter for IQFeed.
	/// </summary>
	public partial class IQFeedMarketDataMessageAdapter : MessageAdapter
	{
		private readonly SynchronizedDictionary<long, SecurityLookupMessage> _lookupResult = new SynchronizedDictionary<long, SecurityLookupMessage>();

		private readonly SynchronizedDictionary<int, string> _markets = new SynchronizedDictionary<int, string>();
		private readonly SynchronizedDictionary<int, SecurityTypes?> _securityTypes = new SynchronizedDictionary<int, SecurityTypes?>();

		private IQFeedWrapper _level2Feed;
		private IQFeedWrapper _level1Feed;
		private IQFeedWrapper _lookupFeed;
		private IQFeedWrapper _derivativeFeed;

		private readonly Regex _regexError = new Regex(@"^E,([^,]*?),*?$", RegexOptions.Compiled);
		private readonly Regex _regexTime = new Regex("^T,([^,]*?)$", RegexOptions.Compiled);
		private readonly Regex _regexLastMessage = new Regex("^!ENDMSG!,$", RegexOptions.Compiled);
		private readonly Regex _regexRequestId = new Regex("^#(.*?)#,(.*?)$", RegexOptions.Compiled);
		private readonly Regex _regex = new Regex("^(S|n|Z|2|Q|P|F|N),(.+)$", RegexOptions.Compiled);

		private readonly SynchronizedDictionary<long, MessageTypes> _requestsType = new SynchronizedDictionary<long, MessageTypes>();
		private readonly SynchronizedDictionary<long, SecurityId> _secIds = new SynchronizedDictionary<long, SecurityId>();
		private readonly SynchronizedDictionary<long, Tuple<Func<string[], CandleMessage>, object>> _candleParsers = new SynchronizedDictionary<long, Tuple<Func<string[], CandleMessage>, object>>();

		private MessageTypes? _currSystemType;
		private bool _isDownloadSecurityFromSite;

		private readonly SynchronizedDictionary<long, Tuple<string, StringBuilder>> _newsIds = new SynchronizedDictionary<long, Tuple<string, StringBuilder>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="IQFeedMarketDataMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public IQFeedMarketDataMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Level1ColumnRegistry = new IQFeedLevel1ColumnRegistry();

			_level1Columns = new[]
			{
				Level1ColumnRegistry.OpenInterest,
				Level1ColumnRegistry.Open,
				Level1ColumnRegistry.High,
				Level1ColumnRegistry.Low,
				Level1ColumnRegistry.Close,
				Level1ColumnRegistry.BidPrice,
				Level1ColumnRegistry.BidTime,
				Level1ColumnRegistry.BidVolume,
				//Level1ColumnRegistry.BidMarket,
				Level1ColumnRegistry.AskPrice,
				Level1ColumnRegistry.AskTime,
				Level1ColumnRegistry.AskVolume,
				//Level1ColumnRegistry.AskMarket,
				Level1ColumnRegistry.LastTradeId,
				Level1ColumnRegistry.LastDate,
				Level1ColumnRegistry.LastTradeTime,
				Level1ColumnRegistry.LastTradePrice,
				Level1ColumnRegistry.LastTradeVolume,
				//Level1ColumnRegistry.LastTradeMarket,
				Level1ColumnRegistry.TotalVolume,
				Level1ColumnRegistry.TradeCount,
				Level1ColumnRegistry.VWAP,
				Level1ColumnRegistry.DecimalPrecision,
				Level1ColumnRegistry.MarketOpen,
				Level1ColumnRegistry.MessageContents
			};

			this.AddMarketDataSupport();
		}

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return IsDownloadSecurityFromSite; }
		}

		private static void GetCandleParams(MarketDataTypes type, object arg, out string strArg, out string intervalType)
		{
			switch (type)
			{
				case MarketDataTypes.CandleTimeFrame:
				{
					intervalType = "s";
					strArg = arg.To<TimeSpan>().TotalSeconds.To<int>().To<string>();
					break;
				}
				case MarketDataTypes.CandleTick:
				{
					intervalType = "t";
					strArg = arg.To<string>();
					break;
				}
				case MarketDataTypes.CandleVolume:
				{
					intervalType = "v";
					strArg = arg.To<string>();
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.WrongCandleType);
			}
		}

		private void SafeDisconnectFeed(ref IQFeedWrapper feed)
		{
			try
			{
				feed.Disconnect();
				feed = null;
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_requestsType.Clear();
					_secIds.Clear();
					_candleParsers.Clear();
					_newsIds.Clear();

					_lookupResult.Clear();

					_currSystemType = null;

					if (_lookupFeed != null)
						SafeDisconnectFeed(ref _lookupFeed);

					if (_level1Feed != null)
						SafeDisconnectFeed(ref _level1Feed);

					if (_level2Feed != null)
						SafeDisconnectFeed(ref _level2Feed);

					if (_derivativeFeed != null)
						SafeDisconnectFeed(ref _derivativeFeed);

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					_isDownloadSecurityFromSite = IsDownloadSecurityFromSite;

					_lookupFeed = CreateFeed(LookupAddress, "LookupFeed");
					_level1Feed = CreateFeed(Level1Address, "Level1Feed");
					_level2Feed = CreateFeed(Level2Address, "Level2Feed");
					_derivativeFeed = CreateFeed(DerivativeAddress, "DerivativeFeed");

					_level1Feed.SetLevel1FieldSet(new[]
						{
							Level1ColumnRegistry.Symbol,
							Level1ColumnRegistry.ExchangeId,
							Level1ColumnRegistry.LastTradeMarket,
							Level1ColumnRegistry.BidMarket,
							Level1ColumnRegistry.AskMarket
						}
						.Concat(Level1Columns)
						.Select(c => c.Name)
						.ToArray());

					break;
				}

				case MessageTypes.Disconnect:
				{
					SafeDisconnectFeed(ref _lookupFeed);
					SafeDisconnectFeed(ref _level1Feed);
					SafeDisconnectFeed(ref _level2Feed);
					SafeDisconnectFeed(ref _derivativeFeed);

					//_isCommonLookupDone = null;

					SendOutMessage(new DisconnectMessage());
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.Level1:
						case MarketDataTypes.Trades:
						{
							if (mdMsg.To == null)
							{
								if (mdMsg.IsSubscribe)
									_level1Feed.SubscribeSymbol(mdMsg.SecurityId.SecurityCode);
								else
									_level1Feed.UnSubscribeSymbol(mdMsg.SecurityId.SecurityCode);
							}
							else
							{
								if (mdMsg.IsSubscribe)
								{
									_requestsType.Add(mdMsg.TransactionId, MessageTypes.Execution);
									_secIds.Add(mdMsg.TransactionId, mdMsg.SecurityId);

									if (mdMsg.Count != null)
										_lookupFeed.RequestTicks(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, mdMsg.Count.Value);
									else
										_lookupFeed.RequestTicks(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, mdMsg.From.ToEst(), mdMsg.To.ToEst());
								}
							}

							break;
						}
						case MarketDataTypes.MarketDepth:
						{
							if (mdMsg.IsSubscribe)
								_level2Feed.SubscribeSymbol(mdMsg.SecurityId.SecurityCode);
							else
								_level2Feed.UnSubscribeSymbol(mdMsg.SecurityId.SecurityCode);

							break;
						}
						//case MarketDataTypes.Trades:
						//{
						//	if (mdMsg.To == DateTime.MaxValue)
						//	{
						//		if (mdMsg.IsSubscribe)
						//			_level1Feed.SubscribeSymbol(mdMsg.SecurityId.SecurityCode);
						//		else
						//			_level1Feed.UnSubscribeSymbol(mdMsg.SecurityId.SecurityCode);
						//	}
						//	else
						//	{
						//		if (mdMsg.IsSubscribe)
						//		{
						//			_requestsType.Add(mdMsg.TransactionId, MessageTypes.Execution);
						//			_lookupFeed.RequestTrades(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, mdMsg.From, mdMsg.To);
						//		}
						//	}

						//	break;
						//}
						case MarketDataTypes.News:
						{
							if (mdMsg.IsSubscribe)
							{
								if (mdMsg.NewsId.IsEmpty())
								{
									if (mdMsg.From.IsDefault())
										_level1Feed.SubscribeNews();
									else
									{
										_requestsType.Add(mdMsg.TransactionId, MessageTypes.News);
										_lookupFeed.RequestNewsHeadlines(mdMsg.TransactionId, mdMsg.From.ToEst());
									}
								}
								else
								{
									var newsId = mdMsg.NewsId;
									_newsIds.Add(mdMsg.TransactionId, Tuple.Create(newsId, new StringBuilder()));
									_requestsType.Add(mdMsg.TransactionId, ExtendedMessageTypes.NewsStory);
									_lookupFeed.RequestNewsStory(mdMsg.TransactionId, newsId);
								}
							}
							else
								_level1Feed.UnSubscribeNews();

							break;
						}
						case MarketDataTypes.CandleTimeFrame:
						case MarketDataTypes.CandleTick:
						case MarketDataTypes.CandleVolume:
						case MarketDataTypes.CandleRange:
						case MarketDataTypes.CandlePnF:
						case MarketDataTypes.CandleRenko:
						{
							if (mdMsg.IsSubscribe)
							{
								// streaming
								if (mdMsg.To == null && mdMsg.Count == null)
								{
									string strArg, intervalType;
									GetCandleParams(mdMsg.DataType, mdMsg.Arg, out strArg, out intervalType);

									_requestsType.Add(mdMsg.TransactionId, mdMsg.DataType.ToCandleMessageType());
									_secIds.Add(mdMsg.TransactionId, mdMsg.SecurityId);
									_candleParsers.Add(mdMsg.TransactionId, Tuple.Create(_candleStreamingParser, mdMsg.Arg));

									_derivativeFeed.SubscribeCandles(mdMsg.SecurityId.SecurityCode, intervalType, strArg, mdMsg.From.ToEst(), mdMsg.TransactionId);
									break;
								}

								if (mdMsg.Arg is TimeSpan)
								{
									var tf = (TimeSpan)mdMsg.Arg;

									if (tf.Ticks == TimeHelper.TicksPerMonth)
									{
										_requestsType.Add(mdMsg.TransactionId, ExtendedMessageTypes.HistoryExtraDayCandle);
										_secIds.Add(mdMsg.TransactionId, mdMsg.SecurityId);
										_candleParsers.Add(mdMsg.TransactionId, Tuple.Create(_candleParser, mdMsg.Arg));

										var count = mdMsg.Count ?? ExchangeBoard.Associated.GetTimeFrameCount(new Range<DateTimeOffset>(mdMsg.From ?? DateTimeOffset.MinValue, mdMsg.To ?? DateTimeOffset.MaxValue), tf);

										_lookupFeed.RequestMonthlyCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, count);
									}
									else if (tf == TimeSpan.FromDays(7))
									{
										_requestsType.Add(mdMsg.TransactionId, ExtendedMessageTypes.HistoryExtraDayCandle);
										_secIds.Add(mdMsg.TransactionId, mdMsg.SecurityId);
										_candleParsers.Add(mdMsg.TransactionId, Tuple.Create(_candleParser, mdMsg.Arg));

										var count = mdMsg.Count ?? ExchangeBoard.Associated.GetTimeFrameCount(new Range<DateTimeOffset>(mdMsg.From ?? DateTimeOffset.MinValue, mdMsg.To ?? DateTimeOffset.MaxValue), tf);

										_lookupFeed.RequestWeeklyCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, count);
									}
									else if (tf == TimeSpan.FromDays(1))
									{
										_requestsType.Add(mdMsg.TransactionId, ExtendedMessageTypes.HistoryExtraDayCandle);
										_secIds.Add(mdMsg.TransactionId, mdMsg.SecurityId);
										_candleParsers.Add(mdMsg.TransactionId, Tuple.Create(_candleParser, mdMsg.Arg));

										if (mdMsg.Count != null)
											_lookupFeed.RequestDailyCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, mdMsg.Count.Value);
										else
											_lookupFeed.RequestDailyCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, mdMsg.From.ToEst(), mdMsg.To.ToEst());
									}
									else if (tf < TimeSpan.FromDays(1))
									{
										string strArg, intervalType;
										GetCandleParams(mdMsg.DataType, mdMsg.Arg, out strArg, out intervalType);

										_requestsType.Add(mdMsg.TransactionId, mdMsg.DataType.ToCandleMessageType());
										_secIds.Add(mdMsg.TransactionId, mdMsg.SecurityId);
										_candleParsers.Add(mdMsg.TransactionId, Tuple.Create(_candleIntradayParser, mdMsg.Arg));

										//var interval = tf.TotalSeconds.To<int>();

										if (mdMsg.Count != null)
											_lookupFeed.RequestCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, intervalType, strArg, mdMsg.Count.Value);
										else
											_lookupFeed.RequestCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, intervalType, strArg, mdMsg.From.ToEst(), mdMsg.To.ToEst());
									}
									else
									{
										throw new InvalidOperationException(LocalizedStrings.Str2139Params.Put(tf));
									}
								}
								else
								{
									string strArg, intervalType;
									GetCandleParams(mdMsg.DataType, mdMsg.Arg, out strArg, out intervalType);

									if (mdMsg.Count != null)
										_lookupFeed.RequestCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, intervalType, strArg, mdMsg.Count.Value);
									else
										_lookupFeed.RequestCandles(mdMsg.TransactionId, mdMsg.SecurityId.SecurityCode, intervalType, strArg, mdMsg.From.ToEst(), mdMsg.To.ToEst());
								}
							}
							else
							{
								_derivativeFeed.UnSubscribeCandles(mdMsg.SecurityId.SecurityCode, mdMsg.OriginalTransactionId);
							}

							break;
						}
						default:
						{
							SendOutMarketDataNotSupported(mdMsg.TransactionId);
							return;
						}
					}

					var reply = (MarketDataMessage)message.Clone();
					reply.OriginalTransactionId = mdMsg.TransactionId;
					SendOutMessage(reply);

					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;

					var securityTypes = new HashSet<SecurityTypes>();

					if (lookupMsg.SecurityTypes != null)
						securityTypes.AddRange(lookupMsg.SecurityTypes);
					else if (lookupMsg.SecurityType != null)
						securityTypes.Add(lookupMsg.SecurityType.Value);

					if (_isDownloadSecurityFromSite)
					{
						_isDownloadSecurityFromSite = false;

						using (var zip = new ZipArchive(
							SecuritiesFile.IsEmpty()
								? IQFeedHelper.DownloadSymbols().To<Stream>()
								: File.OpenRead(SecuritiesFile)))
						{
							var entry = zip.GetEntry("mktsymbols_v2.txt");

							using (var reader = entry.Open())
							{
								reader
									.ReadLines()
									.Skip(1)
									.Select(line => line.Split('\t'))
									.ForEach(parts =>
									{
										if (parts.Length == 9)
										{
											// mika 2014.09.16
											// below line has incorrect tabulation
											// CS.17.CB	CREDIT SUISSE NEW YORK 1.375% 05/26/17		NYSE	NYSE	BONDS			

											parts = parts.Exclude(2, 1).ToArray();
										}

										var secType = parts[4].ToSecurityType();

										if (secType == null)
											this.AddWarningLog(LocalizedStrings.Str2140Params.Put(parts[4]));

										if (secType != null && !securityTypes.Contains(secType.Value))
											return;

										var secCode = parts[0];
										var secName = parts[0];
										var boardCode = parts[2];

										SendOutMessage(new BoardMessage
										{
											Code = boardCode,
											ExchangeCode = boardCode
										});

										SendOutMessage(new SecurityMessage
										{
											SecurityId = new SecurityId
											{
												SecurityCode = secCode,
												BoardCode = boardCode
											},
											Name = secName,
											SecurityType = secType,
											OriginalTransactionId = lookupMsg.TransactionId
										});
									});
							}
						}

						break;
					}

					var requestedTypes = _securityTypes
						.Where(t => t.Value != null && securityTypes.Contains(t.Value.Value))
						.Select(i => i.Key.To<string>())
						.ToArray();

					_requestsType.Add(lookupMsg.TransactionId, MessageTypes.Security);
					_lookupResult.Add(lookupMsg.TransactionId, lookupMsg);

					var code = lookupMsg.SecurityId.SecurityCode;

					if (code.IsEmpty())
						code = "*";

					_lookupFeed.RequestSecurities(lookupMsg.TransactionId, IQFeedSearchField.Symbol, code, IQFeedFilterType.SecurityType, requestedTypes);
					break;
				}
			}
		}

		private IQFeedWrapper CreateFeed(EndPoint endPoint, string name)
		{
			var feed = new IQFeedWrapper(this, name, endPoint);
			
			feed.ProcessReply += line =>
			{
				IEnumerable<Message> messages;

				try
				{
					messages = ConvertToMessages(feed, line).ToArray();
				}
				catch (Exception ex)
				{
					ex = new InvalidOperationException(LocalizedStrings.Str2141Params.Put(line), ex);
					this.AddErrorLog(ex);
					messages = new[] { new ErrorMessage { Error = ex } };
				}

				foreach (var message in messages)
				{
					ProcessIQFeedMessage(feed, message);
				}
			};
			feed.ConnectionError += err =>
			{
				if (name == "LookupFeed")
					SendOutMessage(new ConnectMessage { Error = err });
				else
					SendOutError(err);

				//feed.Disconnect();
				//feed.Connect();
			};

			feed.Connect();

			return feed;
		}

		private void ProcessIQFeedMessage(IQFeedWrapper feed, Message message)
		{
			switch (message.Type)
			{
				case ExtendedMessageTypes.System:
				{
					var systemMsg = (IQFeedSystemMessage)message;

					switch (systemMsg.Value)
					{
						case "SERVER DISCONNECTED":
						case "SERVER RECONNECT FAILED":
							SendOutMessage(systemMsg);
							break;
						default:
						{
							if (feed == _lookupFeed)
							{
								_currSystemType = ExtendedMessageTypes.ListedMarket;
								_lookupFeed.RequestListedMarkets();
							}

							break;
						}
					}

					break;
				}
				case ExtendedMessageTypes.HistoryExtraDayCandle:

					// расширенное сообщение лучше передавать внешнему коду, чтобы ему хоть как-то получить информацию
					//case ExtendedMessageTypes.Data:

					break;

				case ExtendedMessageTypes.ListedMarket:
				{
					var lmMsg = (IQFeedListedMarketMessage)message;
					_markets[lmMsg.Id] = lmMsg.Code;
					break;
				}

				case ExtendedMessageTypes.SecurityType:
				{
					var stMsg = (IQFeedSecurityTypeMessage)message;

					try
					{
						var secType = stMsg.Code.ToSecurityType();

						if (secType == null)
							this.AddWarningLog(LocalizedStrings.Str2140Params.Put(stMsg.Code));

						_securityTypes[stMsg.Id] = secType;
					}
					catch (Exception ex)
					{
						this.AddErrorLog(ex);
					}

					break;
				}

				case ExtendedMessageTypes.End:
				{
					var type = feed == _lookupFeed ? _currSystemType : null;
					var requestId = 0L;

					if (_currSystemType == null)
					{
						requestId = message.GetRequestId();

						type = _requestsType.TryGetValue2(requestId);

						if (type == null)
							return;

						_requestsType.Remove(requestId);
						_secIds.Remove(requestId);

						if (type == MessageTypes.CandleTimeFrame
							|| type == MessageTypes.CandleVolume
							|| type == MessageTypes.CandleTick
							|| type == ExtendedMessageTypes.HistoryExtraDayCandle)
							_candleParsers.Remove(requestId);
					}

					switch (type)
					{
						case MessageTypes.Security:
						{
							var result = _lookupResult.TryGetValue(requestId);

							if (result == null)
								return;

							_lookupResult.Remove(requestId);

							SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = requestId });

							break;
						}

						case ExtendedMessageTypes.ListedMarket:
						{
							_currSystemType = ExtendedMessageTypes.SecurityType;
							_lookupFeed.RequestSecurityTypes();
							break;
						}

						case ExtendedMessageTypes.SecurityType:
						{
							_currSystemType = null;
							SendOutMessage(new ConnectMessage());
							break;
						}

						// для сделок и свечек отправляем фиктивный объект,
						// чтобы сообщить, что получение исторических данных завершено

						case MessageTypes.Execution:
						{
							SendOutMessage(new Level1ChangeMessage
							{
								ExtensionInfo = new Dictionary<object, object>
								{
									{ "IsFinished", true },
								}
							}.InitRequestId(requestId));
							break;
						}

						case MessageTypes.CandleTimeFrame:
						case MessageTypes.CandleTick:
						case MessageTypes.CandleVolume:
						case ExtendedMessageTypes.HistoryExtraDayCandle:
						{
							SendOutMessage(new TimeFrameCandleMessage
							{
								IsFinished = true,
								OriginalTransactionId = requestId,
							});
							break;
						}
					}

					break;
				}

				//case MessageTypes.Security:
				//{
				//	var secMsg = (SecurityMessage)message;

				//	var exchangeRoot = message.GetValue<string>("ExchangeRoot");

				//	switch (secMsg.SecurityType)
				//	{
				//		case SecurityTypes.Future:
				//			secMsg.ExpiryDate = message.GetValue<DateTime>("ExpiryDate");
				//			secMsg.UnderlyingSecurityCode = exchangeRoot;
				//			break;

				//		case SecurityTypes.Option:
				//			secMsg.ExpiryDate = message.GetValue<DateTime>("ExpiryDate");
				//			secMsg.UnderlyingSecurityCode = exchangeRoot;
				//			secMsg.Strike = message.GetValue<decimal>("Strike");
				//			break;
				//	}

				//	SendOutMessage(secMsg);
				//	break;
				//}

				default:
					SendOutMessage(message);
					break;
			}
		}

		private IEnumerable<Message> ConvertToMessages(IQFeedWrapper feed, string line)
		{
			//Market Depth & NASDAQ Level 2 via TCP/IP
			//This is a deprecated message and only sent for backwards compatability
			if (line.CompareIgnoreCase("O"))
				return Enumerable.Empty<Message>();

			long requestId = 0;

			var match = _regexRequestId.Match(line);
			if (match.Success)
			{
				requestId = match.Groups[1].Value.To<long>();
				line = match.Groups[2].Value;
			}

			match = _regexError.Match(line);

			if (match.Success)
			{
				var text = match.Groups[1].Value;

				return text.Equals("!NO_DATA!")
					? Enumerable.Empty<Message>()
					: ToMessages(feed, text, MessageTypes.Error, requestId);
			}

			match = _regexTime.Match(line);

			if (match.Success)
				return ToMessages(feed, match.Groups[1].Value, MessageTypes.Time, requestId);

			match = _regexLastMessage.Match(line);

			if (match.Success)
				return ToMessages(feed, match.Groups[1].Value, ExtendedMessageTypes.End, requestId);

			MessageTypes type;

			if (feed == _lookupFeed && _currSystemType != null)
				type = _currSystemType.Value;
			else
			{
				match = _regex.Match(line);

				if (match.Success)
				{
					var cmd = match.Groups[1].Value;
					line = match.Groups[2].Value;

					switch (cmd)
					{
						case "S":
							type = ExtendedMessageTypes.System;
							break;
						case "n":
							type = MessageTypes.Error;
							line = LocalizedStrings.Str704Params.Put(line);
							break;
						case "Z":
						case "2":
							type = MessageTypes.QuoteChange;
							break;
						case "F":
							type = ExtendedMessageTypes.Fundamental;
							break;
						case "P":
						case "Q":
							type = MessageTypes.Level1Change;
							break;
						case "N":
							type = MessageTypes.News;
							break;
						default:
							if (!_requestsType.TryGetValue(requestId, out type))
								type = ExtendedMessageTypes.Data;

							break;
					}
				}
				else if (!_requestsType.TryGetValue(requestId, out type))
					type = ExtendedMessageTypes.Data;
			}

			return ToMessages(feed, line, type, requestId);
		}

		private IEnumerable<Message> ToMessages(IQFeedWrapper feed, string str, MessageTypes type, long requestId)
		{
			var messages = ParseMessages(feed, str, type, requestId);

			if (requestId == 0)
				return messages;

			return messages.Select(m =>
			{
				m.InitRequestId(requestId);
				return m;
			});
		}

		private IEnumerable<Message> ParseMessages(IQFeedWrapper feed, string str, MessageTypes type, long requestId)
		{
			switch (type)
			{
				case ExtendedMessageTypes.System:
					yield return new IQFeedSystemMessage(feed, str);
					break;
				case ExtendedMessageTypes.SecurityType:
				{
					var parts = str.SplitByComma();
					yield return new IQFeedSecurityTypeMessage(parts[0].To<int>(), parts[1], parts[2]);
					break;
				}
				case ExtendedMessageTypes.ListedMarket:
				{
					var parts = str.SplitByComma();
					yield return new IQFeedListedMarketMessage(parts[0].To<int>(), parts[1], parts[2]);
					break;
				}
				case ExtendedMessageTypes.Data:
					yield return new IQFeedDataMessage(str);
					break;
				case ExtendedMessageTypes.End:
					yield return new IQFeedEndMessage();
					break;
				case MessageTypes.Time:
					yield return new TimeMessage { ServerTime = str.ToDateTime("yyyyMMdd HH:mm:ss").ApplyTimeZone(TimeHelper.Est) };
					break;
				case MessageTypes.Security:
				{
					var parts = str.SplitByComma();

					yield return new SecurityMessage
					{
						SecurityId = CreateSecurityId(parts[0], parts[1].To<int>()),
						Name = parts[3],
						OriginalTransactionId = requestId,
						SecurityType = _securityTypes[parts[2].To<int>()],
					};

					break;
				}
				
				case ExtendedMessageTypes.Fundamental:
				{
					foreach (var result in ToSecurityFundamentalMessages(str))
						yield return result;

					break;
				}
				
				case MessageTypes.Level1Change:
				{
					foreach (var result in ToSecurityUpdateMessage(str))
						yield return result;

					break;
				}
				
				case MessageTypes.Execution:
				{
					yield return ToLevel1(str, _secIds[requestId]);
					break;
				}

				case MessageTypes.News:
					yield return ToNewsMessage(str);
					break;

				case ExtendedMessageTypes.NewsStory:
				{
					var tuple = _newsIds[requestId];

					if (str.IsEmpty())
					{
						tuple.Item2.AppendLine();
						break;
					}

					tuple.Item2.Append(str.Replace("<BEGIN>", string.Empty).Replace("<END>", string.Empty));

					if (str.EndsWith("<END>"))
					{
						_newsIds.Remove(requestId);

						yield return new NewsMessage
						{
							Id = tuple.Item1,
							Story = tuple.Item2.ToString(),
							ServerTime = CurrentTime.Convert(TimeHelper.Est)
						};
					}

					break;
				}

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				case ExtendedMessageTypes.HistoryExtraDayCandle:
				{
					var parts = str.SplitByComma();

					var tuple = _candleParsers[requestId];

					var candleMsg = tuple.Item1(parts);

					candleMsg.OriginalTransactionId = requestId;
					candleMsg.SecurityId = _secIds[requestId];

					if (tuple.Item2 is TimeSpan)
					{
						var tf = (TimeSpan)tuple.Item2;

						if (tf == TimeSpan.FromDays(1))
						{
							candleMsg.OpenTime = candleMsg.CloseTime;
							candleMsg.CloseTime = candleMsg.OpenTime.EndOfDay();
						}
						else// if (tf == TimeSpan.FromDays(7) || tf.Ticks == TimeHelper.TicksPerMonth)
						{
							candleMsg.CloseTime -= TimeSpan.FromTicks(1);
							candleMsg.OpenTime = tf.GetCandleBounds(candleMsg.CloseTime).Min;
						}
					}
					else
						candleMsg.OpenTime = candleMsg.CloseTime;
					
					yield return candleMsg;
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var l1Msg = ToLevel2(str);

					if (l1Msg != null)
						yield return l1Msg;

					break;
				}

				case MessageTypes.Error:
					yield return str.ToErrorMessage();
					break;

				default:
					throw new InvalidOperationException(LocalizedStrings.Str2142Params.Put(type));
			}
		}
	}
}