#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BarChart.BarChart
File: BarChartMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BarChart
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;
	using System.Xml.Linq;

	using ddfplus;
	using ddfplus.HistoricalData;
	using ddfplus.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Web;

	using StockSharp.Algo;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The message adapter for BarChart.
	/// </summary>
	[Icon("BarChart_logo.png")]
	// TODO
	//[Doc("")]
	[DisplayName("BarChart")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "BarChart")]
	public class BarChartMessageAdapter : MessageAdapter
	{
		private string _streamAddress;
		private string _historicalAddress;
		private string _extrasAddress;
		private string _newsAddress;
		private const string _defaultTimeFormatRequest = "yyyyMMddHHmmss";

		private Client _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="BarChartMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public BarChartMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
		}

		/// <summary>
		/// Login.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(1)]
		public string Login { get; set; }

		/// <summary>
		/// Password.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(2)]
		public SecureString Password { get; set; }

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromMinutes(1),
			TimeSpan.FromDays(1),
		});

		/// <summary>
		/// Available time frames.
		/// </summary>
		[Browsable(false)]
		public static IEnumerable<TimeSpan> TimeFrames
		{
			get { return _timeFrames; }
		}

		private void DisposeClient()
		{
			_client.Error -= ClientOnError;
			_client.NewBookQuote -= ClientOnNewBookQuote;
			_client.NewOHLCQuote -= ClientOnNewOhlcQuote;
			_client.NewQuote -= ClientOnNewQuote;

			_client = null;
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
					if (_client != null)
					{
						DisposeClient();
					}

					try
					{
						Connection.Close();
						Connection.ClearCache();
					}
					catch (Exception ex)
					{
						SendOutError(ex);
					}

					Connection.StatusChange -= ConnectionOnStatusChange;

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_client != null)
						throw new InvalidOperationException(LocalizedStrings.Str3378);

					var doc = XDocument.Load("http://www.ddfplus.com/getusersettings.php?username={0}&password={1}".Put(Login, Password.To<string>()));

					var loginElem = doc.Element("usersettings").Elements("login").First();

					if (loginElem.GetAttributeValue<string>("status") != "ok")
						throw new InvalidOperationException(LocalizedStrings.UnknownServerError);

					if (loginElem.GetAttributeValue<string>("credentials") != "ok")
						throw new InvalidOperationException(LocalizedStrings.Str3350);

					foreach (var elem in doc.Element("usersettings").Element("servers").Elements())
					{
						switch (elem.GetAttributeValue<string>("type"))
						{
							case "stream":
								_streamAddress = elem.GetAttributeValue<string>("primary");
								break;

							case "historicalv2":
								_historicalAddress = elem.GetAttributeValue<string>("primary");
								break;

							case "extras":
								_extrasAddress = elem.GetAttributeValue<string>("primary");
								break;

							case "news":
								_newsAddress = elem.GetAttributeValue<string>("primary");
								break;
						}
					}

					SendOutMessage(new ConnectMessage());

					Connection.StatusChange += ConnectionOnStatusChange;
					Connection.Properties["streamingversion"] = "3";

					_client = new Client();

					_client.Error += ClientOnError;
					_client.NewBookQuote += ClientOnNewBookQuote;
					_client.NewOHLCQuote += ClientOnNewOhlcQuote;
					_client.NewQuote += ClientOnNewQuote;

					Connection.Username = Login;
					Connection.Password = Password.To<string>();
					Connection.Mode = ConnectionMode.TCPClient;

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					DisposeClient();
					Connection.Close();

					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;

					XDocument doc = null;

					if (!lookupMsg.SecurityId.SecurityCode.IsEmpty())
					{
						doc = XDocument.Load("{0}/instruments/?lookup={1}".Put(_extrasAddress, lookupMsg.SecurityId.SecurityCode));
					}
					else if (!lookupMsg.SecurityId.BoardCode.IsEmpty())
					{
						doc = XDocument.Load("{0}/instruments/?exchange={1}".Put(_extrasAddress, lookupMsg.SecurityId.BoardCode));
					}

					if (doc != null)
					{
						foreach (var element in doc.Element("instruments").Elements())
						{
							SendOutMessage(new SecurityMessage
							{
								SecurityId = new SecurityId
								{
									SecurityCode = element.GetAttributeValue<string>("guid"),
									BoardCode = element.GetAttributeValue<string>("exchange"),
								},
								Name = element.GetAttributeValue<string>("symbol_description"),
								OriginalTransactionId = lookupMsg.TransactionId,
								SecurityType = TraderHelper.FromIso10962(element.GetAttributeValue<string>("symbol_cfi")),
								PriceStep = element.GetAttributeValue<decimal?>("tick_increment"),
								Multiplier = element.GetAttributeValue<decimal?>("point_value"),
								Currency = element.GetAttributeValue<CurrencyTypes?>("currency")
							});
						}
					}

					SendOutMessage(new SecurityLookupResultMessage
					{
						OriginalTransactionId = lookupMsg.TransactionId,
					});

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.Level1:
							break;
						case MarketDataTypes.MarketDepth:
							break;
						case MarketDataTypes.Trades:
						{
							if (mdMsg.Count != null || mdMsg.From != null || mdMsg.To != null)
							{
								var url = new Url("{0}/queryticks.ashx".Put(_historicalAddress));

								url.QueryString
									.Append("username", Login)
									.Append("password", Password.To<string>())
									.Append("symbol", mdMsg.SecurityId.SecurityCode)
									.Append("order", "asc");

								if (mdMsg.Count != null)
									url.QueryString.Append("maxrecords", mdMsg.Count.Value);

								if (mdMsg.From != null)
									url.QueryString.Append("start", mdMsg.From.Value.FromDateTimeOffset(_defaultTimeFormatRequest));

								if (mdMsg.To != null)
									url.QueryString.Append("end", mdMsg.To.Value.FromDateTimeOffset(_defaultTimeFormatRequest));

								using (var client = new WebClient())
								{
									var lines = client.DownloadString(url)
										.Split("\n")
										.Where(l => l != "\r")
										.ToArray();

									var i = 0;
									foreach (var line in lines)
									{
										var columns = line.Split(',');

										try
										{
											var msg = new ExecutionMessage
											{
												SecurityId = mdMsg.SecurityId,
												OriginalTransactionId = mdMsg.TransactionId,
												ExecutionType = ExecutionTypes.Tick,
												ServerTime = columns[0].ToDateTime("yyyy-MM-dd HH:mm:ss.fff"),
												TradePrice = columns[3].To<decimal>(),
												TradeVolume = columns[4].To<decimal>(),
											};

											msg.AddValue("IsFinished", ++i == lines.Length);

											SendOutMessage(msg);
										}
										catch (Exception ex)
										{
											throw new InvalidOperationException(LocalizedStrings.Str2141Params.Put(line), ex);
										}
									}
								}
							}

							break;
						}
						case MarketDataTypes.News:
							break;
						case MarketDataTypes.CandleTimeFrame:
						{
							var tf = (TimeSpan)mdMsg.Arg;

							string serviceName;
							string timeFormatRequest = _defaultTimeFormatRequest;
							string timeFormatResponse = "yyyy-MM-dd";

							if (tf == TimeSpan.FromMinutes(1))
							{
								serviceName = "queryminutes";
								timeFormatResponse = "yyyy-MM-dd HH:mm";
							}
							else if (tf == TimeSpan.FromDays(1))
							{
								serviceName = "queryeod";
								timeFormatRequest = "yyyyMMdd";
							}
							else
								throw new InvalidOperationException(LocalizedStrings.Str2102);

							var url = new Url("{0}/{1}.ashx".Put(_historicalAddress, serviceName));

							url.QueryString
								.Append("username", Login)
								.Append("password", Password.To<string>())
								.Append("symbol", mdMsg.SecurityId.SecurityCode)
								.Append("order", "asc");

							if (mdMsg.Count != null)
								url.QueryString.Append("maxrecords", mdMsg.Count.Value);

							if (mdMsg.From != null)
								url.QueryString.Append("start", mdMsg.From.Value.FromDateTimeOffset(timeFormatRequest));

							if (mdMsg.To != null)
								url.QueryString.Append("end", mdMsg.To.Value.FromDateTimeOffset(timeFormatRequest));

							using (var client = new WebClient())
							{
								var lines = client.DownloadString(url)
										.Split("\n")
										.Where(l => l != "\r")
										.ToArray();

								var i = 0;
								foreach (var line in lines)
								{
									var columns = line.Split(',');

									try
									{
										SendOutMessage(new TimeFrameCandleMessage
										{
											SecurityId = mdMsg.SecurityId,
											OriginalTransactionId = mdMsg.TransactionId,
											OpenTime = columns[tf == TimeSpan.FromMinutes(1) ? 0 : 1].ToDateTime(timeFormatResponse).ApplyTimeZone(TimeHelper.Est),
											OpenPrice = columns[2].To<decimal>(),
											HighPrice = columns[3].To<decimal>(),
											LowPrice = columns[4].To<decimal>(),
											ClosePrice = columns[5].To<decimal>(),
											TotalVolume = columns[6].To<decimal>(),
											OpenInterest = columns.Length > 7 ? columns[7].To<decimal>() : (decimal?)null,
											IsFinished = ++i == lines.Length
										});
									}
									catch (Exception ex)
									{
										throw new InvalidOperationException(LocalizedStrings.Str2141Params.Put(line), ex);
									}
								}
							}

							break;
						}
						default:
						{
							SendOutMarketDataNotSupported(mdMsg.TransactionId);
							return;
						}
					}

					SendOutMessage(new MarketDataMessage { OriginalTransactionId = mdMsg.TransactionId });
					break;
				}
			}
		}

		private void ClientOnNewQuote(object sender, Client.NewQuoteEventArgs e)
		{
			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = e.Quote.Symbol,
					BoardCode = e.Quote.ExchangeCode
				},
				ServerTime = e.Quote.Timestamp.ApplyTimeZone(TimeHelper.Est)
			}
			.TryAdd(Level1Fields.BestBidPrice, e.Quote.Bid.ToDecimal())
			.TryAdd(Level1Fields.BestBidVolume, (decimal)e.Quote.BidSize)
			.TryAdd(Level1Fields.BestAskPrice, e.Quote.Ask.ToDecimal())
			.TryAdd(Level1Fields.BestAskVolume, (decimal)e.Quote.AskSize));
		}

		private void ClientOnNewOhlcQuote(object sender, Client.NewOHLCQuoteEventArgs e)
		{
			SendOutMessage(new TimeFrameCandleMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = e.OHLCQuote.Symbol,
					BoardCode = AssociatedBoardCode,
				},
				OpenTime = e.OHLCQuote.Timestamp.ApplyTimeZone(TimeHelper.Est),
				OpenPrice = e.OHLCQuote.Open.ToDecimal() ?? 0,
				HighPrice = e.OHLCQuote.High.ToDecimal() ?? 0,
				LowPrice = e.OHLCQuote.Low.ToDecimal() ?? 0,
				ClosePrice = e.OHLCQuote.Close.ToDecimal() ?? 0,
				TotalVolume = e.OHLCQuote.Volume,
				OpenInterest = e.OHLCQuote.OpenInterest
			});
		}

		private void ClientOnNewBookQuote(object sender, Client.NewBookQuoteEventArgs e)
		{
			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = e.BookQuote.Symbol,
					BoardCode = AssociatedBoardCode
				},
				ServerTime = e.BookQuote.Timestamp.ApplyTimeZone(TimeHelper.Est),
				Bids = e.BookQuote.BidPrices.Select((p, i) => new QuoteChange(Sides.Buy, p.ToDecimal() ?? 0, e.BookQuote.BidSizes[i])).ToArray(),
				Asks = e.BookQuote.AskPrices.Select((p, i) => new QuoteChange(Sides.Sell, p.ToDecimal() ?? 0, e.BookQuote.AskSizes[i])).ToArray()
			});
		}

		private void ClientOnError(object sender, Client.ErrorEventArgs e)
		{
			SendOutError(LocalizedStrings.Str1701Params.Put(e.Error, e.Description));
		}

		private void ConnectionOnStatusChange(object sender, StatusChangeEventArgs e)
		{
			switch (e.NewStatus)
			{
				case Status.Disconnected:
					SendOutMessage(new DisconnectMessage());
					break;
				//case Status.Connected:
				//	SendOutMessage(new ConnectMessage());
				//	break;
				case Status.Connecting:
				case Status.Disconnecting:
				case Status.Retrying:
					break;
				case Status.Error:
					SendOutMessage(new ConnectMessage { Error = new InvalidOperationException(LocalizedStrings.Str2959) });
					break;
				default:
					SendOutError(LocalizedStrings.Str1838Params.Put(e.NewStatus));
					break;
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Login), Login);
			storage.SetValue(nameof(Password), Password);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Login = storage.GetValue<string>(nameof(Login));
			Password = storage.GetValue<SecureString>(nameof(Password));
		}
	}
}