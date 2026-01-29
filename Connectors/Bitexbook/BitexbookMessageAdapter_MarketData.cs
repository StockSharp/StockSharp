namespace StockSharp.Bitexbook;

public partial class BitexbookMessageAdapter
{
	private readonly SynchronizedDictionary<string, SecurityId> _secIdMapping = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	protected override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var left = lookupMsg.Count ?? long.MaxValue;

		foreach (var symbol in await _httpClient.GetSymbols(cancellationToken))
		{
			await SendOutMessageAsync(new SecurityMessage
			{
				SecurityId = symbol.Alias.ToStockSharp(),
				MinVolume = (decimal?)symbol.MinAmount,
				OriginalTransactionId = lookupMsg.TransactionId,
				SecurityType = SecurityTypes.CryptoCurrency,
			}
			.TryFillUnderlyingId(symbol.CurrencyBase)
			.FillDefaultCryptoFields(), cancellationToken);

			if (--left <= 0)
				break;
		}

		SendSubscriptionResult(lookupMsg);
	}

	/// <inheritdoc />
	protected override async ValueTask OnOrderLogSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeTicker(mdMsg.TransactionId, symbol, cancellationToken);
			SendSubscriptionResult(mdMsg);
		}
		else
			await _pusherClient.UnSubscribeTicker(mdMsg.OriginalTransactionId, symbol, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var secId = mdMsg.SecurityId;
		var symbol = secId.ToNative();

		var tf = mdMsg.GetTimeFrame();
		var tfName = tf.ToNative();

		if (mdMsg.IsSubscribe)
		{
			if (mdMsg.From is not null)
			{
				var candles = await _httpClient.GetCandles(symbol, tfName, (long)mdMsg.From.Value.ToUnix(), (long)(mdMsg.To ?? DateTime.UtcNow).ToUnix(), cancellationToken);
				var left = mdMsg.Count ?? long.MaxValue;

				foreach (var candle in candles)
				{
					await ProcessCandle(candle, secId, tf, mdMsg.TransactionId, cancellationToken);

					if (--left <= 0)
						break;
				}
			}

			if (!mdMsg.IsHistoryOnly())
				_pusherClient.SubscribeCandles(symbol, tfName, cancellationToken);

			SendSubscriptionResult(mdMsg);
		}
		else
			_pusherClient.UnSubscribeCandles(symbol, tfName, cancellationToken);
	}

	private ValueTask ProcessCandle(Ohlc candle, SecurityId securityId, TimeSpan timeFrame, long originTransId, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = securityId,
			TypedArg = timeFrame,
			OpenPrice = (decimal)candle.Open,
			ClosePrice = (decimal)candle.Close,
			HighPrice = (decimal)candle.High,
			LowPrice = (decimal)candle.Low,
			TotalVolume = (decimal)candle.Volume,
			OpenTime = candle.Time,
			State = CandleStates.Finished,
			OriginalTransactionId = originTransId,
		}, cancellationToken);
	}

	private ValueTask SessionOnTickerChanged(Ticker ticker, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new Level1ChangeMessage
		{
			SecurityId = _secIdMapping[ticker.Symbol],
			ServerTime = ticker.Timestamp,
		}
		.TryAdd(Level1Fields.OpenPrice, (decimal?)ticker.Open)
		.TryAdd(Level1Fields.HighPrice, (decimal?)ticker.High)
		.TryAdd(Level1Fields.LowPrice, (decimal?)ticker.Low)
		.TryAdd(Level1Fields.ClosePrice, (decimal?)ticker.Close)
		.TryAdd(Level1Fields.Volume, (decimal?)ticker.Volume), cancellationToken);
	}

	private ValueTask SessionOnNewSymbols(IEnumerable<Symbol> symbols, CancellationToken cancellationToken)
	{
		_secIdMapping.Clear();

		foreach (var symbol in symbols)
		{
			_secIdMapping.Add(symbol.Code, symbol.Alias.ToStockSharp());
		}

		return default;
	}

	private ValueTask SessionOnNewTickerChange(TickerChange ticker, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new Level1ChangeMessage
		{
			SecurityId = _secIdMapping[ticker.Symbol],
			ServerTime = CurrentTimeUtc,
		}
		.TryAdd(Level1Fields.BestBidPrice, ticker.Bid?.ToDecimal())
		.TryAdd(Level1Fields.BestAskPrice, ticker.Ask?.ToDecimal()), cancellationToken);
	}

	private async ValueTask SessionOnTicketsActive(IEnumerable<Ticket> tickets, CancellationToken cancellationToken)
	{
		foreach (var ticket in tickets)
		{
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				SecurityId = _secIdMapping[ticket.Symbol],
				ServerTime = ticket.ModifyTimestamp ?? ticket.CreatedTimestamp ?? CurrentTimeUtc,
				OrderId = ticket.Id,
				OrderPrice = ticket.Price?.ToDecimal() ?? 0,
				OrderVolume = ticket.StartVolume?.ToDecimal(),
				Balance = ticket.Volume?.ToDecimal(),
				Side = ticket.Type.ToSide(),
				OrderState = OrderStates.Active,
			}, cancellationToken);
		}
	}

	private ValueTask SessionOnTicketAdded(Ticket ticket, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = _secIdMapping[ticket.Symbol],
			ServerTime = ticket.CreatedTimestamp ?? CurrentTimeUtc,
			OrderId = ticket.Id,
			OrderPrice = ticket.Price?.ToDecimal() ?? 0,
			OrderVolume = ticket.StartVolume?.ToDecimal(),
			Balance = ticket.Volume?.ToDecimal(),
			Side = ticket.Type.ToSide(),
			OrderState = OrderStates.Active,
		}, cancellationToken);
	}

	private ValueTask SessionOnTicketCanceled(Ticket ticket, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = _secIdMapping[ticket.Symbol],
			ServerTime = CurrentTimeUtc,
			OrderId = ticket.Id,
			OrderPrice = ticket.Price?.ToDecimal() ?? 0,
			Balance = ticket.Volume?.ToDecimal(),
			Side = ticket.Type.ToSide(),
			OrderState = OrderStates.Done,
		}, cancellationToken);
	}

	private ValueTask SessionOnTicketExecuted(Ticket ticket, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = _secIdMapping[ticket.Symbol],
			ServerTime = CurrentTimeUtc,
			OrderId = ticket.Id,
			Balance = ticket.OrderVolume?.ToDecimal(),
			TradePrice = ticket.Price?.ToDecimal() ?? 0,
			TradeVolume = ticket.StartVolume?.ToDecimal(),
			Side = ticket.Type.ToSide(),
			OrderState = OrderStates.Done,
		}, cancellationToken);
	}
}
