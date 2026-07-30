namespace StockSharp.Fix.Dialects;

partial class BaseFixDialect
{
	private class FixDepthBuilder(BaseFixDialect parent)
	{
		private readonly BaseFixDialect _parent = parent ?? throw new ArgumentNullException(nameof(parent));
		private readonly Dictionary<SecurityId, Dictionary<string, (QuoteChange change, Sides side)>> _quotes = [];

		public async IAsyncEnumerable<Message> ProcessQuoteAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
		{
			//_adapter.SessionHolder.AddLog(LogLevels.Debug, () => "Quote: {0}".Put(msg));

			string symbol = null;
			string securityExchange = null;
			string exDestination = null;
			string tradingSessionId = null;
			string quoteId = null;
			QuoteType? quoteType = null;
			char? side = null;
			decimal? bidPx = null, bidSize = null, offerPx = null, offerSize = null;
			var sendingTime = default(DateTime);

			var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
			{
				switch (tag)
				{
					case FixTags.Symbol:
						symbol = await reader.ReadStringAsync(ct);
						return true;
					case FixTags.SecurityExchange:
						securityExchange = await reader.ReadStringAsync(ct);
						return true;
					case FixTags.ExDestination:
						exDestination = await reader.ReadStringAsync(ct);
						return true;
					case FixTags.TradingSessionID:
						tradingSessionId = await reader.ReadStringAsync(ct);
						return true;
					case FixTags.QuoteID:
						quoteId = await reader.ReadStringAsync(ct);
						return true;
					case FixTags.QuoteType:
						quoteType = (QuoteType)await reader.ReadIntAsync(ct);
						return true;
					case FixTags.Side:
						side = await reader.ReadCharAsync(ct);
						return true;
					case FixTags.BidPx:
						bidPx = await reader.ReadDecimalAsync(ct);
						return true;
					case FixTags.BidSize:
						bidSize = await reader.ReadDecimalAsync(ct);
						return true;
					case FixTags.OfferPx:
						offerPx = await reader.ReadDecimalAsync(ct);
						return true;
					case FixTags.OfferSize:
						offerSize = await reader.ReadDecimalAsync(ct);
						return true;
					case FixTags.SendingTime:
						sendingTime = await reader.ReadUtcAsync(_parent.TimeStampParser, ct);
						return true;
					default:
						return false;
				}
			}, cancellationToken);

			if (!isOk)
				yield break;

			var securityId = new SecurityId
			{
				SecurityCode = symbol,
				BoardCode = _parent.GetBoardCode(exDestination, securityExchange, tradingSessionId),
			};

			var dict = _quotes.SafeAdd(securityId, _ => []);

			//var id = msg.QuoteID.Obj;

			if (quoteType == QuoteType.Tradeable)
			{
				var quoteSide = side == Side.Buy ? Sides.Buy : Sides.Sell;
				var change = side switch
				{
					Side.Buy when bidPx != null && bidSize != null => new QuoteChange(bidPx.Value, bidSize.Value),
					Side.Sell when offerPx != null && offerSize != null => new QuoteChange(offerPx.Value, offerSize.Value),
					_ => throw new InvalidOperationException(),
				};
				dict[quoteId] = (change, quoteSide);
			}
			else
			{
				if (!dict.TryGetAndRemove(quoteId, out _))
					yield break;
			}

			var bids = new List<QuoteChange>();
			var asks = new List<QuoteChange>();

			foreach (var (change, quoteSide) in dict.Values)
			{
				(quoteSide == Sides.Buy ? bids : asks).Add(change);
			}

			yield return new QuoteChangeMessage
			{
				SecurityId = securityId,
				ServerTime = sendingTime,
				Bids = [.. bids],
				Asks = [.. asks],
			};
		}

		public QuoteChangeMessage ProcessMarketData(SecurityId securityId, DateTime time, IEnumerable<MDEntry> entries, bool fullRefresh, long? mdReqId, DataType lastBuildFrom)
		{
			var bids = new List<QuoteChange>();
			var asks = new List<QuoteChange>();

			var hasPositions = false;

			foreach (var entry in entries)
			{
				var quote = new QuoteChange(
					entry.Price ?? 0,
					entry.Size ?? 0,
					(int?)entry.NumberOfOrders,
					entry.QuoteCondition.ToQuoteCondition()
				)
				{
					Action = entry.Action?.ToQuoteAction(),
					StartPosition = entry.Position,
					EndPosition = entry.PositionExtra,
				};

				if (quote.StartPosition != null || quote.EndPosition != null)
					hasPositions = true;

				(entry.Type == MDEntryType.Bid ? bids : asks).Add(quote);
			}

			return new QuoteChangeMessage
			{
				SecurityId = securityId,
				ServerTime = time,
				State = fullRefresh ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment,
				Bids = [.. bids],
				Asks = [.. asks],
				HasPositions = hasPositions,
				OriginalTransactionId = mdReqId ?? 0,
				BuildFrom = lastBuildFrom,
			};
		}

