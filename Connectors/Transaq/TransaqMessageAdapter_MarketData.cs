namespace StockSharp.Transaq
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Transaq.Native;
	using StockSharp.Transaq.Native.Commands;
	using StockSharp.Transaq.Native.Responses;
	using StockSharp.Localization;

	partial class TransaqMessageAdapter
	{
		private readonly HashSet<SecurityId> _registeredSecurityIds = new HashSet<SecurityId>();
		private readonly CachedSynchronizedDictionary<int, TimeSpan> _candlePeriods = new CachedSynchronizedDictionary<int, TimeSpan>();
		private readonly SynchronizedDictionary<Tuple<int, int>, long> _candleTransactions = new SynchronizedDictionary<Tuple<int, int>, long>();
		private readonly SynchronizedDictionary<int, string> _boards = new SynchronizedDictionary<int, string>();
		private readonly SynchronizedDictionary<int, Tuple<Dictionary<decimal, decimal>, Dictionary<decimal, decimal>>> _quotes = new SynchronizedDictionary<int, Tuple<Dictionary<decimal, decimal>, Dictionary<decimal, decimal>>>();

		private void ProcessMarketDataMessage(MarketDataMessage mdMsg)
		{
			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					SendCommand(mdMsg.IsSubscribe
									? new SubscribeMessage { Quotations = { mdMsg.SecurityId } }
									: new UnsubscribeMessage { Quotations = { mdMsg.SecurityId } });

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					SendCommand(mdMsg.IsSubscribe
									? new SubscribeMessage { Quotes = { mdMsg.SecurityId } }
									: new UnsubscribeMessage { Quotes = { mdMsg.SecurityId } });

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (mdMsg.IsSubscribe)
					{
						//Подписаться/отписаться на тики можно двумя способами:
						//SubscribeMessage/UnsubscribeMessage - тики приходят с момента подписки
						//SubscribeTicksMessage - Тики приходят с момента подиски(TradeNo = 0), или с любого номера. При повторном запросе отписка получения тиков по предыдущему запросу.


						//var command = new SubscribeMessage();
						//command.AllTrades.Add(security.GetTransaqId());

						//ApiClient.Send(new Tuple<BaseCommandMessage, Action<BaseResponse>>(command, ProcessResult));

						//---

						_registeredSecurityIds.Add(mdMsg.SecurityId);

						var command = new SubscribeTicksMessage { Filter = true }; //Filter только сделки нормально периода торгов

						foreach (var id in _registeredSecurityIds)
						{
							command.Items.Add(new SubscribeTicksSecurity
							{
								SecId = (int)id.Native,
								TradeNo = id.Equals(mdMsg.SecurityId) ? 1 : 0,
							});
						}

						SendCommand(command);
					}
					else
					{
						//var command = new UnsubscribeMessage();
						//command.AllTrades.Add(security.GetTransaqId());

						//ApiClient.Send(new Tuple<BaseCommandMessage, Action<BaseResponse>>(command, ProcessResult));

						//---

						_registeredSecurityIds.Remove(mdMsg.SecurityId);

						var command = new SubscribeTicksMessage { Filter = true }; //Filter только сделки нормально периода торгов

						foreach (var id in _registeredSecurityIds)
						{
							command.Items.Add(new SubscribeTicksSecurity
							{
								SecId = (int)id.Native,
								TradeNo = 0,
							});
						}

						SendCommand(command);
					}

					break;
				}
				case MarketDataTypes.News:
				{
					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.NewsId.IsEmpty())
						{
							var count = (int)mdMsg.Count;

							if (count <= 0)
								throw new InvalidOperationException(LocalizedStrings.Str3511Params.Put(count));

							if (count > MaxNewsHeaderCount)
								throw new InvalidOperationException(LocalizedStrings.Str3512Params.Put(count, MaxNewsHeaderCount));

							SendCommand(new RequestOldNewsMessage { Count = count });
						}
						else
						{
							SendCommand(new RequestNewsBodyMessage { NewsId = mdMsg.NewsId.To<int>() });
						}
					}

					break;
				}
				case MarketDataTypes.CandleTimeFrame:
				{
					if (mdMsg.IsSubscribe)
					{
						var periodId = _candlePeriods.GetKeys((TimeSpan)mdMsg.Arg).First();

						_candleTransactions.Add(Tuple.Create((int)mdMsg.SecurityId.Native, periodId), mdMsg.TransactionId);

						var command = new RequestHistoryDataMessage
						{
							SecId = (int)mdMsg.SecurityId.Native,
							Period = periodId,
							Count = mdMsg.Count,
							Reset = mdMsg.To == DateTimeOffset.MaxValue,
						};

						SendCommand(command);
					}

					break;
				}
				default:
					throw new ArgumentOutOfRangeException("mdMsg", mdMsg.DataType, LocalizedStrings.Str1618);
			}

			var reply = (MarketDataMessage)mdMsg.Clone();
			reply.OriginalTransactionId = mdMsg.TransactionId;
			SendOutMessage(reply);
		}

		/// <summary>
		/// Список доступных периодов свечей.
		/// </summary>
		public IEnumerable<TimeSpan> CandleTimeFrames
		{
			get { return _candlePeriods.CachedValues; }
		}

		/// <summary>
		/// Событие инициализации поля <see cref="CandleTimeFrames"/>.
		/// </summary>
		public event Action CandleTimeFramesInitialized;

		/// <summary>
		/// Максимально-допустимое количество заголовков новостей.
		/// </summary>
		public const int MaxNewsHeaderCount = 100;

		private void OnAllTradesResponse(AllTradesResponse response)
		{
			foreach (var tick in response.AllTrades)
			{
				SendOutMessage(new ExecutionMessage
				{
					TradeId = tick.TradeNo,
					ServerTime = tick.TradeTime.ApplyTimeZone(TimeHelper.Moscow),
					SecurityId = new SecurityId { Native = tick.SecId },
					TradePrice = tick.Price,
					Volume = tick.Quantity,
					OriginSide = tick.BuySell.FromTransaq(),
					OpenInterest = tick.OpenInterest,
					ExecutionType = ExecutionTypes.Tick,
				});
			}
		}

		private void OnCandleKindsResponse(CandleKindsResponse response)
		{
			_candlePeriods.Clear();

			foreach (var kind in response.Kinds)
				_candlePeriods.Add(kind.Id, TimeSpan.FromSeconds(kind.Period));

			CandleTimeFramesInitialized.SafeInvoke();
		}

		private void OnCandlesResponse(CandlesResponse response)
		{
			var key = Tuple.Create(response.SecId, response.Period);
			var transactionId = _candleTransactions.TryGetValue2(key);

			if (transactionId == null)
			{
				SendOutError(LocalizedStrings.Str3513Params.Put(response.SecCode, response.Board, response.Period));
				return;
			}

			var isFinished = false;

			switch (response.Status)
			{
				case CandleResponseStatus.Finished:
				case CandleResponseStatus.Done:
				case CandleResponseStatus.NotAvailable:
					isFinished = true;
					_candleTransactions.Remove(key);
					break;
			}

			foreach (var candle in response.Candles)
			{
				var time = candle.Date.ApplyTimeZone(TimeHelper.Moscow);

				SendOutMessage(new TimeFrameCandleMessage
				{
					OriginalTransactionId = (long)transactionId,
					SecurityId = new SecurityId { Native = response.SecId },
					OpenPrice = candle.Open,
					HighPrice = candle.High,
					LowPrice = candle.Low,
					ClosePrice = candle.Close,
					TotalVolume = candle.Volume,
					OpenTime = time - _candlePeriods[response.Period],
					CloseTime = time,
					OpenInterest = candle.Oi,
					IsFinished = isFinished,
					State = CandleStates.Finished,
				});
			}
		}

		private void OnMarketsResponse(MarketsResponse response)
		{
			//foreach (var market in response.Markets)
			//{
			//	switch (market.Name.Trim())
			//	{
			//		case "ММВБ":
			//			_exchangeBoards.Add(market.Id, ExchangeBoard.MicexEqbr);
			//			break;
			//		case "FORTS":
			//			_exchangeBoards.Add(market.Id, ExchangeBoard.Forts);
			//			break;
			//		default:
			//			var board = new ExchangeBoard { Code = market.Name };
			//			_exchangeBoards.Add(market.Id, board);
			//			break;
			//	}
			//}
		}

		private void OnNewsBodyResponse(NewsBodyResponse response)
		{
			SendOutMessage(new NewsMessage
			{
				Id = response.Id.To<string>(),
				Story = response.Text,
				ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow)
			});
		}

		private void OnNewsHeaderResponse(NewsHeaderResponse response)
		{
			SendOutMessage(new NewsMessage
			{
				Source = response.Source,
				Headline = response.Title,
				Id = response.Id.To<string>(),
				Story = response.Text,
				ServerTime = response.TimeStamp == null
					? SessionHolder.CurrentTime.Convert(TimeHelper.Moscow)
					: response.TimeStamp.Value.ApplyTimeZone(TimeHelper.Moscow),
			});
		}

		private void OnQuotationsResponse(QuotationsResponse response)
		{
			foreach (var quote in response.Quotations)
			{
				var message = new Level1ChangeMessage
				{
					SecurityId = new SecurityId { Native = quote.SecId },
					ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
				};

				message.TryAdd(Level1Fields.AccruedCouponIncome, quote.AccruedIntValue);
				message.TryAdd(Level1Fields.OpenPrice, quote.Open);
				message.TryAdd(Level1Fields.HighPrice, quote.High);
				message.TryAdd(Level1Fields.LowPrice, quote.Low);
				message.TryAdd(Level1Fields.ClosePrice, quote.ClosePrice);
				message.TryAdd(Level1Fields.BidsCount, quote.BidsCount);
				message.TryAdd(Level1Fields.BidsVolume, (decimal?)quote.BidsVolume);
				message.TryAdd(Level1Fields.AsksCount, quote.AsksCount);
				message.TryAdd(Level1Fields.AsksVolume, (decimal?)quote.AsksVolume);
				message.TryAdd(Level1Fields.HighBidPrice, quote.HighBid);
				message.TryAdd(Level1Fields.LowAskPrice, quote.LowAsk);
				message.TryAdd(Level1Fields.Yield, quote.Yield);
				message.TryAdd(Level1Fields.MarginBuy, quote.BuyDeposit);
				message.TryAdd(Level1Fields.MarginSell, quote.SellDeposit);
				message.TryAdd(Level1Fields.HistoricalVolatility, quote.Volatility);
				message.TryAdd(Level1Fields.TheorPrice, quote.TheoreticalPrice);
				message.TryAdd(Level1Fields.Change, quote.Change);
				message.TryAdd(Level1Fields.Volume, (decimal?)quote.VolToday);
				message.TryAdd(Level1Fields.StepPrice, quote.PointCost);
				message.TryAdd(Level1Fields.OpenInterest, (decimal?)quote.OpenInterest);
				message.TryAdd(Level1Fields.TradesCount, quote.TradesCount);

				if (quote.Status != null)
					message.Add(Level1Fields.State, quote.Status.Value.FromTransaq());

				
				// Transaq передает только изменения (например, передать только цену сделки, если объем при этом не изменился)

				message.TryAdd(Level1Fields.LastTradePrice, quote.LastTradePrice);
				message.TryAdd(Level1Fields.LastTradeVolume, (decimal?)quote.LastTradeVolume);

				if (quote.LastTradeTime != null)
					message.Add(Level1Fields.LastTradeTime, quote.LastTradeTime.Value.ApplyTimeZone(TimeHelper.Moscow));

				message.TryAdd(Level1Fields.BestBidPrice, quote.BestBidPrice);
				message.TryAdd(Level1Fields.BestBidVolume, (decimal?)quote.BestBidVolume);

				message.TryAdd(Level1Fields.BestAskPrice, quote.BestAskPrice);
				message.TryAdd(Level1Fields.BestAskVolume, (decimal?)quote.BestAskVolume);

				SendOutMessage(message);
			}
		}

		private void OnQuotesResponse(QuotesResponse response)
		{
			foreach (var group in response.Quotes.GroupBy(q => q.SecId))
			{
				var tuple = _quotes.SafeAdd(group.Key, key => Tuple.Create(new Dictionary<decimal, decimal>(), new Dictionary<decimal, decimal>()));

				foreach (var quote in group)
				{
					if (quote.Price == 0)
						continue;

					if (quote.Buy != null)
					{
						if (quote.Buy == -1)
							tuple.Item1.Remove(quote.Price);
						else
							tuple.Item1[quote.Price] = (decimal)quote.Buy;
					}

					if (quote.Sell != null)
					{
						if (quote.Sell == -1)
							tuple.Item2.Remove(quote.Price);
						else
							tuple.Item2[quote.Price] = (decimal)quote.Sell;
					}
				}

				SendOutMessage(new QuoteChangeMessage
				{
					SecurityId = new SecurityId { Native = group.Key },
					Bids = tuple.Item1.Select(p => new QuoteChange(Sides.Buy, p.Key, p.Value)).ToArray(),
					Asks = tuple.Item2.Select(p => new QuoteChange(Sides.Sell, p.Key, p.Value)).ToArray(),
					ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
				});
			}
		}

		private void OnSecInfoResponse(SecInfoResponse response)
		{
			var securityId = new SecurityId
			{
				Native = response.SecId,
				SecurityCode = response.SecCode,
				BoardCode = _boards[response.Market],
			};

			SendOutMessage(new SecurityMessage
			{
				SecurityId = securityId,
				ExpiryDate = response.MatDate == null ? (DateTimeOffset?)null : response.MatDate.Value.ApplyTimeZone(TimeHelper.Moscow),
				OptionType = response.PutCall == null ? (OptionTypes?)null : response.PutCall.Value.FromTransaq(),
			});

			var l1Msg = new Level1ChangeMessage
			{
				SecurityId = new SecurityId { Native = response.SecId },
				ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
			};

			l1Msg.TryAdd(Level1Fields.MinPrice, response.MinPrice);
			l1Msg.TryAdd(Level1Fields.MaxPrice, response.MaxPrice);

			var marginBuy = response.BuyDeposit;

			if (marginBuy == null || marginBuy == 0m)
				marginBuy = response.BgoBuy;

			var marginSell = response.SellDeposit;

			if (marginSell == null || marginSell == 0m)
				marginSell = response.BgoC;

			l1Msg.TryAdd(Level1Fields.MarginBuy, marginBuy);
			l1Msg.TryAdd(Level1Fields.MarginSell, marginSell);

			SendOutMessage(l1Msg);
		}

		private void OnSecuritiesResponse(SecuritiesResponse response)
		{
			foreach (var security in response.Securities)
			{
				var securityId = new SecurityId
				{
					Native = security.SecId,
					SecurityCode = security.SecCode,
					BoardCode = security.Board.FixBoardName(),
				};

				SendOutMessage(new SecurityMessage
				{
					Name = security.ShortName,
					SecurityId = securityId,
					Multiplier = security.LotSize,
					PriceStep = security.MinStep,
					ShortName = security.ShortName,
					SecurityType = security.Type.FromTransaq(),
				});

				SendOutMessage(
					new Level1ChangeMessage
					{
						SecurityId = securityId,
						ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
					}
					.Add(Level1Fields.State, security.Active ? SecurityStates.Trading : SecurityStates.Stoped)
					.TryAdd(Level1Fields.StepPrice, security.PointCost));
			}
		}

		private void OnTicksResponse(TicksResponse response)
		{
			foreach (var tick in response.Ticks)
			{
				SendOutMessage(new ExecutionMessage
				{
					TradeId = tick.TradeNo,
					ServerTime = tick.TradeTime.ApplyTimeZone(TimeHelper.Moscow),
					SecurityId = new SecurityId { Native = tick.SecId },
					TradePrice = tick.Price,
					Volume = tick.Quantity,
					OriginSide = tick.BuySell.FromTransaq(),
					OpenInterest = tick.OpenInterest,
					ExecutionType = ExecutionTypes.Tick,
				});
			}
		}

		private void OnBoardsResponse(BoardsResponse response)
		{
			foreach (var board in response.Boards)
			{
				var code = board.Id.FixBoardName();

				SendOutMessage(new BoardMessage
				{
					Code = code,
					ExchangeCode = Exchange.Moex.Name,
				});

				_boards.TryAdd(board.Market, code);
			}
		}

		private void OnPitsResponse(PitsResponse response)
		{
			//foreach (var pit in response.Pits)
			//{
			//	RaiseNewMessage(new SecurityMessage
			//	{
			//		SecurityId = new SecurityId
			//		{
			//			SecurityCode = pit.SecCode,
			//			BoardCode = pit.Board.FixBoardName(),
			//		},
			//		VolumeStep = pit.LotSize,
			//		PriceStep = pit.MinStep,
			//	});	
			//}
		}

		private void OnMessagesResponse(MessagesResponse response)
		{
			foreach (var message in response.Messages)
			{
				SendOutMessage(new NewsMessage
				{
					Source = message.From,
					Headline = message.Text,
					Story = message.Text,
					ServerTime = message.Date == null
						? SessionHolder.CurrentTime.Convert(TimeHelper.Moscow)
						: message.Date.Value.ApplyTimeZone(TimeHelper.Moscow),
				});
			}
		}
	}
}