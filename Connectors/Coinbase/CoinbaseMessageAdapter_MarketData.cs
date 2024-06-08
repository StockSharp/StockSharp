namespace StockSharp.Coinbase;

public partial class CoinbaseMessageAdapter
{
	/// <inheritdoc />
	public override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		var products = await _httpClient.GetProducts(cancellationToken);

		foreach (var product in products)
		{
			var secId = product.Id.ToStockSharp();

			var secMsg = new SecurityMessage
			{
				SecurityType = SecurityTypes.CryptoCurrency,
				SecurityId = secId,
				Name = product.DisplayName,
				PriceStep = product.QuoteIncrement,
				VolumeStep = 0.00000001m,
				MinVolume = product.BaseMinSize,
				OriginalTransactionId = lookupMsg.TransactionId,
			}
			.TryFillUnderlyingId(product.BaseCurrency.ToUpperInvariant());

			if (!secMsg.IsMatch(lookupMsg, secTypes))
				continue;

			SendOutMessage(secMsg);

			if (product.Status != "online")
			{
				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = CurrentTime.ConvertToUtc(),
				}.Add(Level1Fields.State, SecurityStates.Stoped));
			}

			if (--left <= 0)
				break;
		}

		SendSubscriptionResult(lookupMsg);
	}

	/// <inheritdoc />
	protected override ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var currency = mdMsg.SecurityId.ToCurrency();

		if (mdMsg.IsSubscribe)
			return _pusherClient.SubscribeTicker(currency, cancellationToken);
		else
			return _pusherClient.UnSubscribeTicker(currency, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var currency = mdMsg.SecurityId.ToCurrency();

		if (mdMsg.IsSubscribe)
			return _pusherClient.SubscribeOrderBook(currency, cancellationToken);
		else
			return _pusherClient.UnSubscribeOrderBook(currency, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var currency = mdMsg.SecurityId.ToCurrency();

		if (mdMsg.IsSubscribe)
		{
			//if (mdMsg.From != null && mdMsg.From.Value.IsToday())
			//{
			//	_httpClient.RequestTransactions(currency, "minute").Select(t => new ExecutionMessage
			//	{
			//		DataTypeEx = DataType.Ticks,
			//		SecurityId = mdMsg.SecurityId,
			//		TradeId = t.Id,
			//		TradePrice = (decimal)t.Price,
			//		TradeVolume = (decimal)t.Amount,
			//		ServerTime = t.Time.To<long>().FromUnix(),
			//	}).ForEach(SendOutMessage);
			//}
			//else
			return _pusherClient.SubscribeTrades(currency, cancellationToken);
		}
		else
		{
			return _pusherClient.UnSubscribeTrades(currency, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override ValueTask OnOrderLogSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var currency = mdMsg.SecurityId.ToCurrency();

		if (mdMsg.IsSubscribe)
			return _pusherClient.SubscribeOrderLog(currency, cancellationToken);
		else
			return _pusherClient.UnSubscribeOrderLog(currency, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		if (!mdMsg.IsSubscribe)
			return;

		var currency = mdMsg.SecurityId.ToCurrency();

		var candles = await _httpClient.GetCandles(currency, mdMsg.From, mdMsg.To, (int)mdMsg.GetTimeFrame().TotalSeconds, cancellationToken);
		var left = mdMsg.Count ?? long.MaxValue;

		foreach (var candle in candles.OrderBy(c => c.Time))
		{
			SendOutMessage(new TimeFrameCandleMessage
			{
				SecurityId = mdMsg.SecurityId,
				TypedArg = mdMsg.GetTimeFrame(),
				OpenPrice = candle.Open,
				ClosePrice = candle.Close,
				HighPrice = candle.High,
				LowPrice = candle.Low,
				TotalVolume = candle.Volume,
				OpenTime = candle.Time.FromUnix(),
				State = CandleStates.Finished,
				OriginalTransactionId = mdMsg.TransactionId,
			});

			if (--left <= 0)
				break;
		}

		SendSubscriptionFinished(mdMsg.TransactionId);
	}

	private void SessionOnTickerChanged(Ticker ticker)
	{
		SendOutMessage(new Level1ChangeMessage
		{
			SecurityId = ticker.Product.ToStockSharp(),
			ServerTime = ticker.Time ?? CurrentTime.ConvertToUtc(),
		}
		.TryAdd(Level1Fields.LastTradeId, ticker.LastTradeId)
		.TryAdd(Level1Fields.LastTradePrice, ticker.LastTradePrice?.ToDecimal())
		.TryAdd(Level1Fields.LastTradeVolume, ticker.LastTradePrice?.ToDecimal())
		.TryAdd(Level1Fields.LastTradeOrigin, ticker.LastTradeSide?.ToSide())
		.TryAdd(Level1Fields.HighPrice, ticker.High?.ToDecimal())
		.TryAdd(Level1Fields.LowPrice, ticker.Low?.ToDecimal())
		.TryAdd(Level1Fields.Volume, ticker.Volume?.ToDecimal())
		.TryAdd(Level1Fields.BestBidPrice, ticker.Bid?.ToDecimal())
		.TryAdd(Level1Fields.BestAskPrice, ticker.Ask?.ToDecimal()));
	}

	private void SessionOnNewTrade(Trade trade)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = trade.Product.ToStockSharp(),
			TradeId = trade.Id,
			TradePrice = trade.Price,
			TradeVolume = trade.Size,
			ServerTime = trade.Time,
			OriginSide = trade.Side.ToSide(),
		});
	}

	private void SessionOnOrderBookSnapshot(OrderBook book)
	{
		static QuoteChange ToChange(OrderBookEntry entry) => new(entry.Price, entry.Size);

		SendOutMessage(new QuoteChangeMessage
		{
			SecurityId = book.Product.ToStockSharp(),
			Bids = book.Bids?.Select(ToChange).ToArray() ?? Array.Empty<QuoteChange>(),
			Asks = book.Asks?.Select(ToChange).ToArray() ?? Array.Empty<QuoteChange>(),
			State = QuoteChangeStates.SnapshotComplete,
			ServerTime = CurrentTime.ConvertToUtc(),
		});
	}

	private void SessionOnOrderBookChanged(OrderBookChanges changes)
	{
		var bids = new List<QuoteChange>();
		var asks = new List<QuoteChange>();

		foreach (var entry in changes.Entries)
		{
			var side = entry.Side.ToSide();

			var quotes = side == Sides.Buy ? bids : asks;

			quotes.Add(new QuoteChange(entry.Price, entry.Size));
		}

		SendOutMessage(new QuoteChangeMessage
		{
			SecurityId = changes.Product.ToStockSharp(),
			Bids = bids.ToArray(),
			Asks = asks.ToArray(),
			ServerTime = CurrentTime.ConvertToUtc(),
			State = QuoteChangeStates.Increment,
		});
	}

	private void SessionOnNewOrderLog(OrderLog log)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = log.Product.ToStockSharp(),
			ServerTime = log.Time,
			OrderStringId = log.OrderId,
			OrderPrice = log.Price ?? 0,
			OrderVolume = log.Size,
			Balance = log.RemainingSize,
			OrderType = log.OrderType?.ToOrderType(),
			OrderState = log.Reason.IsEmpty() ? OrderStates.Active : log.Reason.ToOrderState(),
			Side = log.Side.ToSide(),
		});
	}

	private void SessionOnHeartbeat(Heartbeat heartbeat)
	{
		
	}
}