		public void Reset()
		{
			_quotes.Clear();
		}

		public QuoteChangeMessage ClearOrderBook(SecurityId securityId, DateTime time)
		{
			_quotes.Clear();

			return new QuoteChangeMessage
			{
				SecurityId = securityId,
				ServerTime = time,
				State = QuoteChangeStates.SnapshotComplete,
			};
		}
	}

	private readonly FixDepthBuilder _depthBuilder;
	private readonly SynchronizedDictionary<long, RefPair<int, int>> _totalSecCountByRequestId = [];

	private async IAsyncEnumerable<Message> ReadMarketDataRefreshAsync(IFixReader reader, bool fullRefresh, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var quotes = new List<MDEntry>();
		var l1Msgs = new List<Level1ChangeMessage>();
		var candles = new List<CandleMessage>();

		SecurityId? securityId = null;
		long? mdReqId = null;
		string symbol = null;
		string securityExchange = null;
		var sendingTime = default(DateTime);
		DateTime? mdEntryDate = null;
		TimeSpan? mdEntryTime = null;
		char? mdEntryType = null;
		decimal? mdEntryPx = null;
		decimal? mdEntrySize = null;
		decimal? prevClosePx = null;
		decimal? highPx = null;
		decimal? lowPx = null;
		decimal? lastPx = null;
		string quoteEntryId = null;
		char? tickDirection = null;
		int? mdEntryPositionNo = null;
		int? mdEntryPositionExtra = null;
		char? mdUpdateAction = null;
		string otherValue = null;
		CurrencyTypes? currency = null;
		decimal? numberOfOrders = null;
		int? candleState = null;
		int? securityTradingStatus = null;
		string quoteCondition = null;
		decimal? yield = null;
		char? mdEntryOriginator = null;
		decimal? minQty = null;
		decimal? maxTradeVol = null;
		int? upTicks = null;
		int? downTicks = null;
		int? totalTicks = null;
		CandlePriceLevel[] levels = null;
		decimal? levelPrevPrice = null;
		int levelsIdx = -1;
		Sides? levelCurrSide = null;
		int levelVolsIdx = -1;
		char? buildFromType = null;
		string buildFromArg = null;
		string buyer = null;
		string seller = null;

		DataType lastBuildFrom = null;

		DateTime GetTime()
		{
			if (mdEntryDate == null && mdEntryTime != null)
				return (sendingTime == default ? DateTime.UtcNow.Date : sendingTime.Date) + mdEntryTime.Value;

			return (mdEntryDate + mdEntryTime) ?? sendingTime;
		}

		DataType GetBuildFrom() => buildFromType?.ToDataType(buildFromArg);

		var msgs = new List<Message>();

		void FlushMarketData()
		{
			switch (mdEntryType)
			{
				case MDEntryType.Trade:
				{
					if (securityId == null)
					{
						LogError(LocalizedStrings.SecForRequestNotSpecified.Put(mdReqId));
						return;
					}

					if (TickAsLevel1)
					{
						var l1Msg = new Level1ChangeMessage
						{
							ServerTime = GetTime(),
							OriginalTransactionId = mdReqId ?? 0,
							BuildFrom = GetBuildFrom(),
							SecurityId = securityId.Value,
						}
						.Add(Level1Fields.LastTradePrice, mdEntryPx)
						.Add(Level1Fields.LastTradeVolume, mdEntrySize)
						.TryAdd(Level1Fields.LastTradeUpDown, tickDirection?.FromTickDir())
						.TryAdd(Level1Fields.LastTradeOrigin, mdEntryOriginator?.FromFixSide());

						l1Msgs.Add(l1Msg);
					}
					else
					{
						if (securityId.Value == default)
						{
							securityId = new SecurityId
							{
								SecurityCode = symbol,
								BoardCode = securityExchange
							};
						}

						var tickMsg = new ExecutionMessage
						{
							SecurityId = securityId.Value,
							TradeId = quoteEntryId.To<long?>(),
							TradePrice = mdEntryPx,
							TradeVolume = mdEntrySize,
							ServerTime = GetTime(),
							DataTypeEx = DataType.Ticks,
							Currency = currency,
							OpenInterest = numberOfOrders,
							OriginSide = mdEntryOriginator?.FromFixSide(),
							IsUpTick = tickDirection?.FromTickDir(),
							OriginalTransactionId = mdReqId ?? 0,
							BuildFrom = GetBuildFrom(),
							Yield = yield,
						};

						if (buyer != null && long.TryParse(buyer, out var buyId))
							tickMsg.OrderBuyId = buyId;

						if (seller != null && long.TryParse(seller, out var sellId))
							tickMsg.OrderSellId = sellId;

						msgs.Add(tickMsg);
					}
				
					break;
				}
				case MDEntryType.Bid:
				case MDEntryType.Offer:
				{
					if (QuotesAsLevel1)
					{
						var l1Msg = new Level1ChangeMessage
			            {
				            ServerTime = GetTime(),
							OriginalTransactionId = mdReqId ?? 0,
							BuildFrom = GetBuildFrom(),
							SecurityId = securityId ?? default,
			            };

						if (mdEntryType == MDEntryType.Bid)
						{
							l1Msg
								.TryAdd(Level1Fields.BestBidPrice, mdEntryPx)
								.TryAdd(Level1Fields.BestBidVolume, mdEntrySize);
						}
						else
						{
							l1Msg
								.TryAdd(Level1Fields.BestAskPrice, mdEntryPx)
								.TryAdd(Level1Fields.BestAskVolume, mdEntrySize);
						}

						l1Msgs.Add(l1Msg);
					}
					else
					{
						quotes.Add(new MDEntry
						{
							Type = mdEntryType.Value,
							Price = mdEntryPx,
							Size = mdEntrySize,
							Position = mdEntryPositionNo,
							PositionExtra = mdEntryPositionExtra,
							Action = mdUpdateAction,
							Currency = currency,
							NumberOfOrders = numberOfOrders,
							QuoteCondition = quoteCondition,
							Yield = yield,
						});

						lastBuildFrom = GetBuildFrom();
					}

					break;
				}
				case MDEntryType.EmptyBook:
				{
					if (securityId.Value == default)
					{
						securityId = new SecurityId
						{
							SecurityCode = symbol,
							BoardCode = securityExchange
						};
					}

					msgs.Add(_depthBuilder.ClearOrderBook(securityId.Value, sendingTime));

					break;
				}
				default:
				{
					if (mdEntryType.Value.IsCandleEntry())
					{
						var candle = mdEntryType.Value.ToCandleMessage();

						candle.OpenPrice = prevClosePx ?? 0;
						candle.HighPrice = highPx ?? 0;
						candle.LowPrice = lowPx ?? 0;
						candle.ClosePrice = lastPx ?? 0;
						candle.TotalVolume = mdEntrySize ?? 0;
						candle.OpenInterest = numberOfOrders;
						candle.OpenTime = GetTime();
						candle.State = (CandleStates?)candleState ?? CandleStates.None;
						candle.LowVolume = minQty;
						candle.HighVolume = maxTradeVol;
						candle.OriginalTransactionId = mdReqId ?? 0;
						candle.UpTicks = upTicks;
						candle.DownTicks = downTicks;
						candle.TotalTicks = totalTicks;
						candle.BuildFrom = GetBuildFrom();

						if (levels != null)
						{
							for (var i = 0; i < levels.Length; i++)
							{
								var level = levels[i];

								if (level.BuyVolumes != null)
								{
									level.BuyCount = level.BuyVolumes.Count();
									level.BuyVolume = level.BuyVolumes.Sum();
								}

								if (level.SellVolumes != null)
								{
									level.SellCount = level.SellVolumes.Count();
									level.SellVolume = level.SellVolumes.Sum();
								}

								level.TotalVolume = level.BuyVolume + level.SellVolume;

								levels[i] = level;
							}
						}

						candle.PriceLevels = levels;

						candles.Add(candle);
					}
					else
					{
						var l1Msg = new Level1ChangeMessage
						{
							ServerTime = GetTime(),
							OriginalTransactionId = mdReqId ?? 0,
							BuildFrom = GetBuildFrom(),
							SecurityId = securityId ?? default,
						};
						l1Msg.FillLevel1(mdEntryType.Value, mdEntryPx, mdEntrySize, otherValue, TimeStampParser);

						if (securityTradingStatus != null)
							l1Msg.TryAdd(Level1Fields.State, FromSecurityTradingStatus(securityTradingStatus));

						l1Msgs.Add(l1Msg);
					}

					break;
				}
			}

			mdEntryType = null;
			mdEntryDate = null;
			mdEntryTime = null;
			mdEntryPx = null;
			mdEntrySize = null;
			mdEntryPositionNo = null;
			mdEntryPositionExtra = null;
			mdUpdateAction = null;
			prevClosePx = null;
			highPx = null;
			lowPx = null;
			lastPx = null;
			quoteEntryId = null;
			tickDirection = null;
			otherValue = null;
			currency = null;
			numberOfOrders = null;
			candleState = null;
			securityTradingStatus = null;
			quoteCondition = null;
			yield = null;
			mdEntryOriginator = null;
			minQty = null;
			maxTradeVol = null;
			upTicks = null;
			downTicks = null;
			totalTicks = null;
			levels = null;
			levelPrevPrice = null;
			levelsIdx = -1;
			levelCurrSide = null;
			buildFromType = null;
			buildFromArg = null;
			buyer = null;
			seller = null;
		}

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
				{
					mdReqId = await reader.ReadLongAsync(ct);
					securityId = _requestSecurities.TryGetValue2(mdReqId.Value);
					return true;
				}
				case FixTags.Symbol:
					symbol = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityExchange:
					securityExchange = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SendingTime:
					sendingTime = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				case FixTags.MDEntryDate:
				{
					if (mdEntryDate != null)
						FlushMarketData();

					mdEntryDate = await reader.ReadUtcAsync(DateParser, ct);
					return true;
				}
				case FixTags.MDEntryTime:
				{
					if (mdEntryTime != null)
						FlushMarketData();

					mdEntryTime = await reader.ReadTimeSpanAsync(TimeParser, ct);
					return true;
				}
				case FixTags.MDEntryType:
				{
					if (mdEntryType != null)
						FlushMarketData();

					mdEntryType = await reader.ReadCharAsync(ct);
					return true;
				}
				case FixTags.MDEntryPx:
				{
					if (mdEntryPx != null)
						FlushMarketData();

					mdEntryPx = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.MDEntrySize:
				{
					if (mdEntrySize != null)
						FlushMarketData();

					mdEntrySize = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.MDEntryPositionNo:
				{
					if (mdEntryPositionNo != null)
						FlushMarketData();

					mdEntryPositionNo = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.MDEntryPositionExtra:
				{
					if (mdEntryPositionExtra != null)
						FlushMarketData();

					mdEntryPositionExtra = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.MDUpdateAction:
				{
					if (mdUpdateAction != null)
						FlushMarketData();

					mdUpdateAction = await reader.ReadCharAsync(ct);
					return true;
				}
				case FixTags.QuoteEntryID:
				{
					if (quoteEntryId != null)
						FlushMarketData();

					quoteEntryId = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.TickDirection:
				{
					if (tickDirection != null)
						FlushMarketData();

					tickDirection = await reader.ReadCharAsync(ct);
					return true;
				}
				case FixTags.MDOtherValue:
				{
					if (otherValue != null)
						FlushMarketData();

					otherValue = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.Currency:
				{
					if (currency != null)
						FlushMarketData();

					currency = (await reader.ReadStringAsync(ct)).FromMicexCurrencyName(LogError);
					return true;
				}
				case FixTags.NumberOfOrders:
				{
					if (numberOfOrders != null)
						FlushMarketData();

					numberOfOrders = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.PrevClosePx:
				{
					if (prevClosePx != null)
						FlushMarketData();

					prevClosePx = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.HighPx:
				{
					if (highPx != null)
						FlushMarketData();

					highPx = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.LowPx:
				{
					if (lowPx != null)
						FlushMarketData();

					lowPx = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.LastPx:
				{
					if (lastPx != null)
						FlushMarketData();

					lastPx = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.CandleState:
				{
					if (candleState != null)
						FlushMarketData();

					candleState = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.SecurityTradingStatus:
				{
					if (securityTradingStatus != null)
						FlushMarketData();

					securityTradingStatus = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.QuoteCondition:
				{
					if (quoteCondition != null)
						FlushMarketData();

					quoteCondition = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.Yield:
				{
					if (yield != null)
						FlushMarketData();

					yield = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.MDEntryBuyer:
				{
					if (buyer != null)
						FlushMarketData();

					buyer = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.MDEntrySeller:
				{
					if (seller != null)
						FlushMarketData();

					seller = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.MDEntryOriginator:
				{
					if (mdEntryOriginator != null)
						FlushMarketData();

					mdEntryOriginator = await reader.ReadCharAsync(ct);
					return true;
				}
				case FixTags.MinQty:
				{
					if (minQty != null)
						FlushMarketData();

					minQty = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.MaxTradeVol:
				{
					if (maxTradeVol != null)
						FlushMarketData();

					maxTradeVol = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.UpTicks:
				{
					if (upTicks != null)
						FlushMarketData();

					upTicks = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.DownTicks:
				{
					if (downTicks != null)
						FlushMarketData();

					downTicks = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.TotalTicks:
				{
					if (totalTicks != null)
						FlushMarketData();

					totalTicks = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.BuildFromType:
				{
					if (buildFromType != null)
						FlushMarketData();

					buildFromType = await reader.ReadCharAsync(ct);
					return true;
				}
				case FixTags.BuildFromArg:
				{
					if (buildFromArg != null)
						FlushMarketData();

					buildFromArg = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.NoQuoteEntries:
				{
					if (levels != null)
						FlushMarketData();

					levels = new CandlePriceLevel[await reader.ReadIntAsync(ct)];
					
					for (int i = 0; i < levels.Length; i++)
						levels[i] = new CandlePriceLevel();

					levelsIdx = 0;
					levelPrevPrice = null;
					levelCurrSide = null;
					return true;
				}
				case FixTags.LevelPrice:
				{
					if (levelPrevPrice != null)
					{
						levelsIdx++;
						levelCurrSide = null;
					}

					levelPrevPrice = levels[levelsIdx].Price = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.LevelPriceBuyCount:
				{
					levels[levelsIdx].BuyCount = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.LevelPriceSellCount:
				{
					levels[levelsIdx].SellCount = await reader.ReadIntAsync(ct);
					return true;
				}
				case FixTags.LevelPriceBuyVolume:
				{
					levels[levelsIdx].BuyVolume = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.LevelPriceSellVolume:
				{
					levels[levelsIdx].SellVolume = await reader.ReadDecimalAsync(ct);
					return true;
				}
				case FixTags.LevelType:
				{
					levelCurrSide = (await reader.ReadCharAsync(ct)).FromFixSide(true);
					return true;
				}
				case FixTags.NoLegs:
				{
					var count = await reader.ReadIntAsync(ct);

					var level = levels[levelsIdx];

					if (levelCurrSide == Sides.Buy)
						level.BuyVolumes = new decimal[count];
					else
						level.SellVolumes = new decimal[count];

					levelVolsIdx = 0;

					return true;
				}
				case FixTags.LegIssueSize:
				{
					var level = levels[levelsIdx];

					var volumes = (decimal[])(levelCurrSide == Sides.Buy ? level.BuyVolumes : level.SellVolumes);
					volumes[levelVolsIdx++] = await reader.ReadDecimalAsync(ct);

					return true;
				}
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (securityId == null)
		{
			var board = securityExchange.IsEmpty(ExchangeBoard);

			if (symbol.IsEmpty() || board.IsEmpty())
			{
				LogError(LocalizedStrings.SecForRequestNotSpecified.Put(mdReqId));
				yield break;
			}

			securityId = new SecurityId
			{
				SecurityCode = symbol,
				BoardCode = board,
			};
		}

		if (securityId.Value == default)
		{
			securityId = new SecurityId
			{
				SecurityCode = symbol,
				BoardCode = securityExchange
			};
		}

		if (mdEntryType != null)
			FlushMarketData();

		foreach (var msg in msgs)
			yield return msg;

		if (quotes.Count > 0)
			yield return _depthBuilder.ProcessMarketData(securityId.Value, sendingTime, quotes, fullRefresh, mdReqId, lastBuildFrom);

		Level1ChangeMessage l1ResultMsg = null;

		foreach (var l1Msg in l1Msgs)
		{
			l1ResultMsg ??= new Level1ChangeMessage
			{
				SecurityId = securityId.Value,
				ServerTime = l1Msg.ServerTime,
				OriginalTransactionId = mdReqId ?? 0,
				BuildFrom = l1Msg.BuildFrom,
			};

			foreach (var change in l1Msg.Changes)
			{
				l1ResultMsg.Changes[change.Key] = change.Value;
			}
		}

		if (l1ResultMsg != null)
		{
			if (securityTradingStatus != null)
				l1ResultMsg.TryAdd(Level1Fields.State, FromSecurityTradingStatus(securityTradingStatus));

			yield return l1ResultMsg;
		}

		foreach (var candleMsg in candles)
		{
			candleMsg.SecurityId = securityId.Value;
			yield return candleMsg;
		}
	}

	private static async IAsyncEnumerable<Message> ReadMarketDataRequestRejectAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string mdReqId = null;
		char? mdReqRejReason = null;
		string text = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					mdReqId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MDReqRejReason:
					mdReqRejReason = await reader.ReadCharAsync(ct);
					return true;
				case FixTags.Text:
					text = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		var isNonSupported = mdReqRejReason == MDReqRejReason.UnsupportedMdEntryType;

		yield return new SubscriptionResponseMessage
		{
			OriginalTransactionId = mdReqId.To<long>(),
			Error = isNonSupported ? SubscriptionResponseMessage.NotSupported : new InvalidOperationException(LocalizedStrings.ErrorReceiveMarketData.Put(mdReqRejReason, text)),
		};
	}

	private async IAsyncEnumerable<Message> ReadSecurityMessageAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var result = reader.ReadSecurityMessageAsync(
			DateParser, YearMonthParser, _totalSecCountByRequestId,
			InitSecId, LogError, ProcessSecurityDefinitionAsync, GetSecurityType, cancellationToken);

		await foreach (var (msg, lastFragment, securityReqId, isError, reason, text) in result.WithEnforcedCancellation(cancellationToken))
		{
			if (msg != null)
				yield return msg;

			var transId = securityReqId ?? 0;

			if (isError)
			{
				yield return new SubscriptionResponseMessage
				{
					OriginalTransactionId = transId,
					Error = new InvalidOperationException(text.IsEmpty() ? reason : $"{reason}: {text}"),
				};
			}
			else if (lastFragment == true)
				yield return new SubscriptionFinishedMessage { OriginalTransactionId = transId };
		}
	}

	/// <summary>
	/// Convert <see cref="string"/> to <see cref="SecurityTypes"/> value.
	/// </summary>
	/// <param name="type"><see cref="string"/> value.</param>
	/// <returns><see cref="SecurityTypes"/> value.</returns>
	protected virtual SecurityTypes? GetSecurityType(string type) => type.FromFixType();

	/// <summary>
	/// Process <see cref="FixMessages.SecurityDefinition"/> message.
	/// </summary>
	/// <param name="tag">Tag.</param>
	/// <param name="reader">The reader of data recorded in the FIX protocol format.</param>
	/// <param name="message">A message containing info about the security.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Processing result.</returns>
	protected virtual ValueTask<bool> ProcessSecurityDefinitionAsync(FixTags tag, IFixReader reader, SecurityMessage message, CancellationToken cancellationToken)
	{
		return new(false);
	}

	/// <summary>
	/// Init security id information.
	/// </summary>
	/// <param name="message">A message containing info about the security.</param>
	/// <param name="symbol">Symbol.</param>
	/// <param name="securityExchange">Security exchange.</param>
	/// <param name="idSource">Id source.</param>
	/// <param name="idValue">Id value.</param>
	protected virtual void InitSecId(SecurityMessage message, string symbol, string securityExchange, string idSource, string idValue)
	{
		message.InitSecId(symbol, securityExchange, idSource, idValue);
	}

	private static async IAsyncEnumerable<Message> ReadQuoteRequestRejectAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		int? quoteRequestRejectReason = null;
		string quoteReqId = null;
		var symbols = new List<string>();

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.QuoteReqID:
					quoteReqId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.QuoteRequestRejectReason:
					quoteRequestRejectReason = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.NoRelatedSym:
					await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Symbol:
					symbols.Add(await reader.ReadStringAsync(ct));
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		yield return LocalizedStrings.NotCorrectlyHandleByFix.Put(quoteReqId, symbols.JoinComma(), quoteRequestRejectReason).ToErrorMessage();
	}

	private async IAsyncEnumerable<Message> ReadQuoteStatusReportAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		//int? quoteRequestRejectReason = null;
		string quoteStatusReqID = null;
		string quoteReqID = null;
		string quoteID = null;
		string symbol = null;
		string securityExchange = null;
		int? quoteType = null;
		DateTime? sendingTime = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.QuoteStatusReqID:
					quoteStatusReqID = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.QuoteReqID:
					quoteReqID = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.QuoteID:
					quoteID = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.QuoteType:
					quoteType = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Symbol:
					symbol = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityExchange:
					securityExchange = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SendingTime:
					sendingTime = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (sendingTime == null)
			throw new InvalidOperationException();

		yield return new Level1ChangeMessage
		{
			ServerTime = sendingTime.Value,
			SecurityId = new SecurityId
			{
				SecurityCode = symbol,
				BoardCode = securityExchange ?? ExchangeBoard ?? SecurityId.AssociatedBoardCode,
			}
		}.TryAdd(Level1Fields.State, quoteType.FromQuoteType());
	}

	private async IAsyncEnumerable<Message> ReadSecurityStatusAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string securityStatusReqId = null;
		string symbol = null;
		string securityExchange = null;
		DateTime? sendingTime = null;
		int? securityTradingStatus = null;
		decimal? highPx = null;
		decimal? lowPx = null;
		decimal? lastPx = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.SecurityStatusReqID:
					securityStatusReqId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Symbol:
					symbol = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityExchange:
					securityExchange = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityTradingStatus:
					securityTradingStatus = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.HighPx:
					highPx = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.LowPx:
					lowPx = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.LastPx:
					lastPx = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.SendingTime:
					sendingTime = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (sendingTime == null)
			throw new InvalidOperationException();

		yield return new Level1ChangeMessage
		{
			ServerTime = sendingTime.Value,
			SecurityId = new SecurityId
			{
				SecurityCode = symbol,
				BoardCode = securityExchange ?? ExchangeBoard ?? SecurityId.AssociatedBoardCode,
			}
		}
		.TryAdd(Level1Fields.HighPrice, highPx)
		.TryAdd(Level1Fields.LowPrice, lowPx)
		.TryAdd(Level1Fields.ClosePrice, lastPx)
		.TryAdd(Level1Fields.State, FromSecurityTradingStatus(securityTradingStatus));
	}

	/// <summary>
	/// Convert <see cref="FixTags.SecurityTradingStatus"/> to <see cref="SecurityStates"/> value.
	/// </summary>
	/// <param name="status"><see cref="FixTags.SecurityTradingStatus"/> value.</param>
	/// <returns><see cref="SecurityStates"/> value.</returns>
	protected virtual SecurityStates? FromSecurityTradingStatus(int? status)
	{
		return status switch
		{
			null => null,
			7 => (SecurityStates?)SecurityStates.Trading,
			_ => (SecurityStates?)SecurityStates.Stoped,
		};
	}

	private async IAsyncEnumerable<Message> ReadTradingSessionStatusAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string tradingSessionId = null;
		TradSesStatus? tradSesStatus = null;
		TradSesStatusRejReason? tradSesStatusRejReason = null;
		string tradSesReqId = null;
		DateTime? mdEntryTime = null;
		DateTime? sendingTime = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.TradingSessionID:
					tradingSessionId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.TradSesStatus:
					tradSesStatus = (TradSesStatus)await reader.ReadIntAsync(ct);
					return true;
				case FixTags.TradSesStatusRejReason:
					tradSesStatusRejReason = (TradSesStatusRejReason)await reader.ReadIntAsync(ct);
					return true;
				case FixTags.TradSesReqID:
					tradSesReqId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MDEntryTime:
					mdEntryTime = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				case FixTags.SendingTime:
					sendingTime = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (tradSesStatus == TradSesStatus.RequestRejected)
		{
			var msg = LocalizedStrings.ErrorReceiveState
				.Put(tradingSessionId,
					tradSesStatusRejReason == TradSesStatusRejReason.InvalidSession
						? LocalizedStrings.UnkSession
						: LocalizedStrings.Unk);

			yield return msg.ToErrorMessage();
		}
		else
		{
			yield return new BoardStateMessage
			{
				ServerTime = mdEntryTime ?? sendingTime.Value,
				BoardCode = ExchangeBoard ?? tradingSessionId,
				State = tradSesStatus?.FromFixStatus() ?? SessionStates.Active
			};
		}
	}
}