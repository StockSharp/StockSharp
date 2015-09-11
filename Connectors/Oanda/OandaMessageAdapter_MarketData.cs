namespace StockSharp.Oanda
{
	using System;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Oanda.Native.DataTypes;

	partial class OandaMessageAdapter
	{
		private void ProcessSecurityLookupMessage(SecurityLookupMessage message)
		{
			var instruments = _restClient.GetInstruments(GetAccountId(),
				message.SecurityId.IsDefault()
					? ArrayHelper.Empty<string>()
					: new[] { message.SecurityId.ToOanda() });

			foreach (var instrument in instruments)
			{
				var secId = instrument.Code.ToSecurityId();

				SendOutMessage(new SecurityMessage
				{
					OriginalTransactionId = message.TransactionId,
					SecurityId = secId,
					SecurityType = SecurityTypes.Currency,
					Name = instrument.DisplayName,
					PriceStep = 0.000001m
				});

				if (instrument.Halted)
				{
					SendOutMessage(new Level1ChangeMessage
					{
						ServerTime = CurrentTime.Convert(TimeZoneInfo.Utc),
						SecurityId = secId,
					}.Add(Level1Fields.State, SecurityStates.Stoped));
				}
			}

			SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = message.TransactionId });
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
				{
					var instrument = message.SecurityId.ToOanda();

					if (message.IsSubscribe)
						_streamigClient.SubscribePricesStreaming(GetAccountId(), instrument);
					else
						_streamigClient.UnSubscribePricesStreaming(instrument);

					break;
				}
				//case MarketDataTypes.MarketDepth:
				//	break;
				case MarketDataTypes.News:
				{
					if (message.IsSubscribe)
					{
						var calendar = _restClient.GetCalendar(message.SecurityId.ToOanda(),
							(int)(3600 * (message.Count ?? 1)));

						foreach (var item in calendar)
						{
							SendOutMessage(new NewsMessage
							{
								//OriginalTransactionId = message.TransactionId,
								SecurityId = message.SecurityId,
								ServerTime = TimeHelper.GregorianStart.AddSeconds(item.TimeStamp).ApplyTimeZone(TimeZoneInfo.Utc),
								Headline = item.Title,
								Story = "unit={0} curr={1} market={2} forecast={3} previous={4} actual={5}"
									.Put(item.Unit, item.Currency, item.Market, item.Forecast, item.Previous, item.Actual),
							});
						}
					}

					break;
				}
				case MarketDataTypes.CandleTimeFrame:
				{
					if (message.IsSubscribe)
					{
						var from = message.From;

						while (true)
						{
							var candles = _restClient.GetCandles(message.SecurityId.ToOanda(),
								((TimeSpan)message.Arg).ToOanda(), message.Count ?? 0, (from ?? DateTimeOffset.MinValue).ToOanda());

							var count = 0;

							foreach (var candle in candles)
							{
								count++;

								var time = candle.Time.FromOanda();

								SendOutMessage(new TimeFrameCandleMessage
								{
									OriginalTransactionId = message.TransactionId,
									OpenTime = time,
									SecurityId = message.SecurityId,
									OpenPrice = (decimal)candle.Open,
									HighPrice = (decimal)candle.High,
									LowPrice = (decimal)candle.Low,
									ClosePrice = (decimal)candle.Close,
									TotalVolume = (decimal)candle.Volume,
									State = candle.Complete ? CandleStates.Finished : CandleStates.Active
								});

								from = time;
							}

							if (message.Count == null && count == 500)
								continue;

							break;
						}
					}

					break;
				}
				default:
				{
					SendOutMarketDataNotSupported(message.TransactionId);
					return;
				}
			}

			var reply = (MarketDataMessage)message.Clone();
			reply.OriginalTransactionId = message.TransactionId;
			SendOutMessage(reply);
		}

		private void SessionOnNewPrice(Price price)
		{
			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = price.Instrument.ToSecurityId(),
				ServerTime = price.Time.FromOanda(),
			}
			.TryAdd(Level1Fields.BestBidPrice, (decimal)price.Bid)
			.TryAdd(Level1Fields.BestAskPrice, (decimal)price.Ask));
		}
	}
}