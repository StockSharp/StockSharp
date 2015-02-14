namespace StockSharp.LMAX
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Net;
	using System.Text;

	using Com.Lmax.Api.MarketData;
	using Com.Lmax.Api.OrderBook;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class LmaxMessageAdapter
	{
		private static readonly string[] _indexCodes =
		{
			"WS30",
			"STOXX50E",
			"FCHI",
			"UK100",
			"GDAXI",
			"J225",
			"SPX",
			"NDX"
		};

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		private void ProcessSecurityLookupMessage(SecurityLookupMessage lookupMsg)
		{
			if (lookupMsg.GetValue<bool>("FromSite"))
			{
				using (var client = new WebClient())
				{
					var rows = client.DownloadString("http://www.lmax.com/doc/LMAX-Instruments.csv").Split("\n");

					foreach (var row in rows.Skip(1))
					{
						var cells = row.Split(',');

						var secCode = cells[2];

						// иногда файл с багами, и там есть пустая строчка с запятыми
						if (cells[2].IsEmpty())
							continue;

						var secName = cells[0];

						SecurityTypes securityType;

						var volumeStep = 0m;

						if (_indexCodes.Contains(secCode, StringComparer.InvariantCultureIgnoreCase))
							securityType = SecurityTypes.Index;
						else if (
							secName.ContainsIgnoreCase("brent") ||
							secName.ContainsIgnoreCase("gasoil") ||
							secName.ContainsIgnoreCase("crude")
						)
							securityType = SecurityTypes.Commodity;
						else
						{
							securityType = SecurityTypes.Currency;

							if (!secCode.StartsWith("XA", StringComparison.InvariantCultureIgnoreCase))
								volumeStep = 0.1m;
						}

						SendOutMessage(new SecurityMessage
						{
							SecurityId = new SecurityId
							{
								SecurityCode = secCode,
								BoardCode = ExchangeBoard.Lmax.Code,
								Native = cells[1].To<long>(),
							},
							SecurityType = securityType,
							VolumeStep = volumeStep,
							Name = secName,
							PriceStep = cells[4].To<decimal>(),
							//security.MinStepPrice = cells[5].To<decimal>(),
							Currency = cells[8].To<CurrencyTypes>(),
							ExpiryDate = cells[7].IsEmpty() ? (DateTime?)null : cells[7].ToDateTime("dd/MM/yyyy HH:mm"),
							OriginalTransactionId = lookupMsg.TransactionId
						});
					}
				}
			}
			else
				SearchSecurities(lookupMsg.SecurityId.SecurityCode, lookupMsg.TransactionId, new List<Instrument>(), true);
		}

		private void SearchSecurities(string secCode, long transactionId, ICollection<Instrument> instruments, bool hasMoreResults)
		{
			if (secCode.IsEmpty())
				throw new ArgumentNullException("secCode", LocalizedStrings.Str3391);

			if (instruments == null)
				throw new ArgumentNullException("instruments");

			foreach (var instrument in instruments)
			{
				SecurityTypes type;
				var typeName = instrument.Underlying.AssetClass.ToUpperInvariant();
				switch (typeName)
				{
					case "COMMODITY":
						type = SecurityTypes.Commodity;
						break;
					case "CURRENCY":
						type = SecurityTypes.Currency;
						break;
					case "INDEX":
						type = SecurityTypes.Index;
						break;
					default:
						SendOutError(LocalizedStrings.Str2140Params.Put(typeName));
						continue;
				}

				var securityId = new SecurityId
				{
					SecurityCode = instrument.Underlying.Symbol,
					BoardCode = ExchangeBoard.Lmax.Code,
					Native = instrument.Id
				};

				SendOutMessage(new SecurityMessage
				{
					SecurityId = securityId,
					Name = instrument.Name,
					PriceStep = instrument.OrderBook.PriceIncrement,
					VolumeStep = instrument.OrderBook.QuantityIncrement,
					Currency = instrument.Contract.Currency.To<CurrencyTypes>(),
					ExpiryDate = instrument.Calendar.ExpiryTime,
					SecurityType = type,
					OriginalTransactionId = transactionId,
				});

				SendOutMessage(
					new Level1ChangeMessage
					{
						SecurityId = securityId,
						ServerTime = SessionHolder.CurrentTime.Convert(TimeZoneInfo.Utc),
					}
					.TryAdd(Level1Fields.StepPrice, instrument.Contract.UnitPrice));
			}

			if (hasMoreResults)
				Session.SearchInstruments(new SearchRequest(secCode, instruments.Count), (i, h) => SearchSecurities(secCode, transactionId, i, h), CreateErrorHandler("SearchInstruments"));
			else
				SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = transactionId });
		}

		private void ProcessMarketDataMessage(MarketDataMessage mdMsg)
		{
			if (!mdMsg.IsSubscribe)
				return;

			if (mdMsg.SecurityId.Native == null)
				throw new InvalidOperationException(LocalizedStrings.Str3392Params.Put(mdMsg.SecurityId.SecurityCode));

			var lmaxId = (long)mdMsg.SecurityId.Native;

			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					Session.Subscribe(new OrderBookStatusSubscriptionRequest(lmaxId), () => { }, CreateErrorHandler("OrderBookStatusSubscriptionRequest"));
					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					Session.Subscribe(new OrderBookSubscriptionRequest(lmaxId), () => { }, CreateErrorHandler("OrderBookSubscriptionRequest"));
					break;
				}
				case MarketDataTypes.CandleTimeFrame:
				{
					IHistoricMarketDataRequest request;

					var tf = (TimeSpan)mdMsg.Arg;

					if (tf.Ticks == 1)
						request = new TopOfBookHistoricMarketDataRequest(mdMsg.TransactionId, lmaxId, mdMsg.From.UtcDateTime, mdMsg.To.UtcDateTime, Format.Csv);
					else
					{
						Resolution resolution;

						if (tf == TimeSpan.FromMinutes(1))
							resolution = Resolution.Minute;
						else if (tf == TimeSpan.FromDays(1))
							resolution = Resolution.Day;
						else
							throw new InvalidOperationException(LocalizedStrings.Str3393Params.Put(tf));

						request = new AggregateHistoricMarketDataRequest(mdMsg.TransactionId, lmaxId, mdMsg.From.UtcDateTime, mdMsg.To.UtcDateTime, resolution, Format.Csv, Option.Bid, Option.Ask);
					}

					Session.RequestHistoricMarketData(request, () => { }, CreateErrorHandler("RequestHistoricMarketData"));
					break;
				}
				case MarketDataTypes.Trades:
					break;
				default:
					throw new ArgumentOutOfRangeException("mdMsg", mdMsg.DataType, LocalizedStrings.Str1618);
			}

			var result = (MarketDataMessage)mdMsg.Clone();
			result.OriginalTransactionId = mdMsg.TransactionId;
			SendOutMessage(result);
		}

		private void OnSessionOrderBookStatusChanged(OrderBookStatusEvent orderBookStatusEvent)
		{
			SecurityStates state;

			switch (orderBookStatusEvent.Status)
			{
				case OrderBookStatus.New:
				case OrderBookStatus.Opened:
					state = SecurityStates.Trading;
					break;
				case OrderBookStatus.Suspended:
				case OrderBookStatus.Closed:
				case OrderBookStatus.Settled:
				case OrderBookStatus.Withdrawn:
					state = SecurityStates.Stoped;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendOutMessage(
				new Level1ChangeMessage
				{
					SecurityId = new SecurityId { Native = orderBookStatusEvent.InstrumentId },
					ServerTime = SessionHolder.CurrentTime.Convert(TimeZoneInfo.Utc),
				}
				.Add(Level1Fields.State, state));
		}

		private void OnSessionMarketDataChanged(OrderBookEvent orderBookEvent)
		{
			var time = TimeHelper.GregorianStart.AddMilliseconds(orderBookEvent.Timestamp).ApplyTimeZone(TimeZoneInfo.Utc);
			var secId = new SecurityId { Native = orderBookEvent.InstrumentId };

			var l1Msg = new Level1ChangeMessage
			{
				ServerTime = time,
				SecurityId = secId,
			};

			if (orderBookEvent.HasMarketClosePrice)
				l1Msg.Add(Level1Fields.ClosePrice, orderBookEvent.MktClosePrice);

			if (orderBookEvent.HasDailyHighestTradedPrice)
				l1Msg.Add(Level1Fields.HighPrice, orderBookEvent.DailyHighestTradedPrice);

			if (orderBookEvent.HasDailyLowestTradedPrice)
				l1Msg.Add(Level1Fields.LowPrice, orderBookEvent.DailyLowestTradedPrice);

			if (orderBookEvent.HasLastTradedPrice)
			{
				l1Msg.Add(Level1Fields.LastTradePrice, orderBookEvent.LastTradedPrice);
			}

			SendOutMessage(l1Msg);

			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = secId,
				Bids = orderBookEvent.BidPrices.Select(p => new QuoteChange(Sides.Buy, p.Price, p.Quantity)),
				Asks = orderBookEvent.AskPrices.Select(p => new QuoteChange(Sides.Sell, p.Price, p.Quantity)),
				ServerTime = time
			});
		}

		private void OnSessionHistoricMarketDataReceived(string instructionId, List<Uri> uris)
		{
			var transactionId = TryParseTransactionId(instructionId);

			if (transactionId == null)
				return;

			foreach (var uri in uris)
			{
				Session.OpenUri(uri, (u, reader) =>
				{
					using (var stream = new StreamReader(new GZipStream(reader.BaseStream, CompressionMode.Decompress), Encoding.UTF8))
					{
						var rows = stream.ReadToEnd().Split("\n");

						var index = 0;

						CultureInfo.InvariantCulture.DoInCulture(() =>
							rows
								.Skip(1)
								.Select(row => row.Split(','))
								.Where(cells => !cells[1].IsEmpty())
								.ForEach(cells =>
								{
									var message = new TimeFrameCandleMessage
									{
										OriginalTransactionId = transactionId.Value,
										OpenTime = TimeHelper.GregorianStart.AddMilliseconds(cells[0].To<long>()),
										IsFinished = index++ == (rows.Length - 2),
										State = CandleStates.Finished,
									};

									if (cells.Length == 5)
									{
										message.OpenPrice = cells[1].To<decimal>();
										message.OpenVolume = !cells[2].IsEmpty() ? cells[2].To<decimal>() : 0;
										message.ClosePrice = cells[3].To<decimal>();
										message.CloseVolume = !cells[4].IsEmpty() ? cells[4].To<decimal>() : 0;
									}
									else
									{
										message.OpenPrice = cells[1].To<decimal>();
										message.HighPrice = cells[2].To<decimal>();
										message.LowPrice = cells[3].To<decimal>();
										message.ClosePrice = cells[4].To<decimal>();
										message.TotalVolume = cells[5].To<decimal>() + cells[6].To<decimal>() + cells[7].To<decimal>();
									}

									SendOutMessage(message);
								}));
					}
				}, CreateErrorHandler("OpenUri"));

				System.Threading.Thread.Sleep(3000);
			}
		}

		private void OnSessionEventStreamFailed(Exception exception)
		{
			SendOutError(exception);
		}
	}
}