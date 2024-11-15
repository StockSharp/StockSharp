namespace StockSharp.Btce;

partial class BtceMessageAdapter
{
	private readonly SynchronizedSet<string> _orderBooks = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		var reply = await _httpClient.GetInstruments(cancellationToken);

		foreach (var info in reply.Items.Values)
		{
			var secId = info.Name.ToStockSharp();

			// NOTE сейчас BTCE транслирует для данного тикера
			// кол-во знаков после запятой 3 и мин цена 0.0001
			if (secId.SecurityCode.EqualsIgnoreCase("ltc/eur"))
				info.MinPrice = 0.001;

			//// NOTE сейчас BTCE транслирует для данного тикера
			//// кол-во знаков после запятой 2, но цены содержат 5 знаков
			//if (secId.SecurityCode.EqualsIgnoreCase("btc/cnh"))
			//	info.DecimalDigits = 5;

			//if (secId.SecurityCode.EqualsIgnoreCase("btc/usd"))
			//	info.DecimalDigits = 5;

			var minPrice = (decimal)info.MinPrice;

			var secMsg = new SecurityMessage
			{
				SecurityId = secId,
				Decimals = minPrice.GetCachedDecimals().Max(info.DecimalDigits),
				VolumeStep = 0.00000001m,
				MinVolume = info.MinVolume.ToDecimal(),
				SecurityType = SecurityTypes.CryptoCurrency,
				OriginalTransactionId = lookupMsg.TransactionId,
			};

			if (!secMsg.IsMatch(lookupMsg, secTypes))
				continue;

			SendOutMessage(secMsg);

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = reply.Timestamp.ApplyUtc()
			}
			.TryAdd(Level1Fields.MinPrice, minPrice)
			.TryAdd(Level1Fields.MaxPrice, info.MaxPrice.ToDecimal())
			.TryAdd(Level1Fields.CommissionTaker, info.Fee.ToDecimal())
			.Add(Level1Fields.State, info.IsHidden ? SecurityStates.Stoped : SecurityStates.Trading));

			if (--left <= 0)
				break;
		}

		SendSubscriptionResult(lookupMsg);
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var currency = mdMsg.SecurityId.ToCurrency();

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeOrderBook(mdMsg.TransactionId, currency, cancellationToken);

			SendSubscriptionResult(mdMsg);
		}
		else
			await _pusherClient.UnSubscribeOrderBook(mdMsg.OriginalTransactionId, currency, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var currency = mdMsg.SecurityId.ToCurrency();

		if (mdMsg.IsSubscribe)
		{
			if (mdMsg.From is not null)
			{
				var to = mdMsg.To ?? DateTimeOffset.UtcNow;
				var left = mdMsg.Count ?? long.MaxValue;

				var trades = (await _httpClient.GetTrades(5000, new[] { currency }, cancellationToken)).Items.TryGetValue(currency);

				if (trades != null)
				{
					foreach (var trade in trades.OrderBy(t => t.Timestamp))
					{
						if (trade.Timestamp < mdMsg.From)
							continue;

						if (trade.Timestamp > to)
							break;

						SendOutMessage(new ExecutionMessage
						{
							DataTypeEx = DataType.Ticks,
							SecurityId = mdMsg.SecurityId,
							TradeId = trade.Id,
							TradePrice = (decimal)trade.Price,
							TradeVolume = trade.Volume.ToDecimal(),
							ServerTime = trade.Timestamp,
							OriginSide = trade.Side.ToSide(),
							OriginalTransactionId = mdMsg.TransactionId,
						});

						if (--left <= 0)
							break;
					}
				}
			}
			
			if (!mdMsg.IsHistoryOnly())
				await _pusherClient.SubscribeTrades(mdMsg.TransactionId, currency, cancellationToken);

			SendSubscriptionResult(mdMsg);
		}
		else
			await _pusherClient.UnSubscribeTrades(mdMsg.OriginalTransactionId, currency, cancellationToken);
	}


	private void SessionOnOrderBookChanged(string ticker, OrderBook book)
	{
		var state = _orderBooks.TryAdd(ticker) ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment;

		static QuoteChange ToChange(OrderBookEntry entry)
			=> new(entry.Price, entry.Size);

		SendOutMessage(new QuoteChangeMessage
		{
			SecurityId = ticker.ToStockSharp(),
			Bids = book.Bids?.Select(ToChange).ToArray() ?? Array.Empty<QuoteChange>(),
			Asks = book.Asks?.Select(ToChange).ToArray() ?? Array.Empty<QuoteChange>(),
			State = state,
			ServerTime = CurrentTime.ConvertToUtc(),
		});
	}

	private void SessionOnNewTrades(string ticker, PusherTransaction[] trades)
	{
		foreach (var trade in trades)
		{
			SendOutMessage(new ExecutionMessage
			{
				SecurityId = ticker.ToStockSharp(),
				DataTypeEx = DataType.Ticks,
				TradePrice = trade.Price,
				TradeVolume = trade.Size,
				ServerTime = CurrentTime.ConvertToUtc(),
				OriginSide = trade.Side.ToSide()
			});	
		}
	}
}