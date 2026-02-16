namespace StockSharp.Bittrex;

public partial class BittrexMessageAdapter
{
	private readonly SynchronizedSet<SecurityId> _orderBooks = [];

	private readonly HashSet<SecurityId> _wsSubscriptions = [];
	private readonly SynchronizedSet<SecurityId> _wsBookSubscriptions = [];
	private readonly SynchronizedSet<SecurityId> _wsTradesSubscriptions = [];

	private bool _summarySubscribed;

	/// <inheritdoc />
	protected override async ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			if (!_summarySubscribed)
			{
				await _pusherClient.SubscribeToSummaryDeltasAsync();
				_summarySubscribed = true;
			}

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var secId = mdMsg.SecurityId;

		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			_wsBookSubscriptions.Add(secId);

			if (_wsSubscriptions.Add(secId))
				await SubscribeToExchangeDeltasAsync(secId, cancellationToken);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			_wsBookSubscriptions.Remove(secId);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var secId = mdMsg.SecurityId;
		var transId = mdMsg.TransactionId;

		await SendSubscriptionReplyAsync(transId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			if (mdMsg.To != null)
			{
				var trades = await _httpClient.GetMarketHistoryAsync(secId.ToSymbol(), cancellationToken);

				foreach (var trade in trades.OrderBy(t => t.Id))
				{
					await SendOutMessageAsync(new ExecutionMessage
					{
						DataTypeEx = DataType.Ticks,
						SecurityId = secId,
						TradeId = trade.Id,
						TradePrice = trade.Price,
						TradeVolume = trade.Quantity,
						ServerTime = trade.Timestamp.UtcKind(),
						OriginSide = trade.OrderType.ToSide(),
						OriginalTransactionId = transId,
					}, cancellationToken);
				}
			}
			else
			{
				_wsTradesSubscriptions.Add(secId);

				if (_wsSubscriptions.Add(secId))
					await SubscribeToExchangeDeltasAsync(secId, cancellationToken);
			}

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var secId = mdMsg.SecurityId;
		var transId = mdMsg.TransactionId;

		await SendSubscriptionReplyAsync(transId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			var tf = mdMsg.GetTimeFrame();
			var tfName = tf.ToNative();

			var candles = await _httpClient.GetCandlesAsync(secId.ToSymbol(), tfName, (long?)mdMsg.From?.ToUnix(false), cancellationToken);

			foreach (var candle in candles)
			{
				await ProcessCandleAsync(candle, secId, tf, transId, cancellationToken);
			}

			await SendSubscriptionFinishedAsync(transId, cancellationToken);
		}
	}

	private async ValueTask SubscribeToExchangeDeltasAsync(SecurityId secId, CancellationToken cancellationToken)
	{
		var symbol = secId.ToSymbol();
		var book = await _pusherClient.QueryExchangeStateAsync(symbol);
		await ProcessOrderBookAsync(secId, book, cancellationToken);
		await _pusherClient.SubscribeToExchangeDeltasAsync(symbol);
	}

	private ValueTask ProcessCandleAsync(Candle candle, SecurityId securityId, TimeSpan timeFrame, long originTransId, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = securityId,
			TypedArg = timeFrame,
			OpenPrice = candle.Open.ToDecimal() ?? 0,
			ClosePrice = candle.Close.ToDecimal() ?? 0,
			HighPrice = candle.High.ToDecimal() ?? 0,
			LowPrice = candle.Low.ToDecimal() ?? 0,
			TotalVolume = candle.Volume24H.ToDecimal() ?? 0,
			OpenTime = candle.Timestamp,
			State = CandleStates.Finished,
			OriginalTransactionId = originTransId,
		}, cancellationToken);
	}

	private async ValueTask SessionOnNewTrade(string currencyPair, WsFill trade, CancellationToken cancellationToken)
	{
		var secId = currencyPair.ToStockSharp();

		if (!_wsTradesSubscriptions.Contains(secId))
			return;

		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradeId = trade.Id,
			TradePrice = (decimal)trade.Rate,
			TradeVolume = (decimal)trade.Quantity,
			OriginSide = trade.OrderType.ToSide(),
			ServerTime = CurrentTime,
		}, cancellationToken);
	}

	private ValueTask SessionOnOrderBookChanged(WsOrderBook wsBook, CancellationToken cancellationToken)
	{
		return ProcessOrderBookAsync(wsBook.Market.ToStockSharp(), wsBook, cancellationToken);
	}

	private ValueTask ProcessOrderBookAsync(SecurityId secId, WsOrderBook book, CancellationToken cancellationToken)
	{
		var state = _orderBooks.TryAdd(secId) ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment;

		QuoteChange ToChange(WsOrderBookEntry entry)
			=> new((decimal)entry.Rate, entry.Type == 1 ? 0 : (decimal)entry.Quantity);

		//if (!_wsBookSubscriptions.Contains(secId))
		//	return;

		return SendOutMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			Bids = [.. book.Bids?.Select(ToChange) ?? []],
			Asks = [.. book.Asks?.Select(ToChange) ?? []],
			State = state,
			ServerTime = CurrentTime,
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		var currencies = (await _httpClient.GetCurrenciesAsync(cancellationToken)).ToDictionary(c => c.Currency, StringComparer.InvariantCultureIgnoreCase);

		var markets = (await _httpClient.GetMarketsAsync(cancellationToken)).ToArray();

		foreach (var market in markets)
		{
			var secMsg = new SecurityMessage
			{
				SecurityId = market.MarketName.ToStockSharp(),
				Name = market.MarketCurrencyLong,
				MinVolume = market.MinTradeSize,
				OriginalTransactionId = lookupMsg.TransactionId
			}
			.TryFillUnderlyingId(market.BaseCurrency.ToUpperInvariant())
			.FillDefaultCryptoFields();

			if (!secMsg.IsMatch(lookupMsg, secTypes))
				continue;

			await SendOutMessageAsync(secMsg, cancellationToken);

			if (--left <= 0)
				break;
		}

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}

	private ValueTask SessionOnTickerChanged(WsTicker ticker, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new Level1ChangeMessage
		{
			SecurityId = ticker.Market.ToStockSharp(),
			ServerTime = ticker.TimeStamp,
		}
		.TryAdd(Level1Fields.HighPrice, ticker.High.ToDecimal())
		.TryAdd(Level1Fields.LowPrice, ticker.Low.ToDecimal())
		.TryAdd(Level1Fields.ClosePrice, ticker.Last.ToDecimal())
		.TryAdd(Level1Fields.BestBidPrice, ticker.Bid.ToDecimal())
		.TryAdd(Level1Fields.BestAskPrice, ticker.Ask.ToDecimal())
		.TryAdd(Level1Fields.BidsCount, ticker.OpenBuyOrders)
		.TryAdd(Level1Fields.AsksCount, ticker.OpenSellOrders)
		.TryAdd(Level1Fields.Volume, ticker.Volume.ToDecimal())
		.TryAdd(Level1Fields.HighPrice, ticker.High.ToDecimal())
		, cancellationToken);
	}
}
