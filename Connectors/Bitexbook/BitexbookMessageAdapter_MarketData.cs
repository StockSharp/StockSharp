namespace StockSharp.Bitexbook;

public partial class BitexbookMessageAdapter
{
	private readonly SynchronizedDictionary<string, SecurityId> _secIdMapping = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var left = lookupMsg.Count ?? long.MaxValue;

		foreach (var symbol in await _httpClient.GetSymbols(cancellationToken))
		{
			SendOutMessage(new SecurityMessage
			{
				SecurityId = symbol.Alias.ToStockSharp(),
				MinVolume = (decimal?)symbol.MinAmount,
				OriginalTransactionId = lookupMsg.TransactionId,
				SecurityType = SecurityTypes.CryptoCurrency,
			}
			.TryFillUnderlyingId(symbol.CurrencyBase)
			.FillDefaultCryptoFields());

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
				var candles = await _httpClient.GetCandles(symbol, tfName, (long)mdMsg.From.Value.ToUnix(), (long)(mdMsg.To ?? DateTimeOffset.UtcNow).ToUnix(), cancellationToken);
				var left = mdMsg.Count ?? long.MaxValue;

				foreach (var candle in candles)
				{
					ProcessCandle(candle, secId, tf, mdMsg.TransactionId);

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

	private void ProcessCandle(Ohlc candle, SecurityId securityId, TimeSpan timeFrame, long originTransId)
	{
		SendOutMessage(new TimeFrameCandleMessage
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
		});
	}

	private void SessionOnTickerChanged(Ticker ticker)
	{
		SendOutMessage(new Level1ChangeMessage
		{
			SecurityId = _secIdMapping[ticker.Symbol],
			ServerTime = ticker.Timestamp,
		}
		.TryAdd(Level1Fields.OpenPrice, (decimal?)ticker.Open)
		.TryAdd(Level1Fields.HighPrice, (decimal?)ticker.High)
		.TryAdd(Level1Fields.LowPrice, (decimal?)ticker.Low)
		.TryAdd(Level1Fields.ClosePrice, (decimal?)ticker.Close)
		.TryAdd(Level1Fields.Volume, (decimal?)ticker.Volume));
	}

	private void SessionOnNewSymbols(IEnumerable<Symbol> symbols)
	{
		_secIdMapping.Clear();

		foreach (var symbol in symbols)
		{
			_secIdMapping.Add(symbol.Code, symbol.Alias.ToStockSharp());
		}
	}

	private void SessionOnNewTickerChange(TickerChange ticker)
	{
		SendOutMessage(new Level1ChangeMessage
		{
			SecurityId = _secIdMapping[ticker.Symbol],
			ServerTime = CurrentTime.ConvertToUtc(),
		}
		.TryAdd(Level1Fields.BestBidPrice, ticker.Bid?.ToDecimal())
		.TryAdd(Level1Fields.BestAskPrice, ticker.Ask?.ToDecimal()));
	}

	private void SessionOnTicketsActive(IEnumerable<Ticket> tickets)
	{
		foreach (var ticket in tickets)
		{
			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				SecurityId = _secIdMapping[ticket.Symbol],
				ServerTime = ticket.ModifyTimestamp ?? ticket.CreatedTimestamp ?? CurrentTime.ConvertToUtc(),
				OrderId = ticket.Id,
				OrderPrice = ticket.Price?.ToDecimal() ?? 0,
				OrderVolume = ticket.StartVolume?.ToDecimal(),
				Balance = ticket.Volume?.ToDecimal(),
				Side = ticket.Type.ToSide(),
				OrderState = OrderStates.Active,
			});
		}
	}

	private void SessionOnTicketAdded(Ticket ticket)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = _secIdMapping[ticket.Symbol],
			ServerTime = ticket.CreatedTimestamp ?? CurrentTime.ConvertToUtc(),
			OrderId = ticket.Id,
			OrderPrice = ticket.Price?.ToDecimal() ?? 0,
			OrderVolume = ticket.StartVolume?.ToDecimal(),
			Balance = ticket.Volume?.ToDecimal(),
			Side = ticket.Type.ToSide(),
			OrderState = OrderStates.Active,
		});
	}

	private void SessionOnTicketCanceled(Ticket ticket)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = _secIdMapping[ticket.Symbol],
			ServerTime = CurrentTime.ConvertToUtc(),
			OrderId = ticket.Id,
			OrderPrice = ticket.Price?.ToDecimal() ?? 0,
			Balance = ticket.Volume?.ToDecimal(),
			Side = ticket.Type.ToSide(),
			OrderState = OrderStates.Done,
		});
	}

	private void SessionOnTicketExecuted(Ticket ticket)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = _secIdMapping[ticket.Symbol],
			ServerTime = CurrentTime.ConvertToUtc(),
			OrderId = ticket.Id,
			Balance = ticket.OrderVolume?.ToDecimal(),
			TradePrice = ticket.Price?.ToDecimal() ?? 0,
			TradeVolume = ticket.StartVolume?.ToDecimal(),
			Side = ticket.Type.ToSide(),
			OrderState = OrderStates.Done,
		});
	}
}