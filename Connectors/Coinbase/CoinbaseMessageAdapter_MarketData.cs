namespace StockSharp.Coinbase;

public partial class CoinbaseMessageAdapter
{
	private readonly SynchronizedDictionary<string, long> _candlesTransIds = new();

	/// <inheritdoc />
	public override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		foreach (var type in new[] { "SPOT", "FUTURE" })
		{
			var products = await _restClient.GetProducts(type, cancellationToken);

			foreach (var product in products)
			{
				var secId = product.ProductId.ToStockSharp();

				var secMsg = new SecurityMessage
				{
					SecurityType = product.ProductType.ToSecurityType(),
					SecurityId = secId,
					Name = product.DisplayName,
					PriceStep = product.QuoteIncrement?.ToDecimal(),
					VolumeStep = product.BaseIncrement?.ToDecimal(),
					MinVolume = product.BaseMinSize?.ToDecimal(),
					MaxVolume = product.BaseMaxSize?.ToDecimal(),
					ExpiryDate = product.FutureProductDetails?.ContractExpiry,
					Multiplier = product.FutureProductDetails?.ContractSize?.ToDecimal(),
					OriginalTransactionId = lookupMsg.TransactionId,
				}
				.TryFillUnderlyingId(product.BaseCurrencyId.ToUpperInvariant());

				if (!secMsg.IsMatch(lookupMsg, secTypes))
					continue;

				SendOutMessage(secMsg);

				if (--left <= 0)
					break;
			}

			if (left <= 0)
				break;
		}

