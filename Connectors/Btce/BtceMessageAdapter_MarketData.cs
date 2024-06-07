namespace StockSharp.Btce;

partial class BtceMessageAdapter
{
	private readonly SynchronizedSet<string> _orderBooks = new(StringComparer.InvariantCultureIgnoreCase);

	private void ProcessSecurityLookup(SecurityLookupMessage lookupMsg)
	{
		var secTypes = lookupMsg.GetSecurityTypes();

		var reply = _httpClient.GetInstruments();

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
		}

		SendSubscriptionResult(lookupMsg);
	}

	private void ProcessMarketData(MarketDataMessage mdMsg)
	{
		var currency = mdMsg.SecurityId.ToCurrency();

		switch (mdMsg.DataType)
		{
			case MarketDataTypes.Level1:
			{
				break;
			}
			case MarketDataTypes.MarketDepth:
			{
				if (mdMsg.IsSubscribe)
					_pusherClient.SubscribeOrderBook(currency);
				else
					_pusherClient.UnSubscribeOrderBook(currency);

				break;
			}
			case MarketDataTypes.Trades:
			{
				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.To != null)
					{
						SendSubscriptionReply(mdMsg.TransactionId);

						var trades = _httpClient.GetTrades(5000, new[] { currency }).Items.TryGetValue(currency);

						if (trades != null)
						{
							foreach (var trade in trades.OrderBy(t => t.Timestamp))
							{
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
							}
						}

						SendSubscriptionResult(mdMsg);
						return;
					}
					else
						_pusherClient.SubscribeTrades(currency);
				}
				else
					_pusherClient.UnSubscribeTrades(currency);

				break;
			}
			default:
			{
				SendSubscriptionNotSupported(mdMsg.TransactionId);
				return;
			}
		}

		SendSubscriptionReply(mdMsg.TransactionId);
	}

	private void SessionOnOrderBookChanged(string ticker, OrderBook book)
	{
		var state = _orderBooks.TryAdd(ticker) ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment;

		QuoteChange ToChange(OrderBookEntry entry)
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