namespace StockSharp.AlfaDirect
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.AlfaDirect.Native;
	using StockSharp.Algo;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class AlfaDirectMessageAdapter
	{
		readonly Dictionary<int, string> _securityCodes = new Dictionary<int, string>();

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (message.SecurityId.Native is int)
					{
						if (message.IsSubscribe)
							Wrapper.RegisterLevel1((int)message.SecurityId.Native);
						else
							Wrapper.UnRegisterLevel1((int)message.SecurityId.Native);
					}
					else
						throw new InvalidOperationException(LocalizedStrings.Str2253Params.Put(message.SecurityId));

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					if (message.SecurityId.Native is int)
					{
						if (message.IsSubscribe)
							Wrapper.RegisterMarketDepth((int)message.SecurityId.Native);
						else
							Wrapper.UnRegisterMarketDepth((int)message.SecurityId.Native);
					}
					else
						throw new InvalidOperationException(LocalizedStrings.Str2253Params.Put(message.SecurityId));

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (message.SecurityId.Native is int)
					{
						if (message.IsSubscribe)
							Wrapper.RegisterTrades((int)message.SecurityId.Native);
						else
							Wrapper.UnRegisterTrades((int)message.SecurityId.Native);
					}
					else
						throw new InvalidOperationException(LocalizedStrings.Str2253Params.Put(message.SecurityId));

					break;
				}
				case MarketDataTypes.News:
				{
					if (!message.NewsId.IsEmpty())
						throw new NotSupportedException(LocalizedStrings.Str1617);

					if (message.IsSubscribe)
						Wrapper.StartExportNews();
					else
						Wrapper.StopExportNews();

					break;
				}
				case MarketDataTypes.CandleTimeFrame:
				{
					if (message.IsSubscribe)
					{
						Wrapper.LookupCandles(message);
					}

					break;
				}
				default:
					throw new ArgumentOutOfRangeException("message", message.DataType, LocalizedStrings.Str1618);
			}

			var reply = (MarketDataMessage)message.Clone();
			reply.OriginalTransactionId = message.TransactionId;
			SendOutMessage(reply);
		}

		private void OnProcessSecurities(long transactionId, string[] data)
		{
			var f = Wrapper.FieldsSecurities;
			var secMessages = new List<Tuple<int, SecurityMessage>>();
			var level1Messages = new List<Level1ChangeMessage>();

			foreach (var row in data)
			{
				var cols = row.ToColumns();

				var secType = f.ATCode.GetValue(cols);
				if(secType == null)
					continue;

				var paperNo = f.PaperNo.GetValue(cols);
				var code = f.PaperCode.GetValue(cols);
				var name = f.AnsiName.GetValue(cols);
				var time = f.ILastUpdate.GetValue(cols);

				_securityCodes[paperNo] = code;

				var secId = new SecurityId
				{
					Native = paperNo,
					SecurityCode = code,
					BoardCode = SessionHolder.GetBoardCode(f.PlaceCode.GetValue(cols))
				};

				var msg = new SecurityMessage
				{
					SecurityId = secId,
					Name = name,
					ShortName = name,
					SecurityType = secType,
					Multiplier = f.LotSize.GetValue(cols),
					PriceStep = f.PriceStep.GetValue(cols),
					//LocalTime = time,
					Currency = f.CurrCode.GetValue(cols),
					Strike = f.Strike.GetValue(cols)
				};

				if(msg.SecurityType == SecurityTypes.Option || msg.SecurityType == SecurityTypes.Future)
					msg.ExpiryDate = f.MatDate.GetValue(cols).ApplyTimeZone(TimeHelper.Moscow);

				if (msg.SecurityType == SecurityTypes.Option)
				{
					msg.OptionType = f.ATCode.GetStrValue(cols).ATCodeToOptionType();
					msg.Strike = f.Strike.GetValue(cols);
				}

				secMessages.Add(Tuple.Create(f.BasePaperNo.GetValue(cols), msg));

				var l1Msg = new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = time.ApplyTimeZone(TimeHelper.Moscow)
				};

				l1Msg.TryAdd(Level1Fields.MarginBuy, f.GoBuy.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.MarginSell, f.GoSell.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.StepPrice, f.PriceStepCost.GetValue(cols));

				level1Messages.Add(l1Msg);
			}

			secMessages.Where(t => t.Item2.SecurityType == SecurityTypes.Option).ForEach(t => 
				t.Item2.UnderlyingSecurityCode = _securityCodes.TryGetValue(t.Item1));

			secMessages.ForEach(t => SendOutMessage(t.Item2));
			level1Messages.ForEach(SendOutMessage);

			if (transactionId > 0)
			{
				SendOutMessage(new SecurityLookupResultMessage
				{
					OriginalTransactionId = transactionId,
				});
			}
		}

		private void OnProcessLevel1(string[] data)
		{
			var f = Wrapper.FieldsLevel1;

			foreach (var row in data)
			{
				var cols = row.ToColumns();
				var paperNo = f.PaperNo.GetValue(cols);
				var secId = new SecurityId { Native = paperNo };

				var l1Msg = new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = (f.LastUpdateDate.GetValue(cols).Date + f.LastUpdateTime.GetValue(cols).TimeOfDay).ApplyTimeZone(TimeHelper.Moscow)
				};

				l1Msg.Add(Level1Fields.State, f.TradingStatus.GetValue(cols));

				l1Msg.TryAdd(Level1Fields.MarginBuy, f.GoBuy.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.MarginSell, f.GoSell.GetValue(cols));

				l1Msg.TryAdd(Level1Fields.OpenInterest, (decimal)f.OpenPosQty.GetValue(cols));

				var minPrice = f.MinDeal.GetValue(cols);
				var maxPrice = f.MaxDeal.GetValue(cols);

				l1Msg.TryAdd(Level1Fields.OpenPrice, f.OpenPrice.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.ClosePrice, f.ClosePrice.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.HighPrice, maxPrice);
				l1Msg.TryAdd(Level1Fields.LowPrice, minPrice);

				l1Msg.TryAdd(Level1Fields.BestBidPrice, f.Buy.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.BestBidVolume, (decimal)f.BuyQty.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.BestAskPrice, f.Sell.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.BestAskVolume, (decimal)f.SellQty.GetValue(cols));

				l1Msg.TryAdd(Level1Fields.MinPrice, minPrice);
				l1Msg.TryAdd(Level1Fields.MaxPrice, maxPrice);

				l1Msg.TryAdd(Level1Fields.Multiplier, (decimal)f.LotSize.GetValue(cols));

				l1Msg.TryAdd(Level1Fields.ImpliedVolatility, f.Volatility.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.TheorPrice, f.TheorPrice.GetValue(cols));

				l1Msg.TryAdd(Level1Fields.LastTradePrice, f.LastPrice.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.LastTradeVolume, (decimal)f.LastQty.GetValue(cols));

				l1Msg.TryAdd(Level1Fields.PriceStep, f.PriceStep.GetValue(cols));

				l1Msg.TryAdd(Level1Fields.BidsVolume, (decimal)f.BuySQty.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.BidsCount, f.BuyCount.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.AsksVolume, (decimal)f.SellSQty.GetValue(cols));
				l1Msg.TryAdd(Level1Fields.AsksCount, f.SellCount.GetValue(cols));

				SendOutMessage(l1Msg);
			}
		}

		private void OnProcessNews(string[] data)
		{
			var lastId = string.Empty;
			var f = Wrapper.FieldsNews;

			foreach (var s in data)
			{
				var line = s.Trim();

				if (line.IsEmpty())
					continue;

				var details = line.ToColumns();

				var id = f.NewNo.GetStrValue(details);

				if (!id.IsEmpty())
				{
					lastId = id;

					SendOutMessage(new NewsMessage
					{
						Id = id,
						Source = f.Provider.GetValue(details),
						Headline = f.Subject.GetValue(details),
						Story = f.Body.GetValue(details),
						ServerTime = f.DbData.GetValue(details).ApplyTimeZone(TimeHelper.Moscow),
					});
				}
				else if (!lastId.IsEmpty()) // Альфа вопреки документации возвращает тело новости отдельной строкой.
				{
					SendOutMessage(new NewsMessage
					{
						Id = lastId,
						Story = line,
					});

					lastId = string.Empty;
				}
				else
				{
					SessionHolder.AddWarningLog(LocalizedStrings.Str2257Params, line);
				}
			}
		}

		private void OnProcessTrades(string[] data)
		{
			var f = Wrapper.FieldsTrades;

			foreach (var row in data)
			{
				var cols = row.ToColumns();

				SendOutMessage(new ExecutionMessage
				{
					SecurityId = new SecurityId { Native = f.PaperNo.GetValue(cols) },
					ExecutionType = ExecutionTypes.Tick,
					TradeId = f.TrdNo.GetValue(cols),
					OriginSide = f.BuySellNum.GetValue(cols),
					TradePrice = f.Price.GetValue(cols),
					Volume = f.Qty.GetValue(cols),
					ServerTime = f.TsTime.GetValue(cols).ApplyTimeZone(TimeHelper.Moscow),
				});
			}
		}

		private void OnProcessQuotes(string where, string[] quotes)
		{
			// paper_no нужно парсить из условия where, так как в поле paper_no передается всегда 0
			var paperNo = where.Split(new[] { '=' })[1].Trim().To<int>();
			var f = Wrapper.FieldsDepth;

			var bids = new List<QuoteChange>();
			var asks = new List<QuoteChange>();

			foreach (var quoteStr in quotes)
			{
				var cols = quoteStr.ToColumns();

				var sellQty = f.SellQty.GetValue(cols);
				var price = f.Price.GetValue(cols);
				var buyQty = f.BuyQty.GetValue(cols);

				if (sellQty == 0 && buyQty == 0)
				{
					// If the sellQty and buyQty are 0 - that is our limit order 
					// which is not a part of the market depth. Just skip it.
					continue;
				}

				QuoteChange quote;

				if (sellQty == 0)
				{
					quote = new QuoteChange(Sides.Buy, price, buyQty);
					bids.Insert(0, quote);
				}
				else
				{
					quote = new QuoteChange(Sides.Sell, price, sellQty);
					asks.Add(quote);
				}
			}

			if (bids.Count > 0 || asks.Count > 0)
			{
				SendOutMessage(new QuoteChangeMessage
				{
					Bids = bids,
					Asks = asks,
					SecurityId = new SecurityId { Native = paperNo },
					IsSorted = true,
					ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
				});	
			}
		}

		private void OnProcessCandles(MarketDataMessage mdMsg, string[] data)
		{
			var index = 0;

			foreach (var candle in data)
			{
				var details = candle.ToColumns();
				var closeTime = details[0].To<DateTime>().ApplyTimeZone(TimeHelper.Moscow);

				SendOutMessage(new TimeFrameCandleMessage
				{
					SecurityId = mdMsg.SecurityId,
					OpenPrice = details[1].To<decimal>(),
					HighPrice = details[2].To<decimal>(),
					LowPrice = details[3].To<decimal>(),
					ClosePrice = details[4].To<decimal>(),
					TotalVolume = details[5].To<decimal>(),
					// из терминала приходит время закрытия свечи
					OpenTime = closeTime - mdMsg.Arg.To<TimeSpan>(),
					CloseTime = closeTime,
					OriginalTransactionId = mdMsg.TransactionId,
					State = CandleStates.Finished,
					IsFinished = ++index != data.Length
				});
			}
		}
	}
}