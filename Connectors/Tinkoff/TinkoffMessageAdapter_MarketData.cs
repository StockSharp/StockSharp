namespace StockSharp.Tinkoff;

using System.IO;
using System.Net.Http;
using System.Text;

using Ecng.IO;

using Google.Protobuf.Collections;

public partial class TinkoffMessageAdapter
{
	private readonly SynchronizedPairSet<long, (DataType dt, string uid)> _mdTransIds = [];
	private readonly CachedSynchronizedPairSet<long, MarketDataMessage> _mdSubs = [];
	private HttpClient _historyClient;

	private void AddTransId(MarketDataMessage mdMsg)
	{
		if (mdMsg is null)
			throw new ArgumentNullException(nameof(mdMsg));

		_mdTransIds.Add(mdMsg.TransactionId, (mdMsg.DataType2, mdMsg.GetInstrumentId()));
		_mdSubs.Add(mdMsg.TransactionId, mdMsg.TypedClone());
	}

	private bool TryGetTransId(DataType dt, string uid, out long transId)
		=> _mdTransIds.TryGetKey((dt, uid), out transId);

	private bool TryGetAndRemove(long transId, out (DataType dt, string uid) t)
	{
		if (!_mdTransIds.TryGetAndRemove(transId, out t))
			return false;

		_mdSubs.Remove(transId);
		return true;
	}

	private void StartMarketDataStreaming(CancellationToken cancellationToken)
	{
		_ = Task.Run(async () =>
		{
			var currentDelay = _baseDelay;

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await foreach (var response in _mdStream.ResponseStream.ReadAllAsync(cancellationToken))
					{
						currentDelay = _baseDelay;

						if (response.Candle is Candle c)
						{
							var dt = c.Interval.ToTimeFrame().TimeFrame();

							if (TryGetTransId(dt, c.InstrumentUid, out var transId))
							{
								SendOutMessage(new TimeFrameCandleMessage
								{
									OriginalTransactionId = transId,
									OpenTime = c.Time.ToDateTimeOffset(),
									CloseTime = c.LastTradeTs?.ToDateTimeOffset() ?? default,
									OpenPrice = c.Open?.ToDecimal() ?? default,
									HighPrice = c.High?.ToDecimal() ?? default,
									LowPrice = c.Low?.ToDecimal() ?? default,
									ClosePrice = c.Close?.ToDecimal() ?? default,
									TotalVolume = c.Volume,
									State = CandleStates.Active,
								});
							}
						}

						if (response.Trade is Trade t)
						{
							if (TryGetTransId(DataType.Ticks, t.InstrumentUid, out var transId))
							{
								SendOutMessage(new ExecutionMessage
								{
									DataTypeEx = DataType.Ticks,
									OriginalTransactionId = transId,
									ServerTime = t.Time.ToDateTimeOffset(),
									TradePrice = t.Price?.ToDecimal(),
									TradeVolume = t.Quantity,
									OriginSide = t.Direction.ToSide(),
									IsSystem = t.TradeSource == TradeSourceType.TradeSourceDealer ? true : null,
								});
							}
						}

						if (response.LastPrice is LastPrice p)
						{
							if (TryGetTransId(DataType.Level1, p.InstrumentUid, out var transId))
							{
								SendOutMessage(new Level1ChangeMessage
								{
									OriginalTransactionId = transId,
									ServerTime = p.Time.ToDateTimeOffset(),
								}.TryAdd(Level1Fields.LastTradePrice, p.Price?.ToDecimal()));
							}
						}

						if (response.TradingStatus is TradingStatus s)
						{
							if (TryGetTransId(DataType.Level1, s.InstrumentUid, out var transId))
							{
								SendOutMessage(new Level1ChangeMessage
								{
									OriginalTransactionId = transId,
									ServerTime = s.Time.ToDateTimeOffset(),
								}.TryAdd(Level1Fields.State, s.TradingStatus_.ToState()));
							}
						}

						if (response.Orderbook is OrderBook b && b.IsConsistent)
						{
							if (TryGetTransId(DataType.MarketDepth, b.InstrumentUid, out var transId))
							{
								static QuoteChange[] convert(RepeatedField<Order> quotes)
									=> [.. quotes.Select(p => new QuoteChange(p.Price?.ToDecimal() ?? default, p.Quantity))];

								SendOutMessage(new QuoteChangeMessage
								{
									OriginalTransactionId = transId,
									ServerTime = b.Time.ToDateTimeOffset(),

									Bids = convert(b.Bids),
									Asks = convert(b.Asks),
								});
							}
						}

						void sendFailed(long transId, SubscriptionStatus status)
						{
							_mdTransIds.Remove(transId);
							_mdSubs.Remove(transId);
							SendSubscriptionReply(transId, new InvalidOperationException(status.ToString()));
						}

						if (response.SubscribeCandlesResponse is SubscribeCandlesResponse rc)
						{
							foreach (var sub in rc.CandlesSubscriptions)
							{
								if (sub.SubscriptionStatus == SubscriptionStatus.Success || !TryGetTransId(sub.Interval.ToTimeFrame().TimeFrame(), sub.InstrumentUid, out var transId))
									continue;

								sendFailed(transId, sub.SubscriptionStatus);
							}
						}

						if (response.SubscribeTradesResponse is SubscribeTradesResponse rt)
						{
							foreach (var sub in rt.TradeSubscriptions)
							{
								if (sub.SubscriptionStatus == SubscriptionStatus.Success || !TryGetTransId(DataType.Ticks, sub.InstrumentUid, out var transId))
									continue;

								sendFailed(transId, sub.SubscriptionStatus);
							}
						}

						if (response.SubscribeLastPriceResponse is SubscribeLastPriceResponse rl)
						{
							foreach (var sub in rl.LastPriceSubscriptions)
							{
								if (sub.SubscriptionStatus == SubscriptionStatus.Success || !TryGetTransId(DataType.Level1, sub.InstrumentUid, out var transId))
									continue;

								sendFailed(transId, sub.SubscriptionStatus);
							}
						}

						if (response.SubscribeOrderBookResponse is SubscribeOrderBookResponse ro)
						{
							foreach (var sub in ro.OrderBookSubscriptions)
							{
								if (sub.SubscriptionStatus == SubscriptionStatus.Success || !TryGetTransId(DataType.MarketDepth, sub.InstrumentUid, out var transId))
									continue;

								sendFailed(transId, sub.SubscriptionStatus);
							}
						}
					}
				}
				catch (Exception ex)
				{
					if (cancellationToken.IsCancellationRequested)
						break;
					
					this.AddErrorLog(ex);

					try
					{
						_mdStream = _service.MarketDataStream.MarketDataStream(cancellationToken: cancellationToken);

						foreach (var mdMsg in _mdSubs.CachedValues)
						{
							if (mdMsg.DataType2 == DataType.Ticks)
								await SubscribeTicks(mdMsg, cancellationToken);
							else if (mdMsg.DataType2 == DataType.MarketDepth)
								await SubscribeMarketDepth(mdMsg, cancellationToken);
							else if (mdMsg.DataType2 == DataType.Level1)
								await SubscribeLevel1(mdMsg, cancellationToken);
							else if (mdMsg.DataType2.IsTFCandles)
								await SubscribeCandles(mdMsg, cancellationToken);
							else
								throw new InvalidOperationException(mdMsg.ToString());
						}
					}
					catch (Exception ex1)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						this.AddErrorLog(ex1);

						currentDelay = GetCurrentDelay(currentDelay);
						await currentDelay.Delay(cancellationToken);
					}
				}
			}
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var instrSvc = _service.Instruments;

