namespace StockSharp.Bitalong;

public partial class BitalongMessageAdapter
{
	private readonly HashSet<string> _orderBookSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<string, long?> _tradesSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly HashSet<string> _level1Subscriptions = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	protected override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		foreach (var pair in await _httpClient.GetSymbols(cancellationToken))
		{
			var symbol = pair.Value;

			var secMsg = new SecurityMessage
			{
				SecurityId = pair.Key.ToStockSharp(),
				Decimals = symbol.DecimalPlaces,
				MinVolume = (decimal)symbol.MinAmount,
				OriginalTransactionId = lookupMsg.TransactionId,
			}.FillDefaultCryptoFields();

			if (!secMsg.IsMatch(lookupMsg, secTypes))
				continue;

			await SendOutMessageAsync(secMsg, cancellationToken);

			await SendOutMessageAsync(new Level1ChangeMessage
			{
				SecurityId = pair.Key.ToStockSharp(),
				ServerTime = CurrentTime
			}
			.TryAdd(Level1Fields.CommissionMaker, (decimal)symbol.FeeSell)
			.TryAdd(Level1Fields.CommissionTaker, (decimal)symbol.FeeBuy), cancellationToken);

			if (--left <= 0)
				break;
		}

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		if (mdMsg.IsSubscribe)
		{
			await ProcessLevel1Subscriptions(new[] { symbol }, cancellationToken);

			if (!mdMsg.IsHistoryOnly())
				_level1Subscriptions.Add(symbol);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			_level1Subscriptions.Remove(symbol);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		if (mdMsg.IsSubscribe)
		{
			await ProcessTicksSubscription(mdMsg.TransactionId, symbol, cancellationToken);

			if (!mdMsg.IsHistoryOnly())
				_tradesSubscriptions.Add(symbol, 0);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
		{
			_tradesSubscriptions.Remove(symbol);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		if (mdMsg.IsSubscribe)
		{
			await ProcessOrderBookSubscription(symbol, cancellationToken);

			if (!mdMsg.IsHistoryOnly())
				_orderBookSubscriptions.Add(symbol);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
			_orderBookSubscriptions.Remove(symbol);
	}

	private async ValueTask ProcessSubscriptions(CancellationToken cancellationToken)
	{
		foreach (var symbol in _orderBookSubscriptions)
		{
			await ProcessOrderBookSubscription(symbol, cancellationToken);
		}

		foreach (var symbol in _tradesSubscriptions.Keys.ToArray())
		{
			await ProcessTicksSubscription(0, symbol, cancellationToken);
		}

		if (_level1Subscriptions.Count > 0)
		{
			await ProcessLevel1Subscriptions(_level1Subscriptions.ToArray(), cancellationToken);
		}
	}

	private async ValueTask ProcessOrderBookSubscription(string symbol, CancellationToken cancellationToken)
	{
		var book = await _httpClient.GetOrderBook(symbol, cancellationToken);

		await SendOutMessageAsync(new QuoteChangeMessage
		{
			SecurityId = symbol.ToStockSharp(),
			Bids = book.Bids?.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size)).ToArray() ?? Array.Empty<QuoteChange>(),
			Asks = book.Asks?.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size)).ToArray() ?? Array.Empty<QuoteChange>(),
			ServerTime = CurrentTime,
		}, cancellationToken);
	}

	private async ValueTask ProcessTicksSubscription(long transId, string symbol, CancellationToken cancellationToken)
	{
		var secId = symbol.ToStockSharp();

		var lastId = _tradesSubscriptions.TryGetValue(symbol);

		foreach (var trade in (await _httpClient.GetTradeHistory(symbol, cancellationToken)).OrderBy(t => t.Timestamp))
		{
			if (lastId != null && trade.Id <= lastId)
				continue;

			lastId = trade.Id;
			await ProcessTick(transId, secId, trade, cancellationToken);

			_tradesSubscriptions[symbol] = lastId;
		}
	}

	private async ValueTask ProcessLevel1Subscriptions(string[] symbols, CancellationToken cancellationToken)
	{
		ValueTask ProcessTicker(string symbol, Ticker ticker)
		{
			return SendOutMessageAsync(new Level1ChangeMessage
			{
				SecurityId = symbol.ToStockSharp(),
				ServerTime = CurrentTime,
			}
			.TryAdd(Level1Fields.HighPrice, (decimal?)ticker.High24)
			.TryAdd(Level1Fields.LowPrice, (decimal?)ticker.Low24)
			.TryAdd(Level1Fields.LastTradePrice, (decimal?)ticker.Last)
			.TryAdd(Level1Fields.HighBidPrice, (decimal?)ticker.HighestBid)
			.TryAdd(Level1Fields.LowAskPrice, (decimal?)ticker.LowestAsk)
			.TryAdd(Level1Fields.Volume, (decimal?)ticker.QuoteVolume)
			.TryAdd(Level1Fields.Change, (decimal?)ticker.PercentChange), cancellationToken);
		}

		if (symbols.Length > 2)
		{
			foreach (var pair in await _httpClient.GetTickers(cancellationToken))
			{
				await ProcessTicker(pair.Key, pair.Value);
			}
		}
		else
		{
			foreach (var symbol in symbols)
			{
				await ProcessTicker(symbol, await _httpClient.GetTicker(symbol, cancellationToken));
			}
		}
	}

	private ValueTask ProcessTick(long transactionId, SecurityId securityId, Native.Model.Trade trade, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = securityId,
			OriginSide = trade.Type.ToSide(),
			TradeId = trade.Id,
			TradePrice = (decimal)trade.Price,
			TradeVolume = (decimal)trade.Amount,
			ServerTime = trade.Timestamp,
			OriginalTransactionId = transactionId,
		}, cancellationToken);
	}
}