		SendSubscriptionResult(lookupMsg);
	}

	/// <inheritdoc />
	protected override async ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var symbol = mdMsg.SecurityId.ToSymbol();

		if (mdMsg.IsSubscribe)
		{
			await _socketClient.SubscribeTicker(mdMsg.TransactionId, symbol, cancellationToken);

			SendSubscriptionResult(mdMsg);
		}
		else
			await _socketClient.UnSubscribeTicker(mdMsg.OriginalTransactionId, symbol, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var symbol = mdMsg.SecurityId.ToSymbol();

		if (mdMsg.IsSubscribe)
		{
			await _socketClient.SubscribeOrderBook(mdMsg.TransactionId, symbol, cancellationToken);

			SendSubscriptionResult(mdMsg);
		}
		else
			await _socketClient.UnSubscribeOrderBook(mdMsg.OriginalTransactionId, symbol, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var symbol = mdMsg.SecurityId.ToSymbol();

		if (mdMsg.IsSubscribe)
		{
			//if (mdMsg.From is not null)
			//{
			//	var from = (long)mdMsg.From.Value.ToUnix(false);
			//	var to = (long)(mdMsg.To ?? DateTimeOffset.UtcNow).ToUnix(false);
			//	var left = mdMsg.Count ?? long.MaxValue;

			//	while (from < to)
			//	{
			//		var trades = await _restClient.GetTrades(symbol, from, to, cancellationToken);
			//		var needBreak = true;
			//		var last = from;

			//		foreach (var trade in trades.OrderBy(t => t.Time))
			//		{
			//			cancellationToken.ThrowIfCancellationRequested();

			//			var time = (long)trade.Time.ToUnix();

			//			if (time < from)
			//				continue;

			//			if (time > to)
			//			{
			//				needBreak = true;
			//				break;
			//			}

			//			SendOutMessage(new ExecutionMessage
			//			{
			//				DataTypeEx = DataType.Ticks,
			//				TradeId = trade.TradeId,
			//				TradePrice = trade.Price?.ToDecimal(),
			//				TradeVolume = trade.Size?.ToDecimal(),
			//				ServerTime = trade.Time,
			//				OriginSide = trade.Side.ToSide(),
			//				OriginalTransactionId = mdMsg.TransactionId,
			//			});

			//			if (--left <= 0)
			//			{
			//				needBreak = true;
			//				break;
			//			}

			//			last = time;
			//			needBreak = false;
			//		}

			//		if (needBreak)
			//			break;

			//		from = last;
			//	}
			//}
			
			if (!mdMsg.IsHistoryOnly())
				await _socketClient.SubscribeTrades(mdMsg.TransactionId, symbol, cancellationToken);

			SendSubscriptionResult(mdMsg);
		}
		else
		{
			await _socketClient.UnSubscribeTrades(mdMsg.OriginalTransactionId, symbol, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(mdMsg.TransactionId);

		var symbol = mdMsg.SecurityId.ToSymbol();

		if (mdMsg.IsSubscribe)
		{
			var tf = mdMsg.GetTimeFrame();

			if (mdMsg.From is not null)
			{
				var from = (long)mdMsg.From.Value.ToUnix();
				var to = (long)(mdMsg.To ?? DateTimeOffset.UtcNow).ToUnix();
				var left = mdMsg.Count ?? long.MaxValue;
				var step = (long)tf.Multiply(200).TotalSeconds;
				var granularity = mdMsg.GetTimeFrame().ToNative();

				while (from < to)
				{
					var candles = await _restClient.GetCandles(symbol, from, from + step, granularity, cancellationToken);
					var needBreak = true;
					var last = from;

					foreach (var candle in candles.OrderBy(t => t.Time))
					{
						cancellationToken.ThrowIfCancellationRequested();

						var time = (long)candle.Time.ToUnix();

						if (time < from)
							continue;

						if (time > to)
						{
							needBreak = true;
							break;
						}

						SendOutMessage(new TimeFrameCandleMessage
						{
							OpenPrice = (decimal)candle.Open,
							ClosePrice = (decimal)candle.Close,
							HighPrice = (decimal)candle.High,
							LowPrice = (decimal)candle.Low,
							TotalVolume = (decimal)candle.Volume,
							OpenTime = candle.Time,
							State = CandleStates.Finished,
							OriginalTransactionId = mdMsg.TransactionId,
						});

						if (--left <= 0)
						{
							needBreak = true;
							break;
						}

						last = time;
						needBreak = false;
					}

					if (needBreak || candles.Length < 10)
						break;

					from = last;
				}
			}

			if (!mdMsg.IsHistoryOnly() && mdMsg.DataType2 == _tf5min)
			{
				_candlesTransIds[symbol] = mdMsg.TransactionId;
				await _socketClient.SubscribeCandles(mdMsg.TransactionId, symbol, cancellationToken);
				SendSubscriptionResult(mdMsg);
			}
			else
				SendSubscriptionFinished(mdMsg.TransactionId);
		}
		else
		{
			_candlesTransIds.Remove(symbol);
			await _socketClient.UnSubscribeCandles(mdMsg.OriginalTransactionId, symbol, cancellationToken);
		}
	}

	private void SessionOnTickerChanged(Ticker ticker)
	{
		SendOutMessage(new Level1ChangeMessage
		{
			SecurityId = ticker.Product.ToStockSharp(),
			ServerTime = CurrentTime.ConvertToUtc(),
		}
		.TryAdd(Level1Fields.LastTradeId, ticker.LastTradeId)
		.TryAdd(Level1Fields.LastTradePrice, ticker.LastTradePrice?.ToDecimal())
		.TryAdd(Level1Fields.LastTradeVolume, ticker.LastTradePrice?.ToDecimal())
		.TryAdd(Level1Fields.LastTradeOrigin, ticker.LastTradeSide?.ToSide())
		.TryAdd(Level1Fields.HighPrice, ticker.High?.ToDecimal())
		.TryAdd(Level1Fields.LowPrice, ticker.Low?.ToDecimal())
		.TryAdd(Level1Fields.Volume, ticker.Volume?.ToDecimal())
		.TryAdd(Level1Fields.Change, ticker.Change?.ToDecimal())
		.TryAdd(Level1Fields.BestBidPrice, ticker.Bid?.ToDecimal())
		.TryAdd(Level1Fields.BestAskPrice, ticker.Ask?.ToDecimal())
		.TryAdd(Level1Fields.BestBidVolume, ticker.BidSize?.ToDecimal())
		.TryAdd(Level1Fields.BestAskVolume, ticker.AskSize?.ToDecimal())
		);
	}

	private void SessionOnTradeReceived(Trade trade)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = trade.ProductId.ToStockSharp(),
			TradeId = trade.TradeId,
			TradePrice = (decimal)trade.Price,
			TradeVolume = (decimal)trade.Size,
			ServerTime = trade.Time,
			OriginSide = trade.Side.ToSide(),
		});
	}

	private void SessionOnOrderBookReceived(string type, string symbol, IEnumerable<OrderBookChange> changes)
	{
		var bids = new List<QuoteChange>();
		var asks = new List<QuoteChange>();

		foreach (var change in changes)
		{
			var side = change.Side.ToSide();

			var quotes = side == Sides.Buy ? bids : asks;

			quotes.Add(new((decimal)change.Price, (decimal)change.Size));
		}

		SendOutMessage(new QuoteChangeMessage
		{
			SecurityId = symbol.ToStockSharp(),
			Bids = bids.ToArray(),
			Asks = asks.ToArray(),
			ServerTime = CurrentTime.ConvertToUtc(),
			State = type == "snapshot" ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment,
		});
	}

	private void SessionOnCandleReceived(Ohlc candle)
	{
		if (!_candlesTransIds.TryGetValue(candle.Symbol, out var transId))
			return;

		SendOutMessage(new TimeFrameCandleMessage
		{
			OpenPrice = (decimal)candle.Open,
			ClosePrice = (decimal)candle.Close,
			HighPrice = (decimal)candle.High,
			LowPrice = (decimal)candle.Low,
			TotalVolume = (decimal)candle.Volume,
			OpenTime = candle.Time,
			State = CandleStates.Active,
			OriginalTransactionId = transId,
		});
	}
}