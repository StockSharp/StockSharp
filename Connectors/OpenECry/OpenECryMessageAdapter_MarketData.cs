#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.OpenECry.OpenECry
File: OpenECryMessageAdapter_MarketData.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	using APIContract = OEC.API.Contract;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class OpenECryMessageAdapter
	{
		private readonly SynchronizedDictionary<int, Tuple<SecurityId, MarketDataTypes, long>> _subscriptionDataBySid = new SynchronizedDictionary<int, Tuple<SecurityId, MarketDataTypes, long>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, Subscription> _subscriptionsByKey = new SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, Subscription>();
		private readonly SynchronizedSet<SecurityId> _processedSecurities = new SynchronizedSet<SecurityId>();
		private readonly SynchronizedPairSet<int, Action<ContractList>> _lookups = new SynchronizedPairSet<int, Action<ContractList>>();

		private APIContract LookupContract(SecurityId id, Message message)
		{
			var contract = _client.Contracts[id.SecurityCode];

			if (contract != null)
			{
				return contract;
			}

			// the message already back and we are prevent infinitive loop
			if (message.IsBack)
				throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(id.SecurityCode));

			var criteria = GetLookupCriteriaFromSecId(id);
			var clone = message.Clone();

			_lookups.Add(criteria.ID, clist =>
			{
				clone.IsBack = true;
				SendOutMessage(clone);
			});

			_client.SymbolLookup(criteria);
			return null;
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var key = Tuple.Create(message.SecurityId, message.DataType, message.Arg);

			APIContract contract = null;

			if (message.DataType != MarketDataTypes.News)
			{
				contract = LookupContract(message.SecurityId, message);

				if (contract == null)
					return;
			}

			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (message.IsSubscribe)
						_client.Subscribe(contract);
					else
						_client.Unsubscribe(contract);

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					if (message.IsSubscribe)
						_client.SubscribeDOM(contract);
					else
						_client.UnsubscribeDOM(contract);

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (message.IsSubscribe)
					{
						var subscription = _client.SubscribeTicks(contract, (message.From ?? DateTimeOffset.MinValue).UtcDateTime);

						_subscriptionDataBySid.Add(subscription.ID, Tuple.Create(message.SecurityId, message.DataType, message.TransactionId));
						_subscriptionsByKey.Add(key, subscription);
					}
					else
						CancelSubscription(key);

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

						var subscription = message.Count == null
							? _client.SubscribeBars(contract, (message.From ?? DateTimeOffset.MinValue).UtcDateTime, subscriptionType, interval, false)
							: _client.SubscribeBars(contract, (int)message.Count.Value, subscriptionType, interval, false);

						_subscriptionDataBySid.Add(subscription.ID, Tuple.Create(message.SecurityId, message.DataType, message.TransactionId));
						_subscriptionsByKey.Add(key, subscription);
					}
					else
						CancelSubscription(key);

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendOutMessage(new MarketDataMessage { OriginalTransactionId = message.TransactionId });
		}

		private void CancelSubscription(Tuple<SecurityId, MarketDataTypes, object> key)
		{
			var s = _subscriptionsByKey.TryGetValue(key);

			if (s != null)
			{
				_client.CancelSubscription(s);
				_subscriptionsByKey.Remove(key);
			}
			else
				throw new InvalidOperationException($"Subscription not found: {key}.");
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
				var exchange = _client.Exchanges[message.SecurityId.BoardCode];

				if (exchange != null)
					criteria.Exchange = exchange;
			}

			if (!message.UnderlyingSecurityCode.IsEmpty())
			{
				var contract = _client.BaseContracts[message.UnderlyingSecurityCode];

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
					throw new ArgumentOutOfRangeException(nameof(message), message.SecurityType, LocalizedStrings.Str2117);
			}

			if (message.TransactionId != 0)
				_lookups.Add(criteria.ID, clist => DefaultLookupHandler(clist, message.TransactionId));

			_client.SymbolLookup(criteria);
		}

		private OEC.API.SymbolLookupCriteria GetLookupCriteriaFromSecId(SecurityId id)
		{
			var criteria = new OEC.API.SymbolLookupCriteria
			{
				SearchText = id.SecurityCode,
				ContractType = ContractType.Electronic,
				Mode = SymbolLookupMode.ExactMatch,
				DesiredResultCount = 1
			};

			if (!id.BoardCode.IsEmpty())
			{
				var exchange = _client.Exchanges[id.BoardCode];

				if (exchange != null)
					criteria.Exchange = exchange;
			}

			return criteria;
		}

		private void SessionOnSymbolLookupReceived(OEC.API.SymbolLookupCriteria criteria, ContractList contracts)
		{
			_lookups.TryGetValue(criteria.ID)?.Invoke(contracts);
		}

		private void DefaultLookupHandler(ContractList contracts, long transId)
		{
			foreach (var contract in contracts)
				ProcessContract(contract, contract.CurrentPrice, transId);

			SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = transId });
		}

		private void ProcessContract(APIContract contract, long originalTransactionId)
		{
			var id = contract.ToSecurityId();

			if (!_processedSecurities.TryAdd(id))
				return;

			SendOutMessage(new SecurityMessage
			{
				SecurityId = id,
				Name = contract.Name,
				UnderlyingSecurityCode = contract.BaseSymbol,
				Currency = contract.Currency.Name.ToCurrency(),
				Strike = contract.Strike.ToDecimal(),
				ExpiryDate = contract.HasExpiration ? contract.ExpirationDate.ApplyTimeZone(TimeHelper.Est) : (DateTimeOffset?)null,
				PriceStep = contract.TickSize.ToDecimal(),
				Decimals = contract.PriceFormat > 0 ? contract.PriceFormat : (int?)null,
				OptionType = contract.IsOption ? (contract.Put ? OptionTypes.Put : OptionTypes.Call) : (OptionTypes?)null,
				SecurityType = contract.GetSecurityType(),
				OriginalTransactionId = originalTransactionId,
			});
		}

		private void ProcessContract(APIContract contract, Price currentPrice, long originalTransactionId)
		{
			ProcessContract(contract, originalTransactionId);

			if (currentPrice == null)
				return;

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = contract.ToSecurityId(),
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
				//var id = Interlocked.Increment(ref _tradeId);

				//SendOutMessage(new ExecutionMessage
				//{
				//	SecurityId = new SecurityId
				//	{
				//		SecurityCode = contract.Symbol,
				//		BoardCode = GetBoardCode(ticks.Exchanges[i], contract, AssociatedBoardCode),
				//	},
				//	ExecutionType = ExecutionTypes.Tick,
				//	ServerTime = ticks.Timestamps[i].ApplyTimeZone(TimeHelper.Est),
				//	TradePrice = contract.Cast(ticks.Prices[i]),
				//	TradeVolume = ticks.Volumes32[i],
				//	TradeId = id,
				//});

				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = contract.Symbol,
						BoardCode = GetBoardCode(ticks.Exchanges[i], contract, AssociatedBoardCode),
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
				ServerTime = CurrentTime.Convert(TimeHelper.Est)
			});
		}

		private void SessionOnRelationsChanged()
		{

		}

		private void SessionOnQuoteDetailsChanged(APIContract contract)
		{

		}

		private void SessionOnProductCalendarUpdated(OEC.API.BaseContract baseContract)
		{

		}

		private void SessionOnPriceTick(APIContract contract, Price price)
		{
			ProcessContract(contract, 0);
		}

		private void SessionOnPriceChanged(APIContract contract, Price price)
		{
			ProcessContract(contract, 0);
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
				ServerTime = CurrentTime.Convert(TimeHelper.Est)
			});
		}

		private void SessionOnIndexComponentsReceived(APIContract contract)
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

		private void SessionOnDomChanged(APIContract contract)
		{
			ProcessContract(contract, 0);

			var dom = contract.DOM;

			var bids = new List<QuoteChange>();
			var asks = new List<QuoteChange>();

			var hasExchange = false;

			for (var i = 0; i < dom.BidExchanges.Length; i++)
			{
				var boardCode = GetBoardCode(dom.BidExchanges[i], contract, null);

				if (!hasExchange)
					hasExchange = !boardCode.IsEmpty() && boardCode != contract.Exchange.Name;

				bids.Add(new QuoteChange(Sides.Buy, contract.Cast(dom.BidLevels[i]) ?? 0, dom.BidSizes[i]) { BoardCode = boardCode });
			}

			for (var i = 0; i < dom.AskExchanges.Length; i++)
			{
				var boardCode = GetBoardCode(dom.AskExchanges[i], contract, null);

				if (!hasExchange)
					hasExchange = !boardCode.IsEmpty() && boardCode != contract.Exchange.Name;

				asks.Add(new QuoteChange(Sides.Sell, contract.Cast(dom.AskLevels[i]) ?? 0, dom.AskSizes[i]) { BoardCode = boardCode });
			}

			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = contract.Symbol,
					BoardCode = hasExchange ? AssociatedBoardCode : contract.Exchange.Name ?? AssociatedBoardCode
				},
				ServerTime = dom.LastUpdate.ApplyTimeZone(TimeHelper.Est),
				Bids = bids,
				Asks = asks,
			});
		}

		private static string GetBoardCode(string exchange, APIContract contract, string defaultBoardCode)
		{
			return exchange ?? contract.Exchange.Name ?? defaultBoardCode;
		}

		private void SessionOnCurrencyPriceChanged(OEC.API.Currency currency, Price price)
		{

		}

		private void SessionOnContractsChanged(OEC.API.BaseContract contract)
		{

		}

		private void SessionOnContractRiskLimitChanged(APIContract contract)
		{

		}

		private void SessionOnContractCreated(int requestId, APIContract contract)
		{

		}

		private void SessionOnContractChanged(APIContract contract)
		{

		}

		private void SessionOnContinuousContractRuleChanged(int requestId, OEC.API.ContinuousContractRule rule)
		{

		}

		private void SessionOnBarsReceived(Subscription subscription, OEC.API.Bar[] bars)
		{
			ProcessBars(subscription, bars, false);
		}

		private void ProcessBars(Subscription subscription, OEC.API.Bar[] bars, bool setFinished)
		{
			var tuple = _subscriptionDataBySid.TryGetValue(subscription.ID);

			var candleType = tuple?.Item2.ToCandleMessage() ?? typeof(TimeFrameCandleMessage);
			var transId = tuple?.Item3 ?? 0;

			var contract = subscription.Contract;

			ProcessContract(contract, 0);

			var i = 0;

			foreach (var bar in bars)
			{
				var msg = candleType.CreateInstance<CandleMessage>();

				msg.SecurityId = new SecurityId
				{
					SecurityCode = contract.Symbol,
					BoardCode = contract.Exchange.Name ?? AssociatedBoardCode,
				};
				msg.OpenTime = bar.Timestamp.ApplyTimeZone(TimeHelper.Est);
				msg.CloseTime = bar.CloseTimestamp.ApplyTimeZone(TimeHelper.Est);
				msg.OpenPrice = contract.Cast(bar.Open) ?? 0;
				msg.HighPrice = contract.Cast(bar.High) ?? 0;
				msg.LowPrice = contract.Cast(bar.Low) ?? 0;
				msg.ClosePrice = contract.Cast(bar.Close) ?? 0;
				msg.TotalVolume = bar.Volume64;
				msg.TotalTicks = bar.Ticks;
				msg.UpTicks = (int)bar.UpTicks;
				msg.DownTicks = (int)bar.DownTicks;
				msg.IsFinished = setFinished && bar == bars.Last();
				msg.State = ++i == bars.Length ? CandleStates.Active : CandleStates.Finished;
				msg.OriginalTransactionId = transId;
				msg.Arg = TimeSpan.FromMinutes(subscription.IntInterval);

				SendOutMessage(msg);
			}
		}
	}
}