namespace StockSharp.Bibox;

public partial class BiboxMessageAdapter
{
	private readonly SynchronizedPairSet<(SecurityId, TimeSpan), long> _candlesTransactions = [];

	/// <inheritdoc />
	protected override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		foreach (var symbol in await _httpClient.GetSymbols(cancellationToken))
		{
			var secMsg = new SecurityMessage
			{
				SecurityId = symbol.Code.ToStockSharp(),
				MinVolume = symbol.MinQuantity?.ToDecimal(),
				MaxVolume = symbol.MaxQuantity?.ToDecimal(),
				PriceStep = symbol.PriceIncrement?.ToDecimal(),
				VolumeStep = symbol.QuantityIncrement?.ToDecimal(),
				SecurityType = SecurityTypes.Future,
				Decimals = symbol.PriceScale,
				OriginalTransactionId = lookupMsg.TransactionId,
			};

			if (!secMsg.IsMatch(lookupMsg, secTypes))
				continue;

			await SendOutMessageAsync(secMsg, cancellationToken);

			if (--left <= 0)
				break;
		}

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		var symbol = mdMsg.SecurityId.ToSymbol();

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeTicker(mdMsg.TransactionId, symbol, cancellationToken);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			await _pusherClient.UnSubscribeTicker(mdMsg.TransactionId, mdMsg.OriginalTransactionId, symbol, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		var symbol = mdMsg.SecurityId.ToSymbol();
		var depth = mdMsg.MaxDepth ?? 20;

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeOrderBook(mdMsg.TransactionId, symbol, depth, cancellationToken);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			await _pusherClient.UnSubscribeOrderBook(mdMsg.TransactionId, mdMsg.OriginalTransactionId, symbol, depth, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		var symbol = mdMsg.SecurityId.ToSymbol();

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeTrades(mdMsg.TransactionId, symbol, cancellationToken);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			await _pusherClient.UnSubscribeTrades(mdMsg.TransactionId, mdMsg.OriginalTransactionId, symbol, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		var symbol = mdMsg.SecurityId.ToSymbol();
		var tf = mdMsg.GetTimeFrame();
		var tfName = tf.ToNative();

		if (mdMsg.IsSubscribe)
		{
			if (mdMsg.From is DateTime from)
			{
				var to = mdMsg.To ?? DateTime.UtcNow;
				var last = from;
				var left = mdMsg.Count ?? long.MaxValue;

				while (true)
				{
					var needBreak = false;

					foreach (var candle in (await _httpClient.GetCandles(symbol, tfName, (long)last.ToUnix(), cancellationToken)).OrderBy(c => c.Time))
					{
						var time = candle.Time.FromUnix(false);

						if (last < from)
							continue;

						if (time > to)
						{
							needBreak = true;
							break;
						}

						await SendOutMessageAsync(new TimeFrameCandleMessage
						{
							SecurityId = mdMsg.SecurityId,
							TypedArg = tf,
							OpenPrice = candle.Open.ToDecimal() ?? 0,
							ClosePrice = candle.Close.ToDecimal() ?? 0,
							HighPrice = candle.High.ToDecimal() ?? 0,
							LowPrice = candle.Low.ToDecimal() ?? 0,
							TotalVolume = candle.Volume.ToDecimal() ?? 0,
							TotalTicks = candle.TradeCount,
							OpenTime = time,
							State = CandleStates.Finished,
							OriginalTransactionId = mdMsg.TransactionId,
						}, cancellationToken);

						last = time;

						if (--left <= 0)
						{
							needBreak = true;
							break;
						}
					}

					if (needBreak)
						break;
				}
			}

			if (!mdMsg.IsHistoryOnly())
			{
				_candlesTransactions.Add((mdMsg.SecurityId, tf), mdMsg.TransactionId);
				await _pusherClient.SubscribeCandles(mdMsg.TransactionId, symbol, tfName, cancellationToken);
			}

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			await _pusherClient.UnSubscribeCandles(mdMsg.TransactionId, mdMsg.OriginalTransactionId, symbol, tfName, cancellationToken);
	}

	private ValueTask SessionOnTickerChanged(Ticker ticker, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new Level1ChangeMessage
		{
			SecurityId = ticker.Pair.ToStockSharp(),
			ServerTime = ticker.Timestamp,
		}
		.TryAdd(Level1Fields.HighPrice, ticker.High?.ToDecimal())
		.TryAdd(Level1Fields.LowPrice, ticker.Low?.ToDecimal())
		.TryAdd(Level1Fields.LastTradePrice, ticker.Last?.ToDecimal())
		.TryAdd(Level1Fields.BestBidPrice, ticker.Buy?.ToDecimal())
		.TryAdd(Level1Fields.BestBidVolume, ticker.BuyAmount?.ToDecimal())
		.TryAdd(Level1Fields.BestAskPrice, ticker.Sell?.ToDecimal())
		.TryAdd(Level1Fields.BestAskTime, ticker.SellAmount?.ToDecimal())
		.TryAdd(Level1Fields.Volume, ticker.Volume?.ToDecimal()), cancellationToken);
	}

	private async ValueTask SessionOnNewTrades(string currencyPair, IEnumerable<Trade> trades, CancellationToken cancellationToken)
	{
		var secId = currencyPair.ToStockSharp();

		foreach (var trade in trades)
		{
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				ServerTime = trade.Time,
				TradeId = trade.Id,
				TradePrice = (decimal)trade.Price,
				TradeVolume = (decimal)trade.Amount,
				OriginSide = trade.Side.ToSide(),
			}, cancellationToken);
		}
	}

	private async ValueTask SessionOnNewCandles(string currencyPair, string timeFrame, IEnumerable<Ohlc> candles, CancellationToken cancellationToken)
	{
		var secId = currencyPair.ToStockSharp();
		var tf = timeFrame.ToTimeFrame();

		if (!_candlesTransactions.TryGetValue((secId, tf), out var originTransId))
			return;

		foreach (var candle in candles.OrderBy(c => c.Time))
		{
			await SendOutMessageAsync(new TimeFrameCandleMessage
			{
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = candle.Open.ToDecimal() ?? 0,
				ClosePrice = candle.Close.ToDecimal() ?? 0,
				HighPrice = candle.High.ToDecimal() ?? 0,
				LowPrice = candle.Low.ToDecimal() ?? 0,
				TotalVolume = candle.Volume.ToDecimal() ?? 0,
				TotalTicks = candle.TradeCount,
				OpenTime = candle.Time.FromUnix(false),
				State = CandleStates.Finished,
				OriginalTransactionId = originTransId,
			}, cancellationToken);
		}
	}

	private ValueTask SessionOnOrderBookChanged(string currencyPair, bool isSnapshot, OrderBook book, CancellationToken cancellationToken)
	{
		static QuoteChange ToChange(OrderBookEntry entry)
			=> new((decimal)entry.Price, (decimal)entry.Size);

		return SendOutMessageAsync(new QuoteChangeMessage
		{
			SecurityId = currencyPair.ToStockSharp(),
			Bids = book.Bids?.Select(ToChange).ToArray() ?? [],
			Asks = book.Asks?.Select(ToChange).ToArray() ?? [],
			ServerTime = CurrentTime,
		}, cancellationToken);
	}
}