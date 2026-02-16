namespace StockSharp.PrizmBit;

partial class PrizmBitMessageAdapter
{
	private readonly SynchronizedPairSet<string, int> _currencies = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SynchronizedPairSet<SecurityId, int> _markets = [];

	private async Task EnsureFillCurrenciesAsync(CancellationToken cancellationToken)
	{
		using (_currencies.EnterScope())
		{
			if (_currencies.Count > 0)
				return;
		}

		var currencies = await _httpClient.GetCurrenciesAsync(cancellationToken);

		using (_currencies.EnterScope())
		{
			foreach (var currency in currencies)
				_currencies.Add(currency.Code, currency.Id);
		}
	}

	private async Task<int> GetCurrencyIdAsync(string code, CancellationToken cancellationToken)
	{
		await EnsureFillCurrenciesAsync(cancellationToken);
		return _currencies[code];
	}

	private async Task<string> GetCurrencyCodeAsync(int id, CancellationToken cancellationToken)
	{
		await EnsureFillCurrenciesAsync(cancellationToken);
		return _currencies[id];
	}

	private async Task EnsureFillMarketsAsync(CancellationToken cancellationToken)
	{
		await EnsureFillMarketsAsync(async ct => await _httpClient.GetSymbolsAsync(ct), cancellationToken);
	}

	private async Task EnsureFillMarketsAsync(Func<CancellationToken, Task<IEnumerable<Symbol>>> getSymbolsAsync, CancellationToken cancellationToken)
	{
		if (getSymbolsAsync == null)
			throw new ArgumentNullException(nameof(getSymbolsAsync));

		using (_markets.EnterScope())
		{
			if (_markets.Count > 0)
				return;
		}

		var symbols = await getSymbolsAsync(cancellationToken);

		using (_markets.EnterScope())
		{
			foreach (var symbol in symbols)
				_markets.Add(symbol.Code.ToStockSharp(), symbol.Id);
		}
	}

	private async Task<SecurityId> GetSecurityIdAsync(int marketId, CancellationToken cancellationToken)
	{
		await EnsureFillMarketsAsync(cancellationToken);
		return _markets[marketId];
	}

	private async Task<int> GetMarketIdAsync(SecurityId secId, CancellationToken cancellationToken)
	{
		await EnsureFillMarketsAsync(cancellationToken);
		return _markets[secId];
	}

	/// <inheritdoc />
	protected override async ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var secTypes = lookupMsg.GetSecurityTypes();
		var left = lookupMsg.Count ?? long.MaxValue;

		var symbols = await _httpClient.GetSymbolsAsync(cancellationToken);

		await EnsureFillMarketsAsync(async ct => symbols, cancellationToken);

		foreach (var symbol in symbols)
		{
			var secId = symbol.Code.ToStockSharp();

			var secMsg = new SecurityMessage
			{
				SecurityId = secId,
				SecurityType = SecurityTypes.CryptoCurrency,
				MinVolume = (decimal)symbol.Amount.Minimum,
				Decimals = symbol.Price.Decimals,
				VolumeStep = symbol.Amount.Decimals.GetPriceStep(),
				OriginalTransactionId = lookupMsg.TransactionId,
			}.TryFillUnderlyingId(symbol.QuoteCoinCode);

			if (!secMsg.IsMatch(lookupMsg, secTypes))
				continue;

			await SendOutMessageAsync(secMsg, cancellationToken);

			if (--left <= 0)
				break;
		}

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnOrderLogSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var secId = mdMsg.SecurityId;
		var transId = mdMsg.TransactionId;
		var marketId = await GetMarketIdAsync(secId, cancellationToken);

		await SendSubscriptionReplyAsync(transId, cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			await _pusherClient.SubscribeTicker(marketId, cancellationToken);

			await SendSubscriptionResultAsync(mdMsg, cancellationToken);
		}
		else
		{
			// Unsubscribe logic not implemented in original method
		}
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
				var trades = await _httpClient.GetTradesAsync(secId.ToNative(), (long)mdMsg.From.Value.ToUnix(), (long)mdMsg.To.Value.ToUnix(), cancellationToken);