		var secTypes = lookupMsg.GetSecurityTypes();

		if (secTypes.IsEmpty())
		{
			secTypes.AddRange(
			[
				SecurityTypes.Stock,
				//SecurityTypes.Etf,
				//SecurityTypes.Bond,
				SecurityTypes.Future,
				//SecurityTypes.Option,
				SecurityTypes.Currency,
			]);
		}

		var left = lookupMsg.Count ?? long.MaxValue;

		bool TrySendOut(SecurityMessage secMsg)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!secMsg.IsMatch(lookupMsg, secTypes))
				return true;

			SendOutMessage(secMsg);
			return --left > 0;
		}

		foreach (var secType in secTypes)
		{
			switch (secType)
			{
				case SecurityTypes.Stock:
				{
					foreach (var instr in (await instrSvc.SharesAsync(cancellationToken)).Instruments)
					{
						if (!TrySendOut(new()
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.ClassCode,
								Native = instr.Uid,
								Isin = instr.Isin,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Stock,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							IssueDate = instr.IpoDate?.ToDateTimeOffset(),
							IssueSize = instr.IssueSize,
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement?.ToDecimal(),
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							break;
						}
					}

					break;
				}
				case SecurityTypes.Future:
				{
					foreach (var instr in (await instrSvc.FuturesAsync(cancellationToken)).Instruments)
					{
						if (!TrySendOut(new SecurityMessage
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.ClassCode,
								Native = instr.Uid,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Future,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							Class = instr.ClassCode,
							ExpiryDate = instr.ExpirationDate?.ToDateTimeOffset(),
							IssueDate = instr.FirstTradeDate?.ToDateTimeOffset(),
							UnderlyingSecurityType = instr.AssetType.ToSecurityType(),
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement?.ToDecimal(),
							SettlementType = instr.FuturesType.ToSettlementType(),
							OriginalTransactionId = lookupMsg.TransactionId,
						}.TryFillUnderlyingId(instr.BasicAsset)))
						{
							break;
						}
					}

					break;
				}
				case SecurityTypes.Option:
				{
					var underlying = lookupMsg.UnderlyingSecurityId;

					if (underlying.Native is null)
						break;

					foreach (var instr in (await instrSvc.OptionsByAsync(new() { BasicAssetUid = (string)underlying.Native }, cancellationToken: cancellationToken)).Instruments)
					{
						if (!TrySendOut(new SecurityMessage
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.ClassCode,
								Native = instr.Uid,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Option,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							ExpiryDate = instr.ExpirationDate?.ToDateTimeOffset(),
							IssueDate = instr.FirstTradeDate?.ToDateTimeOffset(),
							UnderlyingSecurityType = instr.AssetType.ToSecurityType(),
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement?.ToDecimal(),
							OptionType = instr.Direction.ToOptionType(),
							OptionStyle = instr.Style.ToOptionStyle(),
							SettlementType = instr.SettlementType.ToSettlementType(),
							Strike = instr.StrikePrice,
							OriginalTransactionId = lookupMsg.TransactionId,
						}.TryFillUnderlyingId(instr.BasicAsset)))
						{
							break;
						}
					}

					break;
				}
				case SecurityTypes.Currency:
				{
					foreach (var instr in (await instrSvc.CurrenciesAsync(new(), cancellationToken: cancellationToken)).Instruments)
					{
						if (!TrySendOut(new()
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.ClassCode,
								Native = instr.Uid,
								Isin = instr.Isin,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Currency,
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement?.ToDecimal(),
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							break;
						}
					}

					break;
				}
				case SecurityTypes.Bond:
				{
					foreach (var instr in (await instrSvc.BondsAsync(cancellationToken)).Instruments)
					{
						if (!TrySendOut(new()
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.ClassCode,
								Native = instr.Uid,
								Isin = instr.Isin,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Bond,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							ExpiryDate = instr.MaturityDate?.ToDateTimeOffset(),
							IssueDate = instr.StateRegDate?.ToDateTimeOffset(),
							IssueSize = instr.IssueSize,
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement?.ToDecimal(),
							FaceValue = instr.Nominal?.ToDecimal(),
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							break;
						}
					}

					break;
				}
				case SecurityTypes.Etf:
				{
					foreach (var instr in (await instrSvc.EtfsAsync(cancellationToken: cancellationToken)).Instruments)
					{
						if (!TrySendOut(new()
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.ClassCode,
								Native = instr.Uid,
								Isin = instr.Isin,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Etf,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							Shortable = instr.ShortEnabledFlag,
							IssueDate = instr.ReleasedDate?.ToDateTimeOffset(),
							IssueSize = instr.NumShares?.ToDecimal(),
							PriceStep = instr.MinPriceIncrement?.ToDecimal(),
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							break;
						}
					}

					break;
				}
			}

			if (left <= 0)
				break;
		}

		SendSubscriptionFinished(lookupMsg.TransactionId);
	}

	/// <inheritdoc/>
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var tf = mdMsg.GetTimeFrame();

		SendSubscriptionReply(mdMsg.TransactionId);

		if (mdMsg.IsSubscribe)
		{
			if (mdMsg.From is not null)
			{
				var from = mdMsg.From.Value;
				var to = mdMsg.To ?? CurrentTime;

				if (tf.ToNative() == SubscriptionInterval.OneMinute && (to - from).TotalDays > 1)
				{
					var response = await _service.Instruments.GetInstrumentByAsync(new()
					{
						IdType = InstrumentIdType.Uid,
						Id = mdMsg.GetInstrumentId(),
					}, cancellationToken: cancellationToken);

					if (response.Instrument is not null)
						await DownloadHistoryAsync(mdMsg.TransactionId, response.Instrument.Figi, from, to, cancellationToken);

					from = to.Date.UtcKind();
				}

				var request = new GetCandlesRequest
				{
					InstrumentId = mdMsg.GetInstrumentId(),
					Interval = tf.ToNative2(),
					From = from.ToTimestamp(),
					To = to.ToTimestamp(),
					CandleSourceType = mdMsg.IsRegularTradingHours == false ? GetCandlesRequest.Types.CandleSource.Unspecified : GetCandlesRequest.Types.CandleSource.Exchange,
				};

				if (mdMsg.Count is long count)
					request.Limit = (int)count;

				var candles = await _service.MarketData.GetCandlesAsync(request, cancellationToken: cancellationToken);

				foreach (var c in candles.Candles)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (mdMsg.IsFinishedOnly && !c.IsComplete)
						continue;

					SendOutMessage(new TimeFrameCandleMessage
					{
						OriginalTransactionId = mdMsg.TransactionId,

						OpenTime = c.Time.ToDateTimeOffset(),
						OpenPrice = c.Open?.ToDecimal() ?? default,
						HighPrice = c.High?.ToDecimal() ?? default,
						LowPrice = c.Low?.ToDecimal() ?? default,
						ClosePrice = c.Close?.ToDecimal() ?? default,
						TotalVolume = c.Volume,

						State = c.IsComplete ? CandleStates.Finished : CandleStates.Active,
					});
				}
			}

			if (!mdMsg.IsHistoryOnly())
			{
				AddTransId(mdMsg);

				await SubscribeCandles(mdMsg, cancellationToken);
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await WriteMdRequest(new()
			{
				SubscribeCandlesRequest = new()
				{
					Instruments =
					{
						new CandleInstrument
						{
							InstrumentId = t.uid,
							Interval = tf.ToNative(),
						}
					},
					SubscriptionAction = SubscriptionAction.Unsubscribe,
				}
			}, cancellationToken);
		}
	}

	private Task SubscribeCandles(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> WriteMdRequest(new()
		{
			SubscribeCandlesRequest = new()
			{
				Instruments =
				{
					new CandleInstrument
					{
						InstrumentId = mdMsg.GetInstrumentId(),
						Interval = mdMsg.GetTimeFrame().ToNative(),
					}
				},
				SubscriptionAction = SubscriptionAction.Subscribe,
				WaitingClose = mdMsg.IsFinishedOnly,
				CandleSourceType = mdMsg.IsRegularTradingHours switch
				{
					null => GetCandlesRequest.Types.CandleSource.Unspecified,
					false => GetCandlesRequest.Types.CandleSource.IncludeWeekend,
					_ => GetCandlesRequest.Types.CandleSource.Exchange,
				},
			},
		}, cancellationToken);

	private async Task<DateTimeOffset> DownloadHistoryAsync(long transId, string figi, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
	{
		var last = from;
		var curr = from;

		while (curr < to)
		{
			using var response = await _historyClient.GetAsync($"https://{_domainAddr}/history-data?figi={figi}&year={curr.Year}", cancellationToken);
			response.EnsureSuccessStatusCode();

			using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

			using var entries = stream.Unzip(true);

			foreach (var (name, body) in entries.Select(t => (t.name, t.body.To<byte[]>())).OrderBy(t => t.name))
			{
				var fileDate = name.Substring(name.Length - 12, 8).ToDateTime("yyyyMMdd");

				if (fileDate < from.Date)
					continue;

				if (fileDate > to.Date)
					break;

				var reader = new FastCsvReader(body.To<Stream>(), Encoding.UTF8, StringHelper.N);
				var needBreak = false;

				while (reader.NextLine())
				{
					cancellationToken.ThrowIfCancellationRequested();

					reader.Skip();

					var timestamp = reader.ReadDateTime("yyyy-MM-ddTHH:mm:ssZ").UtcKind();
					var open = reader.ReadDecimal();
					var close = reader.ReadDecimal();
					var high = reader.ReadDecimal();
					var low = reader.ReadDecimal();
					var volume = reader.ReadDecimal();

					if (timestamp < from)
						continue;

					if (timestamp > to)
					{
						needBreak = true;
						break;
					}

					SendOutMessage(new TimeFrameCandleMessage
					{
						OpenTime = timestamp,
						OpenPrice = open,
						HighPrice = high,
						LowPrice = low,
						ClosePrice = close,
						TotalVolume = volume,
						OriginalTransactionId = transId,
						State = CandleStates.Finished,
					});

					last = last.Max(timestamp);
				}

				if (needBreak)
					break;
			}

			curr = curr.AddYears(1);
		}

		return last;
	}

	private const int _defBook = 10;

	/// <inheritdoc/>
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		if (mdMsg.IsSubscribe)
		{
			if (!mdMsg.IsHistoryOnly())
			{
				AddTransId(mdMsg);

				await SubscribeMarketDepth(mdMsg, cancellationToken);
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await WriteMdRequest(new()
			{
				SubscribeOrderBookRequest = new()
				{
					Instruments =
					{
						new OrderBookInstrument
						{
							InstrumentId = t.uid,
							Depth = mdMsg.MaxDepth ?? _defBook,
							OrderBookType = mdMsg.IsRegularTradingHours switch
							{
								null => OrderBookType.Unspecified,
								false => OrderBookType.Dealer,
								_ => OrderBookType.Exchange,
							},
						}
					},
					SubscriptionAction = SubscriptionAction.Unsubscribe,
				}
			}, cancellationToken);
		}
	}

	private Task SubscribeMarketDepth(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> WriteMdRequest(new()
		{
			SubscribeOrderBookRequest = new()
			{
				Instruments =
				{
					new OrderBookInstrument
					{
						InstrumentId = mdMsg.GetInstrumentId(),
						Depth = mdMsg.MaxDepth ?? _defBook,
						OrderBookType = mdMsg.IsRegularTradingHours switch
						{
							null => OrderBookType.Unspecified,
							false => OrderBookType.Dealer,
							_ => OrderBookType.Exchange,
						},
					}
				},
				SubscriptionAction = SubscriptionAction.Subscribe,
			}
		}, cancellationToken);

	/// <inheritdoc/>
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		if (mdMsg.IsSubscribe)
		{
			if (!mdMsg.IsHistoryOnly())
			{
				AddTransId(mdMsg);

				await SubscribeTicks(mdMsg, cancellationToken);
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await WriteMdRequest(new()
			{
				SubscribeTradesRequest = new()
				{
					Instruments =
					{
						new TradeInstrument
						{
							InstrumentId = t.uid,
						}
					},
					SubscriptionAction = SubscriptionAction.Unsubscribe,
					TradeSource = mdMsg.IsRegularTradingHours switch
					{
						null => TradeSourceType.TradeSourceUnspecified,
						false => TradeSourceType.TradeSourceAll,
						_ => TradeSourceType.TradeSourceExchange,
					}
				}
			}, cancellationToken);
		}
	}

	private Task SubscribeTicks(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> WriteMdRequest(new()
		{
			SubscribeTradesRequest = new()
			{
				Instruments =
				{
					new TradeInstrument
					{
						InstrumentId = mdMsg.GetInstrumentId(),
					}
				},
				SubscriptionAction = SubscriptionAction.Subscribe,
				TradeSource = mdMsg.IsRegularTradingHours switch
				{
					null => TradeSourceType.TradeSourceUnspecified,
					false => TradeSourceType.TradeSourceAll,
					_ => TradeSourceType.TradeSourceExchange,
				}
			}
		}, cancellationToken);

	/// <inheritdoc/>
	protected override async ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		if (mdMsg.IsSubscribe)
		{
			if (!mdMsg.IsHistoryOnly())
			{
				AddTransId(mdMsg);

				await SubscribeLevel1(mdMsg, cancellationToken);
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await WriteMdRequest(new()
			{
				SubscribeLastPriceRequest = new()
				{
					Instruments =
					{
						new LastPriceInstrument
						{
							InstrumentId = t.uid,
						}
					},
					SubscriptionAction = SubscriptionAction.Unsubscribe,
				},
				SubscribeInfoRequest = new()
				{
					Instruments =
					{
						new InfoInstrument
						{
							InstrumentId = t.uid,
						}
					},
					SubscriptionAction = SubscriptionAction.Unsubscribe,
				},
			}, cancellationToken);
		}
	}

	private Task SubscribeLevel1(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> WriteMdRequest(new()
		{
			SubscribeLastPriceRequest = new()
			{
				Instruments =
				{
					new LastPriceInstrument
					{
						InstrumentId = mdMsg.GetInstrumentId(),
					}
				},
				SubscriptionAction = SubscriptionAction.Subscribe,
			},
			SubscribeInfoRequest = new()
			{
				Instruments =
				{
					new InfoInstrument
					{
						InstrumentId = mdMsg.GetInstrumentId(),
					}
				},
				SubscriptionAction = SubscriptionAction.Subscribe,
			},
		}, cancellationToken);

	private async Task WriteMdRequest(MarketDataRequest message, CancellationToken cancellationToken)
	{
		using var _ = await _lock.LockAsync(cancellationToken);
		await _mdStream.RequestStream.WriteAsync(message, cancellationToken);
	}
}
