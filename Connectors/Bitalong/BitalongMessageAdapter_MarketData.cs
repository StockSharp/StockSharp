namespace StockSharp.Bitalong;

public partial class BitalongMessageAdapter
{
	private readonly HashSet<string> _orderBookSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<string, long?> _tradesSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly HashSet<string> _level1Subscriptions = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
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

			SendOutMessage(secMsg);

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = pair.Key.ToStockSharp(),
				ServerTime = CurrentTime.ConvertToUtc()
			}
			.TryAdd(Level1Fields.CommissionMaker, (decimal)symbol.FeeSell)
			.TryAdd(Level1Fields.CommissionTaker, (decimal)symbol.FeeBuy));

			if (--left <= 0)
				break;
		}

		SendSubscriptionResult(lookupMsg);
	}

	/// <inheritdoc />
	protected override async ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		if (mdMsg.IsSubscribe)
		{
			await ProcessLevel1Subscriptions(new[] { symbol }, cancellationToken);

			if (!mdMsg.IsHistoryOnly())
				_level1Subscriptions.Add(symbol);

			SendSubscriptionResult(mdMsg);
		}
		else
			_level1Subscriptions.Remove(symbol);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		if (mdMsg.IsSubscribe)
		{
			await ProcessTicksSubscription(mdMsg.TransactionId, symbol, cancellationToken);

			if (!mdMsg.IsHistoryOnly())
				_tradesSubscriptions.Add(symbol, 0);

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			_tradesSubscriptions.Remove(symbol);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		if (mdMsg.IsSubscribe)
		{
			await ProcessOrderBookSubscription(symbol, cancellationToken);

			if (!mdMsg.IsHistoryOnly())
				_orderBookSubscriptions.Add(symbol);

			SendSubscriptionResult(mdMsg);
		}
		else
			_orderBookSubscriptions.Remove(symbol);
	}

	private async Task ProcessSubscriptions(CancellationToken cancellationToken)
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

	private async Task ProcessOrderBookSubscription(string symbol, CancellationToken cancellationToken)
	{
		var book = await _httpClient.GetOrderBook(symbol, cancellationToken);

		SendOutMessage(new QuoteChangeMessage
		{
			SecurityId = symbol.ToStockSharp(),
			Bids = book.Bids?.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size)).ToArray() ?? Array.Empty<QuoteChange>(),
			Asks = book.Asks?.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size)).ToArray() ?? Array.Empty<QuoteChange>(),
			ServerTime = CurrentTime.ConvertToUtc(),
		});
	}

	private async Task ProcessTicksSubscription(long transId, string symbol, CancellationToken cancellationToken)
	{
		var secId = symbol.ToStockSharp();

		var lastId = _tradesSubscriptions.TryGetValue(symbol);

		foreach (var trade in (await _httpClient.GetTradeHistory(symbol, cancellationToken)).OrderBy(t => t.Timestamp))
		{
			if (lastId != null && trade.Id <= lastId)
				continue;

			lastId = trade.Id;
			ProcessTick(transId, secId, trade);

			_tradesSubscriptions[symbol] = lastId;
		}
	}

	private async Task ProcessLevel1Subscriptions(string[] symbols, CancellationToken cancellationToken)
	{
		void ProcessTicker(string symbol, Ticker ticker)
		{
			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = symbol.ToStockSharp(),
				ServerTime = CurrentTime.ConvertToUtc(),
			}
			.TryAdd(Level1Fields.HighPrice, (decimal?)ticker.High24)
			.TryAdd(Level1Fields.LowPrice, (decimal?)ticker.Low24)
			.TryAdd(Level1Fields.LastTradePrice, (decimal?)ticker.Last)
			.TryAdd(Level1Fields.HighBidPrice, (decimal?)ticker.HighestBid)
			.TryAdd(Level1Fields.LowAskPrice, (decimal?)ticker.LowestAsk)
			.TryAdd(Level1Fields.Volume, (decimal?)ticker.QuoteVolume)
			.TryAdd(Level1Fields.Change, (decimal?)ticker.PercentChange));
		}
		
		if (symbols.Length > 2)
		{
			foreach (var pair in await _httpClient.GetTickers(cancellationToken))
			{
				ProcessTicker(pair.Key, pair.Value);
			}
		}
		else
		{
			foreach (var symbol in symbols)
			{
				ProcessTicker(symbol, await _httpClient.GetTicker(symbol, cancellationToken));
			}
		}
	}

	private void ProcessTick(long transactionId, SecurityId securityId, Native.Model.Trade trade)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = securityId,
			OriginSide = trade.Type.ToSide(),
			TradeId = trade.Id,
			TradePrice = (decimal)trade.Price,
			TradeVolume = (decimal)trade.Amount,
			ServerTime = trade.Timestamp,
			OriginalTransactionId = transactionId,
		});
	}
}