				foreach (var trade in trades)
				{
					await SendOutMessageAsync(new ExecutionMessage
					{
						DataTypeEx = DataType.Ticks,
						ServerTime = trade.Time,
						SecurityId = secId,
						TradeId = trade.Id,
						TradePrice = (decimal)trade.Price,
						TradeVolume = (decimal)trade.Amount,
						OriginSide = trade.Type.ToSide(),
						OriginalTransactionId = transId,
					}, cancellationToken);
				}
			}
			else
			{
				// Realtime ticks subscribe path not implemented in original method
			}

			await SendSubscriptionFinishedAsync(transId, cancellationToken);
		}
		else
		{
			// Unsubscribe logic not implemented in original method
		}
	}

	/// <inheritdoc />
	protected override ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> base.OnMarketDepthSubscriptionAsync(mdMsg, cancellationToken);

	/// <inheritdoc />
	protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var secId = mdMsg.SecurityId;
		var transId = mdMsg.TransactionId;

		await SendSubscriptionReplyAsync(transId, cancellationToken);

		var tf = mdMsg.GetTimeFrame();
		var tfName = tf.ToNative();

		if (mdMsg.IsSubscribe)
		{
			if (mdMsg.To != null)
			{
				var candles = await _httpClient.GetCandlesAsync(secId.ToNative(), tfName, (long)mdMsg.From.Value.ToUnix(), (long)mdMsg.To.Value.ToUnix(), cancellationToken);

				foreach (var candle in candles)
				{
					await ProcessCandleAsync(candle, secId, tf, transId, cancellationToken);
				}
			}
			else
			{
				// Realtime candles subscribe path not implemented in original method
			}

			await SendSubscriptionFinishedAsync(transId, cancellationToken);
		}
		else
		{
			// Unsubscribe logic not implemented in original method
		}
	}

	private ValueTask ProcessCandleAsync(Ohlc candle, SecurityId securityId, TimeSpan timeFrame, long originTransId, CancellationToken cancellationToken)
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
			OpenTime = candle.Time.FromUnix(false),
			State = CandleStates.Finished,
			TotalTicks = candle.Count.DefaultAsNull(),
			OriginalTransactionId = originTransId,
		}, cancellationToken);
	}

	private async ValueTask SessionOnOrderBookChanged(DateTime timestamp, OrderBook order, CancellationToken cancellationToken)
	{
		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			ServerTime = timestamp,
			SecurityId = await GetSecurityIdAsync(order.MarketId, cancellationToken),
			OrderId = order.Id,
			OrderState = OrderStates.Active,
			OrderPrice = (decimal)order.Price,
			OrderVolume = (decimal)order.Amount,
			Side = order.Side.ToSide(),
		}, cancellationToken);
	}

	private async ValueTask SessionOnOrderCanceled(DateTime timestamp, CanceledOrder order, CancellationToken cancellationToken)
	{
		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			ServerTime = timestamp,
			SecurityId = await GetSecurityIdAsync(order.MarketId, cancellationToken),
			OrderId = order.Id,
			OrderState = OrderStates.Done,
			OrderPrice = (decimal)order.Price,
			OrderVolume = (decimal)order.Amount,
			Side = order.Side.ToSide(),
		}, cancellationToken);
	}

	private async ValueTask SessionOnNewTrade(DateTime timestamp, SocketTrade trade, CancellationToken cancellationToken)
	{
		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			ServerTime = trade.Time,
			SecurityId = await GetSecurityIdAsync(trade.MarketId, cancellationToken),
			OrderId = trade.OrderId,
			TradeId = trade.TradeId,
			OrderPrice = (decimal)trade.Price,
			TradeVolume = (decimal)trade.Amount,
			Side = trade.Side.ToSide(),
		}, cancellationToken);
	}

	private async ValueTask SessionOnMarketPriceChanged(DateTime timestamp, MarketPrice price, CancellationToken cancellationToken)
	{
		await SendOutMessageAsync(new Level1ChangeMessage
		{
			ServerTime = timestamp,
			SecurityId = await GetSecurityIdAsync(price.MarketId, cancellationToken),
		}.TryAdd(Level1Fields.LastTradePrice, (decimal)price.Price), cancellationToken);
	}
}