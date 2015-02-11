namespace StockSharp.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using OEC.API;
	using OEC.API.OSM.Info;
	using OEC.Data;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class OpenECryMessageAdapter
	{
		private readonly SynchronizedPairSet<Tuple<SecurityId, MarketDataTypes>, Subscription> _subscriptions = new SynchronizedPairSet<Tuple<SecurityId, MarketDataTypes>, Subscription>();

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var key = Tuple.Create(message.SecurityId, message.DataType);
			var session = SessionHolder.Session;

			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
				{
					var contract = session.Contracts[message.SecurityId.SecurityCode];

					if (message.IsSubscribe)
						session.Subscribe(contract);
					else
						session.Unsubscribe(contract);

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					var contract = session.Contracts[message.SecurityId.SecurityCode];

					if (message.IsSubscribe)
						session.SubscribeDOM(contract);
					else
						session.UnsubscribeDOM(contract);

					break;
				}
				case MarketDataTypes.Trades:
				{
					var contract = session.Contracts[message.SecurityId.SecurityCode];

					if (message.IsSubscribe)
					{
						var subscription = session.SubscribeTicks(contract, message.From.UtcDateTime);
						_subscriptions.Add(key, subscription);
					}
					else
						session.CancelSubscription(_subscriptions[key]);

					break;
				}
				case MarketDataTypes.News:
				{
					break;
				}
				case MarketDataTypes.CandleTimeFrame:
				case MarketDataTypes.CandleTick:
				case MarketDataTypes.CandleVolume:
				case MarketDataTypes.CandleRange:
				{
					var contract = session.Contracts[message.SecurityId.SecurityCode];

					if (message.IsSubscribe)
					{
						SubscriptionType subscriptionType;
						int interval;

						switch (message.DataType)
						{
							case MarketDataTypes.CandleTimeFrame:
							{
								var tf = (TimeSpan)message.Arg;

								if (tf < TimeSpan.FromMinutes(1))
								{
									subscriptionType = SubscriptionType.SecondBar;
									interval = (int)tf.TotalSeconds;
								}
								else if (tf < TimeSpan.FromHours(1))
								{
									subscriptionType = SubscriptionType.Bar;
									interval = (int)tf.TotalMinutes;
								}
								else if (tf.Ticks < TimeHelper.TicksPerWeek)
								{
									subscriptionType = SubscriptionType.DayBar;
									interval = (int)tf.TotalDays;
								}
								else if (tf.Ticks < TimeHelper.TicksPerMonth)
								{
									subscriptionType = SubscriptionType.WeeklyBar;
									interval = (int)tf.TotalDays;
								}
								else
								{
									subscriptionType = SubscriptionType.MonthlyBar;
									interval = (int)(tf.Ticks / TimeHelper.TicksPerMonth);
								}

								break;
							}
							case MarketDataTypes.CandleTick:
							{
								subscriptionType = SubscriptionType.TickBar;
								interval = message.Arg.To<int>();
								break;
							}
							case MarketDataTypes.CandleVolume:
							{
								subscriptionType = SubscriptionType.VolumeBar;
								interval = message.Arg.To<int>();
								break;
							}
							case MarketDataTypes.CandleRange:
							{
								subscriptionType = SubscriptionType.RangeBar;
								interval = message.Arg.To<int>();
								break;
							}
							default:
								throw new InvalidOperationException();
						}

						var subscription = message.Count == 0
							? session.SubscribeBars(contract, message.From.UtcDateTime, subscriptionType, interval)
							: session.SubscribeBars(contract, (int)message.Count, subscriptionType, interval, false);

						_subscriptions.Add(key, subscription);
					}
					else
						session.CancelSubscription(_subscriptions[key]);

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendOutMessage(new MarketDataMessage { OriginalTransactionId = message.TransactionId });
		}

		private void ProcessSecurityLookup(SecurityLookupMessage message)
		{
			var criteria = new OEC.API.SymbolLookupCriteria
			{
				SearchText = message.SecurityId.SecurityCode,
				ContractType = ContractType.Electronic,
				Mode = SymbolLookupMode.AnyInclusion,
			};

			if (!message.SecurityId.BoardCode.IsEmpty())
			{
				var exchange = SessionHolder.Session.Exchanges[message.SecurityId.BoardCode];

				if (exchange != null)
					criteria.Exchange = exchange;
			}

			if (!message.UnderlyingSecurityCode.IsEmpty())
			{
				var contract = SessionHolder.Session.BaseContracts[message.UnderlyingSecurityCode];

				if (contract != null)
					criteria.BaseContract = contract;
			}

			switch (message.SecurityType)
			{
				case SecurityTypes.Index:
					criteria.ContractKinds.AddRange(new[] { ContractKind.EquityIndex, ContractKind.Continuous, ContractKind.CustomCompound, ContractKind.FutureCompound, ContractKind.GenericCompound, ContractKind.OptionsCompound });
					break;
				case SecurityTypes.Stock:
					criteria.ContractKinds.AddRange(new[] { ContractKind.Equity });
					break;
				case SecurityTypes.Bond:
					criteria.ContractKinds.AddRange(new[] { ContractKind.Bond });
					break;
				case SecurityTypes.Forward:
					criteria.ContractKinds.AddRange(new[] { ContractKind.ForexForward });
					break;
				case SecurityTypes.Currency:
					criteria.ContractKinds.AddRange(new[] { ContractKind.Forex, ContractKind.ForexForward });
					break;
				case SecurityTypes.Future:
					criteria.ContractKinds.AddRange(new[] { ContractKind.Future, ContractKind.Continuous, ContractKind.FutureCompound });
					break;
				case SecurityTypes.Option:
					criteria.OptionsRequired = true;

					switch (message.OptionType)
					{
						case OptionTypes.Call:
							criteria.OptionType = SymbolLookupCriteriaOptionType.Call;
							break;
						case OptionTypes.Put:
							criteria.OptionType = SymbolLookupCriteriaOptionType.Put;
							break;
						case null:
							criteria.OptionType = SymbolLookupCriteriaOptionType.All;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					criteria.ContractKinds.AddRange(new[] { ContractKind.Option, ContractKind.EquityOption, ContractKind.OptionsCompound });
					break;
				case SecurityTypes.Fund:
					criteria.ContractKinds.AddRange(new[] { ContractKind.MutualFund });
					break;
				case null:
					criteria.OptionType = SymbolLookupCriteriaOptionType.All;
					break;
				default:
					throw new ArgumentOutOfRangeException("message", message.SecurityType, LocalizedStrings.Str2117);
			}

			SessionHolder.Session.SymbolLookup(criteria);
		}

		private void SessionOnSymbolLookupReceived(OEC.API.SymbolLookupCriteria criteria, ContractList contracts)
		{
			foreach (var contract in contracts)
			{
				ProcessContract(contract, contract.CurrentPrice, 0);
			}

			SendOutMessage(new SecurityLookupResultMessage());
		}

		private void ProcessContract(OEC.API.Contract contract, Price currentPrice, long originalTransactionId)
		{
			var secId = contract.ToSecurityId();
			
			SendOutMessage(new SecurityMessage
			{
				SecurityId = secId,
				Name = contract.Name,
				UnderlyingSecurityCode = contract.BaseSymbol,
				Currency = contract.Currency.Name.ToCurrency(),
				Strike = contract.Strike.SafeCast(),
				ExpiryDate = contract.HasExpiration ? (contract.ExpirationDate + contract.ExpirationTime.TimeOfDay).ApplyTimeZone(TimeHelper.Est) : (DateTimeOffset?)null,
				PriceStep = contract.TickSize.SafeCast(),
				OptionType = contract.IsOption ? (contract.Put ? OptionTypes.Put : OptionTypes.Call) : (OptionTypes?)null,
				SecurityType = contract.GetSecurityType(),
				OriginalTransactionId = originalTransactionId,
			});

			if (currentPrice == null)
				return;

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = currentPrice.LastDateTime.ApplyTimeZone(TimeHelper.Est),
			}
			.TryAdd(Level1Fields.LastTradePrice, contract.Cast(currentPrice.LastPrice))
			.TryAdd(Level1Fields.BestAskPrice, contract.Cast(currentPrice.AskPrice))
			.TryAdd(Level1Fields.BestAskVolume, (decimal)currentPrice.AskVol)
			.TryAdd(Level1Fields.BestBidPrice, contract.Cast(currentPrice.BidPrice))
			.TryAdd(Level1Fields.BestBidVolume, (decimal)currentPrice.BidVol)
			.TryAdd(Level1Fields.Change, contract.Cast(currentPrice.Change))
			.TryAdd(Level1Fields.OpenInterest, (decimal)currentPrice.OpenInterest)
			.TryAdd(Level1Fields.OpenPrice, contract.Cast(currentPrice.OpenPrice))
			.TryAdd(Level1Fields.HighPrice, contract.Cast(currentPrice.HighPrice))
			.TryAdd(Level1Fields.LowPrice, contract.Cast(currentPrice.LowPrice))
			.TryAdd(Level1Fields.LastTradeVolume, (decimal)currentPrice.LastVol)
			.TryAdd(Level1Fields.SettlementPrice, contract.Cast(currentPrice.Settlement))
			.TryAdd(Level1Fields.Volume, (decimal)currentPrice.TotalVol)
			.TryAdd(Level1Fields.StepPrice, (decimal)contract.ContractSize)
			.Add(Level1Fields.State, contract.GetSecurityState()));
		}

		private void SessionOnTicksReceived(Subscription subscription, OEC.API.Ticks ticks)
		{
			var contract = subscription.Contract;

			for (var i = 0; i < ticks.Exchanges.Length; i++)
			{
				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = contract.Symbol,
						BoardCode = GetBoardCode(ticks.Exchanges[i], contract, SessionHolder.AssociatedBoardCode),
					},
					ServerTime = ticks.Timestamps[i].ApplyTimeZone(TimeHelper.Est)
				}
				.TryAdd(Level1Fields.LastTradePrice, contract.Cast(ticks.Prices[i]))
				.TryAdd(Level1Fields.LastTradeVolume, (decimal)ticks.Volumes32[i])
				.TryAdd(Level1Fields.BestBidPrice, contract.Cast(ticks.BidPrices[i]))
				.TryAdd(Level1Fields.BestAskPrice, contract.Cast(ticks.AskPrices[i])));	
			}
		}

		private void SessionOnTradersChanged()
		{

		}

		private void SessionOnLoggedUserClientsChanged(LoggedUserClientList clients)
		{

		}

		private void SessionOnUserStatusChanged(OEC.API.User status)
		{

		}

		private void SessionOnUserMessage(OEC.API.User user, string message)
		{
			SendOutMessage(new Messages.NewsMessage
			{
				Source = user.Name,
				Headline = message,
				ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Est)
			});
		}

		private void SessionOnRelationsChanged()
		{

		}

		private void SessionOnQuoteDetailsChanged(OEC.API.Contract contract)
		{

		}

		private void SessionOnProductCalendarUpdated(OEC.API.BaseContract baseContract)
		{

		}

		private void SessionOnPriceTick(OEC.API.Contract contract, Price price)
		{
			ProcessContract(contract, price, 0);
		}

		private void SessionOnPriceChanged(OEC.API.Contract contract, Price price)
		{
			ProcessContract(contract, price, 0);
		}

		private void SessionOnPitGroupsChanged()
		{

		}

		private void SessionOnOsmAlgoListUpdated()
		{

		}

		private void SessionOnOsmAlgoListLoaded(IAlgoList algos)
		{

		}

		private void SessionOnNewsMessage(string channel, string message)
		{
			SendOutMessage(new Messages.NewsMessage
			{
				BoardCode = channel,
				Headline = message,
				ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Est)
			});
		}

		private void SessionOnIndexComponentsReceived(OEC.API.Contract contract)
		{
			ProcessContract(contract, contract.CurrentPrice, 0);
		}

		private void SessionOnHistoryReceived(Subscription subscription, OEC.API.Bar[] bars)
		{
			ProcessBars(subscription, bars, true);
		}

		private void SessionOnHistogramReceived(Subscription subscription, OEC.API.Histogram hist)
		{

		}

		private void SessionOnDealQuoteUpdated(OEC.API.DealQuote quote)
		{

		}

		private void SessionOnDomChanged(OEC.API.Contract contract)
		{
			var dom = contract.DOM;

			var bids = new List<QuoteChange>();
			var asks = new List<QuoteChange>();

			for (var i = 0; i < dom.BidExchanges.Length; i++)
				bids.Add(new QuoteChange(Sides.Buy, contract.Cast(dom.BidLevels[i]), dom.BidSizes[i]) { BoardCode = GetBoardCode(dom.BidExchanges[i], contract, null) });

			for (var i = 0; i < dom.AskExchanges.Length; i++)
				asks.Add(new QuoteChange(Sides.Sell, contract.Cast(dom.AskLevels[i]), dom.AskSizes[i]) { BoardCode = GetBoardCode(dom.AskExchanges[i], contract, null) });

			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = contract.Symbol,
					BoardCode = SessionHolder.AssociatedBoardCode
				},
				ServerTime = dom.LastUpdate.ApplyTimeZone(TimeHelper.Est),
				Bids = bids,
				Asks = asks,
			});
		}

		private static string GetBoardCode(string exchange, OEC.API.Contract contract, string defaultBoardCode)
		{
			return exchange ?? contract.Exchange.Name ?? defaultBoardCode;
		}

		private void SessionOnCurrencyPriceChanged(OEC.API.Currency currency, Price price)
		{

		}

		private void SessionOnContractsChanged(OEC.API.BaseContract contract)
		{

		}

		private void SessionOnContractRiskLimitChanged(OEC.API.Contract contract)
		{

		}

		private void SessionOnContractCreated(int requestId, OEC.API.Contract contract)
		{

		}

		private void SessionOnContractChanged(OEC.API.Contract contract)
		{

		}

		private void SessionOnContinuousContractRuleChanged(int requestId, OEC.API.ContinuousContractRule rule)
		{

		}

		private void SessionOnBarsReceived(Subscription subscription, OEC.API.Bar[] bars)
		{
			ProcessBars(subscription, bars, false);
		}

		private static Type GetCandleMessageType(MarketDataTypes type)
		{
			switch (type)
			{
				case MarketDataTypes.CandleTimeFrame:
					return typeof(TimeFrameCandleMessage);
				case MarketDataTypes.CandleTick:
					return typeof(TickCandleMessage);
				case MarketDataTypes.CandleVolume:
					return typeof(VolumeCandleMessage);
				case MarketDataTypes.CandleRange:
					return typeof(RangeCandleMessage);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ProcessBars(Subscription subscription, OEC.API.Bar[] bars, bool setFinished)
		{
			var key = _subscriptions.TryGetKey(subscription);

			var candleType = key == null ? typeof(TimeFrameCandleMessage) : GetCandleMessageType(key.Item2);

			var contract = subscription.Contract;

			foreach (var bar in bars)
			{
				var msg = candleType.CreateInstance<CandleMessage>();

				msg.SecurityId = new SecurityId
				{
					SecurityCode = contract.Symbol,
					BoardCode = contract.Exchange.Name,
				};
				msg.OpenTime = bar.Timestamp.ApplyTimeZone(TimeHelper.Est);
				msg.CloseTime = bar.CloseTimestamp.ApplyTimeZone(TimeHelper.Est);
				msg.OpenPrice = contract.Cast(bar.Open);
				msg.HighPrice = contract.Cast(bar.High);
				msg.LowPrice = contract.Cast(bar.Low);
				msg.ClosePrice = contract.Cast(bar.Close);
				msg.TotalVolume = bar.Volume64;
				msg.TotalTicks = bar.Ticks;
				msg.UpTicks = (int)bar.UpTicks;
				msg.DownTicks = (int)bar.DownTicks;
				msg.IsFinished = setFinished && bar == bars.Last();
				msg.State = CandleStates.Finished;

				SendOutMessage(msg);
			}
		}
	}
}