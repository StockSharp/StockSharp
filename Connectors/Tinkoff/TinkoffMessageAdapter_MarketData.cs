namespace StockSharp.Tinkoff;

public partial class TinkoffMessageAdapter
{
	private readonly SynchronizedPairSet<(DataType dt, string uid), long> _mdTransIds = new();

	private void AddTransId(MarketDataMessage mdMsg)
	{
		if (mdMsg is null)
			throw new ArgumentNullException(nameof(mdMsg));

		_mdTransIds.Add((mdMsg.DataType2, mdMsg.GetInstrumentId()), mdMsg.TransactionId);
	}

	private bool TryGetTransId(DataType dt, string uid, out long transId)
		=> _mdTransIds.TryGetValue((dt, uid), out transId);

	private bool TryGetAndRemove(long transId, out (DataType dt, string uid) t)
	{
		if (!_mdTransIds.TryGetKeyAndRemove(transId, out t))
			return false;

		return true;
	}

	private void StartMarketDataStreaming(CancellationToken cancellationToken)
	{
		_ = Task.Run(async () =>
		{
			var currError = 0;

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					currError = 0;

					await foreach (var response in _mdStream.ResponseStream.ReadAllAsync(cancellationToken))
					{
						if (response.Candle is Candle c)
						{
							var dt = DataType.TimeFrame(c.Interval.ToTimeFrame());

							if (TryGetTransId(dt, c.InstrumentUid, out var transId))
							{
								SendOutMessage(new TimeFrameCandleMessage
								{
									OriginalTransactionId = transId,
									OpenTime = c.Time.ToDateTimeOffset(),
									CloseTime = c.LastTradeTs?.ToDateTimeOffset() ?? default,
									OpenPrice = c.Open,
									HighPrice = c.High,
									LowPrice = c.Low,
									ClosePrice = c.Close,
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
									TradePrice = t.Price,
									TradeVolume = t.Quantity,
									OriginSide = t.Direction.ToSide(),
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
								}.TryAdd(Level1Fields.LastTradePrice, (decimal)p.Price));
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
								SendOutMessage(new QuoteChangeMessage
								{
									OriginalTransactionId = transId,
									ServerTime = b.Time.ToDateTimeOffset(),

									Bids = b.Bids.Select(p => new QuoteChange(p.Price, p.Quantity)).ToArray(),
									Asks = b.Asks.Select(p => new QuoteChange(p.Price, p.Quantity)).ToArray(),
								});
							}
						}

						void sendFailed(long transId, SubscriptionStatus status)
						{
							_mdTransIds.RemoveByValue(transId);
							SendSubscriptionReply(transId, new InvalidOperationException(status.ToString()));
						}

						if (response.SubscribeCandlesResponse is SubscribeCandlesResponse rc)
						{
							foreach (var sub in rc.CandlesSubscriptions)
							{
								if (sub.SubscriptionStatus == SubscriptionStatus.Success || !TryGetTransId(DataType.TimeFrame(sub.Interval.ToTimeFrame()), sub.InstrumentUid, out var transId))
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

					if (++currError >= 10)
						break;

					try
					{
						_mdStream = _service.MarketDataStream.MarketDataStream(cancellationToken: cancellationToken);
					}
					catch (Exception ex1)
					{
						this.AddErrorLog(ex1);
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
			secTypes.AddRange(new[]
			{
				SecurityTypes.Stock,
				//SecurityTypes.Etf,
				//SecurityTypes.Bond,
				SecurityTypes.Future,
				//SecurityTypes.Option,
			});
		}

		var left = lookupMsg.Count ?? long.MaxValue;

		bool TrySendOut(SecurityMessage secMsg)
		{
			if (!secMsg.IsMatch(lookupMsg, secTypes))
				return false;

			SendOutMessage(secMsg);
			return true;
		}

		foreach (var secType in secTypes)
		{
			var shares = await instrSvc.SharesAsync(cancellationToken);
			var bonds = await instrSvc.BondsAsync(cancellationToken);
			var futures = await instrSvc.FuturesAsync(cancellationToken);

			switch (secType)
			{
				case SecurityTypes.Stock:
				{
					foreach (var instr in (await instrSvc.SharesAsync(cancellationToken)).Instruments)
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (TrySendOut(new SecurityMessage
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.Exchange,
								Native = instr.Uid,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Stock,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							Class = instr.ClassCode,
							IssueDate = instr.IpoDate?.ToDateTimeOffset(),
							IssueSize = instr.IssueSize,
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement,
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							if (--left <= 0)
								break;
						}
					}

					break;
				}
				case SecurityTypes.Future:
				{
					foreach (var instr in (await instrSvc.FuturesAsync(cancellationToken)).Instruments)
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (TrySendOut(new SecurityMessage
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.Exchange,
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
							PriceStep = instr.MinPriceIncrement,
							SettlementType = instr.FuturesType.ToSettlementType(),
							OriginalTransactionId = lookupMsg.TransactionId,
						}.TryFillUnderlyingId(instr.BasicAsset)))
						{
							if (--left <= 0)
								break;
						}
					}

					break;
				}
				//case SecurityTypes.Option:
				//{
				//	foreach (var instr in (await instrSvc.OptionsAsync(new(), cancellationToken: cancellationToken)).Instruments)
				//	{
				//		cancellationToken.ThrowIfCancellationRequested();
				//
				//		if (TrySendOut(new SecurityMessage
				//		{
				//			SecurityId = new()
				//			{
				//				SecurityCode = instr.Ticker,
				//				BoardCode = instr.Exchange,
				//				Native = instr.Uid,
				//			},
				//			Name = instr.Name,
				//			Multiplier = instr.Lot,
				//			SecurityType = SecurityTypes.Option,
				//			Currency = instr.Currency.To<CurrencyTypes?>(),
				//			Class = instr.ClassCode,
				//			ExpiryDate = instr.ExpirationDate?.ToDateTimeOffset(),
				//			IssueDate = instr.FirstTradeDate?.ToDateTimeOffset(),
				//			UnderlyingSecurityType = instr.AssetType.ToSecurityType(),
				//			Shortable = instr.ShortEnabledFlag,
				//			PriceStep = instr.MinPriceIncrement,
				//			OptionType = instr.Direction.ToOptionType(),
				//			OptionStyle = instr.Style.ToOptionStyle(),
				//			SettlementType = instr.SettlementType.ToSettlementType(),
				//			Strike = instr.StrikePrice,
				//			OriginalTransactionId = lookupMsg.TransactionId,
				//		}.TryFillUnderlyingId(instr.BasicAsset)))
				//		{
				//			if (--left <= 0)
				//				break;
				//		}
				//	}

				//	break;
				//}
				case SecurityTypes.Currency:
				{
					foreach (var instr in (await instrSvc.CurrenciesAsync(new(), cancellationToken: cancellationToken)).Instruments)
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (TrySendOut(new SecurityMessage
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.Exchange,
								Isin = instr.Isin,
								Native = instr.Uid,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Currency,
							Class = instr.ClassCode,
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement,
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							if (--left <= 0)
								break;
						}
					}

					break;
				}
				case SecurityTypes.Bond:
				{
					foreach (var instr in (await instrSvc.BondsAsync(cancellationToken)).Instruments)
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (TrySendOut(new SecurityMessage
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.Exchange,
								Isin = instr.Isin,
								Native = instr.Uid,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Bond,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							Class = instr.ClassCode,
							ExpiryDate = instr.MaturityDate?.ToDateTimeOffset(),
							IssueDate = instr.StateRegDate?.ToDateTimeOffset(),
							IssueSize = instr.IssueSize,
							Shortable = instr.ShortEnabledFlag,
							PriceStep = instr.MinPriceIncrement,
							FaceValue = instr.Nominal,
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							if (--left <= 0)
								break;
						}
					}

					break;
				}
				case SecurityTypes.Etf:
				{
					foreach (var instr in (await instrSvc.EtfsAsync(cancellationToken: cancellationToken)).Instruments)
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (TrySendOut(new SecurityMessage
						{
							SecurityId = new()
							{
								SecurityCode = instr.Ticker,
								BoardCode = instr.Exchange,
								Isin = instr.Isin,
								Native = instr.Uid,
							},
							Name = instr.Name,
							Multiplier = instr.Lot,
							SecurityType = SecurityTypes.Etf,
							Currency = instr.Currency.To<CurrencyTypes?>(),
							Class = instr.ClassCode,
							Shortable = instr.ShortEnabledFlag,
							IssueDate = instr.ReleasedDate?.ToDateTimeOffset(),
							IssueSize = instr.NumShares,
							PriceStep = instr.MinPriceIncrement,
							OriginalTransactionId = lookupMsg.TransactionId,
						}))
						{
							if (--left <= 0)
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
		var interval = tf.ToNative();

		SendSubscriptionReply(mdMsg.TransactionId);

		if (mdMsg.IsSubscribe)
		{
			if (mdMsg.From is not null)
			{
				var from = mdMsg.From.Value;
				var to = mdMsg.To ?? CurrentTime;

				var left = mdMsg.Count ?? long.MaxValue;

				var candles = await _service.MarketData.GetCandlesAsync(new()
				{
					InstrumentId = mdMsg.GetInstrumentId(),
					Interval = tf.ToNative2(),
					From = from.ToTimestamp(),
					To = to.ToTimestamp(),
					CandleSourceType = mdMsg.IsRegularTradingHours == false ? GetCandlesRequest.Types.CandleSource.Unspecified : GetCandlesRequest.Types.CandleSource.Exchange,
				}, cancellationToken: cancellationToken);

				foreach (var c in candles.Candles)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (mdMsg.IsFinishedOnly && !c.IsComplete)
						continue;

					SendOutMessage(new TimeFrameCandleMessage
					{
						OriginalTransactionId = mdMsg.TransactionId,

						OpenTime = c.Time.ToDateTimeOffset(),
						OpenPrice = c.Open,
						HighPrice = c.High,
						LowPrice = c.Low,
						ClosePrice = c.Close,
						TotalVolume = c.Volume,

						State = c.IsComplete ? CandleStates.Finished : CandleStates.Active,
					});

					if (--left <= 0)
						break;
				}
			}

			if (!mdMsg.IsHistoryOnly())
			{
				AddTransId(mdMsg);

				await _mdStream.RequestStream.WriteAsync(new()
				{
					SubscribeCandlesRequest = new()
					{
						Instruments =
						{
							new CandleInstrument
							{
								InstrumentId = mdMsg.GetInstrumentId(),
								Interval = interval,
							}
						},
						SubscriptionAction = SubscriptionAction.Subscribe,
						WaitingClose = mdMsg.IsFinishedOnly,
					},
				}, cancellationToken);
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await _mdStream.RequestStream.WriteAsync(new()
			{
				SubscribeCandlesRequest = new()
				{
					Instruments =
					{
						new CandleInstrument
						{
							InstrumentId = t.uid,
							Interval = interval,
						}
					},
					SubscriptionAction = SubscriptionAction.Unsubscribe,
				}
			}, cancellationToken);
		}
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

				await _mdStream.RequestStream.WriteAsync(new()
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
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await _mdStream.RequestStream.WriteAsync(new()
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

	/// <inheritdoc/>
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		if (mdMsg.IsSubscribe)
		{
			if (!mdMsg.IsHistoryOnly())
			{
				AddTransId(mdMsg);

				await _mdStream.RequestStream.WriteAsync(new()
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
						TradeType = mdMsg.IsRegularTradingHours switch
						{
							null => TradeSourceType.TradeSourceUnspecified,
							false => TradeSourceType.TradeSourceAll,
							_ => TradeSourceType.TradeSourceExchange,
						}
					}
				}, cancellationToken);
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await _mdStream.RequestStream.WriteAsync(new()
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
					TradeType = mdMsg.IsRegularTradingHours switch
					{
						null => TradeSourceType.TradeSourceUnspecified,
						false => TradeSourceType.TradeSourceAll,
						_ => TradeSourceType.TradeSourceExchange,
					}
				}
			}, cancellationToken);
		}
	}

	/// <inheritdoc/>
	protected override async ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		if (mdMsg.IsSubscribe)
		{
			if (!mdMsg.IsHistoryOnly())
			{
				AddTransId(mdMsg);

				await _mdStream.RequestStream.WriteAsync(new()
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
			}

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			if (!TryGetAndRemove(mdMsg.OriginalTransactionId, out var t))
				return;

			await _mdStream.RequestStream.WriteAsync(new()
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
}
