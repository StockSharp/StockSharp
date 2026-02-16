namespace StockSharp.ZB;

partial class ZBMessageAdapter
{
	/// <inheritdoc />
	protected override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		foreach (var pair in await _httpClient.GetSymbolsAsync(cancellationToken))
		{
			var secMsg = new SecurityMessage
			{
				SecurityId = pair.Key.ToStockSharp(),
				OriginalTransactionId = lookupMsg.TransactionId,
			}.FillDefaultCryptoFields();

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

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeTickerAsync(mdMsg.SecurityId.ToSymbol(), cancellationToken);
			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			await _pusherClient.UnSubscribeTickerAsync(mdMsg.SecurityId.ToSymbol(), cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeTradesAsync(mdMsg.SecurityId.ToSymbol(), cancellationToken);
			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			await _pusherClient.UnSubscribeTradesAsync(mdMsg.SecurityId.ToSymbol(), cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeOrderBookAsync(mdMsg.SecurityId.ToSymbol(), cancellationToken);
			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			await _pusherClient.UnSubscribeOrderBookAsync(mdMsg.SecurityId.ToSymbol(), cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			var candles = await _httpClient.GetCandlesAsync(mdMsg.SecurityId.ToSymbol(false), cancellationToken);
			foreach (var candle in candles)
			{
				await SendOutMessageAsync(new TimeFrameCandleMessage
				{
					OpenPrice = candle.Open.ToDecimal() ?? 0,
					ClosePrice = candle.Close.ToDecimal() ?? 0,
					HighPrice = candle.High.ToDecimal() ?? 0,
					LowPrice = candle.Low.ToDecimal() ?? 0,
					TotalVolume = candle.Volume.ToDecimal() ?? 0,
					OpenTime = candle.Time.FromUnix(false),
					State = CandleStates.Finished,
					OriginalTransactionId = mdMsg.TransactionId,
				}, cancellationToken);
			}

			await SendSubscriptionFinishedAsync(mdMsg.TransactionId, cancellationToken);
		}
	}

	private ValueTask SessionOnTickerChanged(string symbol, DateTime time, Ticker ticker, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new Level1ChangeMessage
		{
			SecurityId = symbol.ToStockSharp(),
			ServerTime = time,
		}
		.TryAdd(Level1Fields.BestBidPrice, ticker.Buy?.ToDecimal())
		.TryAdd(Level1Fields.BestAskPrice, ticker.Sell?.ToDecimal())
		.TryAdd(Level1Fields.HighPrice, ticker.High?.ToDecimal())
		.TryAdd(Level1Fields.LowPrice, ticker.Low?.ToDecimal())
		.TryAdd(Level1Fields.LastTradePrice, ticker.Last?.ToDecimal())
		.TryAdd(Level1Fields.Volume, ticker.Vol?.ToDecimal()), cancellationToken);
	}

	private ValueTask SessionOnOrderBookChanged(string symbol, OrderBook book, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new QuoteChangeMessage
		{
			SecurityId = symbol.ToStockSharp(),
			Bids = [.. book.Bids.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size))],
			Asks = [.. book.Asks.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size))],
			ServerTime = CurrentTime,
		}, cancellationToken);
	}

	private async ValueTask SessionOnNewTrades(string symbol, IEnumerable<Trade> trades, CancellationToken cancellationToken)
	{
		var secId = symbol.ToStockSharp();

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
				Side = trade.Type.ToSide(),
			}, cancellationToken);
		}
	}
}