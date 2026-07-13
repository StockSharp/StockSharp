namespace StockSharp.CryptoHFTData;

public partial class CryptoHFTDataMessageAdapter
{
	/// <inheritdoc />
	protected override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		EnsureConnected();
		var symbols = (await _client.GetSymbols(Exchange, "trades", cancellationToken))
			.Union(await _client.GetSymbols(Exchange, "orderbook", cancellationToken))
			.Distinct(StringComparer.OrdinalIgnoreCase);
		var left = lookupMsg.Count ?? long.MaxValue;

		foreach (var symbol in symbols)
		{
			var security = new SecurityMessage
			{
				SecurityId = new() { SecurityCode = symbol, BoardCode = Exchange },
				Name = symbol,
				SecurityType = Exchange.Contains("futures", StringComparison.OrdinalIgnoreCase) || Exchange is "bybit" or "bitmex"
					? SecurityTypes.Future
					: SecurityTypes.CryptoCurrency,
				OriginalTransactionId = lookupMsg.TransactionId,
			};

			if (!security.IsMatch(lookupMsg, lookupMsg.GetSecurityTypes()))
				continue;

			await SendOutMessageAsync(security, cancellationToken);
			if (--left <= 0)
				break;
		}

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);
		if (!mdMsg.IsSubscribe)
			return;

		EnsureConnected();
		var (from, to) = GetRange(mdMsg);
		var left = mdMsg.Count ?? long.MaxValue;

		foreach (var trade in (await _client.GetTrades(Exchange, mdMsg.SecurityId.SecurityCode, from, to, cancellationToken)).OrderBy(t => t.TradeTime))
		{
			var time = FromUnixMilliseconds(trade.TradeTime);
			if (time < from || time > to)
				continue;

			await SendOutMessageAsync(ToExecutionMessage(trade, mdMsg.SecurityId, mdMsg.TransactionId), cancellationToken);

			if (--left <= 0)
				break;
		}

		await SendSubscriptionFinishedAsync(mdMsg.TransactionId, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		await SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);
		if (!mdMsg.IsSubscribe)
			return;

		EnsureConnected();
		var (from, to) = GetRange(mdMsg);
		var left = mdMsg.Count ?? long.MaxValue;
		var rows = await _client.GetOrderBook(Exchange, mdMsg.SecurityId.SecurityCode, from, to, cancellationToken);

		foreach (var group in GroupOrderBookRows(rows))
		{
			var time = FromUnixMilliseconds(group[0].EventTime);
			if (time < from || time > to)
				continue;

			await SendOutMessageAsync(ToQuoteChangeMessage(group, mdMsg.SecurityId, mdMsg.TransactionId), cancellationToken);

			if (--left <= 0)
				break;
		}

		await SendSubscriptionFinishedAsync(mdMsg.TransactionId, cancellationToken);
	}

	private static (DateTime from, DateTime to) GetRange(MarketDataMessage message)
	{
		if (message.From is null)
			throw new ArgumentException("Historical CryptoHFTData subscriptions require From.", nameof(message));
		return (message.From.Value.ToUniversalTime(), (message.To ?? message.From).Value.ToUniversalTime());
	}

	private static DateTime FromUnixMilliseconds(long value)
		=> DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;

	private static DateTime FromUnixNanoseconds(long value)
		=> DateTimeOffset.FromUnixTimeMilliseconds(value / 1_000_000).UtcDateTime.AddTicks(value % 1_000_000 / 100);

	internal static ExecutionMessage ToExecutionMessage(TradeRow trade, SecurityId securityId, long transactionId)
		=> new()
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = securityId,
			TradeId = trade.TradeId,
			TradePrice = trade.Price.To<decimal>(),
			TradeVolume = trade.Quantity.To<decimal>(),
			ServerTime = FromUnixMilliseconds(trade.TradeTime),
			LocalTime = FromUnixNanoseconds(trade.ReceivedTime),
			OriginSide = trade.IsBuyerMaker ? Sides.Sell : Sides.Buy,
			OriginalTransactionId = transactionId,
		};

	internal static QuoteChangeMessage ToQuoteChangeMessage(IReadOnlyCollection<OrderBookRow> rows, SecurityId securityId, long transactionId)
	{
		if (rows.Count == 0)
			throw new ArgumentException("An order-book event must contain at least one row.", nameof(rows));

		var first = rows.First();
		var changes = rows
			.Select(r => (row: r, quote: new QuoteChange(r.Price.To<decimal>(), r.Quantity.To<decimal>())))
			.ToArray();

		return new()
		{
			SecurityId = securityId,
			Bids = changes.Where(c => c.row.Side.EqualsIgnoreCase("bid")).Select(c => c.quote).ToArray(),
			Asks = changes.Where(c => c.row.Side.EqualsIgnoreCase("ask")).Select(c => c.quote).ToArray(),
			ServerTime = FromUnixMilliseconds(first.EventTime),
			SeqNum = first.SequenceNumber,
			State = first.EventType.EqualsIgnoreCase("snapshot")
				? QuoteChangeStates.SnapshotComplete
				: QuoteChangeStates.Increment,
			OriginalTransactionId = transactionId,
		};
	}

	internal static IEnumerable<IReadOnlyList<OrderBookRow>> GroupOrderBookRows(IEnumerable<OrderBookRow> rows)
	{
		foreach (var group in rows
			.OrderBy(r => r.EventTime)
			.GroupBy(r => new { r.EventTime, r.EventType, r.SequenceNumber }))
			yield return group.ToArray();
	}

	private void EnsureConnected()
	{
		if (_client is null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);
	}
}
