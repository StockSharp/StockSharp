namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class MarketRuleHelper
	{
		private abstract class SecurityRule<TArg> : MarketRule<Security, TArg>
		{
			protected SecurityRule(Security security, IMarketDataProvider provider)
				: base(security)
			{
				Security = security ?? throw new ArgumentNullException(nameof(security));
				Provider = provider ?? throw new ArgumentNullException(nameof(provider));
			}

			protected Security Security { get; }
			protected IMarketDataProvider Provider { get; }
		}

		[Obsolete]
		private class SecurityChangedRule : SecurityRule<Security>
		{
			private readonly Func<Security, bool> _condition;

			public SecurityChangedRule(Security security, IMarketDataProvider provider)
				: this(security, provider, s => true)
			{
			}

			public SecurityChangedRule(Security security, IMarketDataProvider provider, Func<Security, bool> condition)
				: base(security, provider)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.Level1 + " " + security;

				Provider.SecurityChanged += OnSecurityChanged;
			}

			private void OnSecurityChanged(Security security)
			{
				if (Security is BasketSecurity basket)
				{
					if (basket.Contains(ServicesRegistry.TrySecurityProvider, security) && _condition(security))
						Activate(security);
				}
				else
				{
					if (security == Security && _condition(Security))
						Activate(Security);
				}
			}

			protected override void DisposeManaged()
			{
				Provider.SecurityChanged -= OnSecurityChanged;
				base.DisposeManaged();
			}
		}

		[Obsolete]
		private class SecurityNewTradeRule : SecurityRule<Trade>
		{
			public SecurityNewTradeRule(Security security, IMarketDataProvider provider)
				: base(security, provider)
			{
				Name = LocalizedStrings.TradesElement + " " + security;
				Provider.NewTrade += OnNewTrade;
			}

			private void OnNewTrade(Trade trade)
			{
				var sec = Security;

				var basket = sec as BasketSecurity;

				var has = basket?.Contains(ServicesRegistry.TrySecurityProvider, trade.Security) ?? trade.Security == sec;

				if (has)
					Activate(trade);
			}

			protected override void DisposeManaged()
			{
				Provider.NewTrade -= OnNewTrade;
				base.DisposeManaged();
			}
		}

		[Obsolete]
		private class SecurityNewOrderLogItemRule : SecurityRule<OrderLogItem>
		{
			public SecurityNewOrderLogItemRule(Security security, IMarketDataProvider provider)
				: base(security, provider)
			{
				Name = LocalizedStrings.OrderLog + " " + security;
				Provider.NewOrderLogItem += OnNewOrderLogItem;
			}

			private void OnNewOrderLogItem(OrderLogItem item)
			{
				var sec = Security;

				var basket = sec as BasketSecurity;

				var has = basket?.Contains(ServicesRegistry.TrySecurityProvider, item.Order.Security) ?? item.Order.Security == sec;

				if (has)
					Activate(item);
			}

			protected override void DisposeManaged()
			{
				Provider.NewOrderLogItem -= OnNewOrderLogItem;
				base.DisposeManaged();
			}
		}

		[Obsolete]
		private class SecurityLastTradeRule : SecurityRule<Security>
		{
			private readonly Func<Security, bool> _condition;

			public SecurityLastTradeRule(Security security, IMarketDataProvider provider, Func<Security, bool> condition)
				: base(security, provider)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.LastTrade + " " + security;

				Provider.SecurityChanged += OnSecurityChanged;
				Provider.NewTrade += OnNewTrade;
			}

			private void OnSecurityChanged(Security security)
			{
				if (CheckLastTrade(security))
					Activate(security);
			}

			private bool CheckLastTrade(Security security)
			{
				if (Security is BasketSecurity basket)
				{
					return basket.Contains(ServicesRegistry.TrySecurityProvider, security) && _condition(security);
				}
				else
				{
					return security == Security && _condition(Security);
				}
			}

			private void OnNewTrade(Trade trade)
			{
				if (CheckTrades(Security, trade))
					Activate(trade.Security);
			}

			private bool CheckTrades(Security security, Trade trade)
			{
				return security is BasketSecurity basket
					? basket.Contains(ServicesRegistry.TrySecurityProvider, trade.Security) && _condition(trade.Security)
					: trade.Security == security && _condition(trade.Security);
			}

			protected override void DisposeManaged()
			{
				Provider.NewTrade -= OnNewTrade;
				Provider.SecurityChanged -= OnSecurityChanged;

				base.DisposeManaged();
			}
		}

		[Obsolete]
		private class SecurityMarketDepthChangedRule : SecurityRule<MarketDepth>
		{
			public SecurityMarketDepthChangedRule(Security security, IMarketDataProvider provider)
				: base(security, provider)
			{
				Name = LocalizedStrings.MarketDepth + " " + security;
				Provider.MarketDepthChanged += OnMarketDepthChanged;
			}

			private void OnMarketDepthChanged(MarketDepth depth)
			{
				if (depth.Security != Security)
					return;

				Activate(depth);
			}

			protected override void DisposeManaged()
			{
				Provider.MarketDepthChanged -= OnMarketDepthChanged;
				base.DisposeManaged();
			}
		}

		[Obsolete]
		private class BasketSecurityMarketDepthChangedRule : SecurityRule<MarketDepth>
		{
			public BasketSecurityMarketDepthChangedRule(BasketSecurity security, IMarketDataProvider provider)
				: base(security, provider)
			{
				Name = LocalizedStrings.MarketDepth + " " + security;
				Provider.MarketDepthChanged += OnMarketDepthChanged;
			}

			private void OnMarketDepthChanged(MarketDepth depth)
			{
				if (CheckDepth(Security, depth))
					Activate(depth);
			}

			private static bool CheckDepth(Security security, MarketDepth depth)
			{
				var basket = security as BasketSecurity;

				return basket?.Contains(ServicesRegistry.TrySecurityProvider, depth.Security) ?? depth.Security == security;
			}

			protected override void DisposeManaged()
			{
				Provider.MarketDepthChanged -= OnMarketDepthChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the instrument change event.
		/// </summary>
		/// <param name="security">The instrument to be traced for changes.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenLevel1Received method.")]
		public static MarketRule<Security, Security> WhenChanged(this Security security, IMarketDataProvider provider)
		{
			return new SecurityChangedRule(security, provider);
		}

		/// <summary>
		/// To create a rule for the event of new trade occurrence for the instrument.
		/// </summary>
		/// <param name="security">The instrument to be traced for new trade occurrence event.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenTickTradeReceived method.")]
		public static MarketRule<Security, Trade> WhenNewTrade(this Security security, IMarketDataProvider provider)
		{
			return new SecurityNewTradeRule(security, provider);
		}

		/// <summary>
		/// To create a rule for the event of new notes occurrence in the orders log for instrument.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of new notes occurrence in the orders log.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderLogReceived method.")]
		public static MarketRule<Security, OrderLogItem> WhenNewOrderLogItem(this Security security, IMarketDataProvider provider)
		{
			return new SecurityNewOrderLogItemRule(security, provider);
		}

		/// <summary>
		/// To create a rule for the event of order book change by instrument.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of order book change by instrument.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<Security, MarketDepth> WhenMarketDepthChanged(this Security security, IMarketDataProvider provider)
		{
			return new SecurityMarketDepthChangedRule(security, provider);
		}

		/// <summary>
		/// To create a rule for the event of order book change by instruments basket.
		/// </summary>
		/// <param name="security">Instruments basket to be traced for the event of order books change by internal instruments.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<Security, MarketDepth> WhenMarketDepthChanged(this BasketSecurity security, IMarketDataProvider provider)
		{
			return new BasketSecurityMarketDepthChangedRule(security, provider);
		}

		/// <summary>
		/// To create a rule for the event of excess of the best bid of specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of excess of the best bid of specific level.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenTickTradeReceived method.")]
		public static MarketRule<Security, Security> WhenBestBidPriceMore(this Security security, IMarketDataProvider provider, Unit price)
		{
			return CreateSecurityCondition(security, provider, Level1Fields.BestBidPrice, price, false);
		}

		/// <summary>
		/// To create a rule for the event of dropping the best bid below the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of dropping the best bid below the specific level.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenTickTradeReceived method.")]
		public static MarketRule<Security, Security> WhenBestBidPriceLess(this Security security, IMarketDataProvider provider, Unit price)
		{
			return CreateSecurityCondition(security, provider, Level1Fields.BestBidPrice, price, true);
		}

		/// <summary>
		/// To create a rule for the event of excess of the best offer of the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of excess of the best offer of the specific level.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenTickTradeReceived method.")]
		public static MarketRule<Security, Security> WhenBestAskPriceMore(this Security security, IMarketDataProvider provider, Unit price)
		{
			return CreateSecurityCondition(security, provider, Level1Fields.BestAskPrice, price, false);
		}

		/// <summary>
		/// To create a rule for the event of dropping the best offer below the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of dropping the best offer below the specific level.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenTickTradeReceived method.")]
		public static MarketRule<Security, Security> WhenBestAskPriceLess(this Security security, IMarketDataProvider provider, Unit price)
		{
			return CreateSecurityCondition(security, provider, Level1Fields.BestAskPrice, price, true);
		}

		/// <summary>
		/// To create a rule for the event of increase of the last trade price above the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of increase of the last trade price above the specific level.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenTickTradeReceived method.")]
		public static MarketRule<Security, Security> WhenLastTradePriceMore(this Security security, IMarketDataProvider provider, Unit price)
		{
			return CreateLastTradeCondition(security, provider, price, false);
		}

		/// <summary>
		/// To create a rule for the event of reduction of the last trade price below the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of reduction of the last trade price below the specific level.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenTickTradeReceived method.")]
		public static MarketRule<Security, Security> WhenLastTradePriceLess(this Security security, IMarketDataProvider provider, Unit price)
		{
			return CreateLastTradeCondition(security, provider, price, true);
		}

		[Obsolete]
		private static SecurityChangedRule CreateSecurityCondition(Security security, IMarketDataProvider provider, Level1Fields field, Unit offset, bool isLess)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (offset == null)
				throw new ArgumentNullException(nameof(offset));

			if (offset <= 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, LocalizedStrings.InvalidValue);

			var price = (decimal?)provider.GetSecurityValue(security, field);

			if (price == null && offset.Type != UnitTypes.Limit)
				throw new InvalidOperationException(LocalizedStrings.QuoteMissed);

			if (isLess)
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price - offset);
				return new SecurityChangedRule(security, provider, s =>
				{
					var quote = (decimal?)provider.GetSecurityValue(s, field);
					return quote != null && quote < finishPrice;
				});
			}
			else
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price + offset);
				return new SecurityChangedRule(security, provider, s =>
				{
					var quote = (decimal?)provider.GetSecurityValue(s, field);
					return quote != null && quote > finishPrice;
				});
			}
		}

		[Obsolete]
		private static SecurityLastTradeRule CreateLastTradeCondition(Security security, IMarketDataProvider provider, Unit offset, bool isLess)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (offset == null)
				throw new ArgumentNullException(nameof(offset));

			if (offset <= 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, LocalizedStrings.InvalidValue);

			var price = (decimal?)provider.GetSecurityValue(security, Level1Fields.LastTradePrice);

			if (price == null && offset.Type != UnitTypes.Limit)
				throw new ArgumentException(LocalizedStrings.NoInfoAboutLastTrade, nameof(security));

			if (isLess)
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price - offset);
				return new SecurityLastTradeRule(security, provider, s => (decimal?)provider.GetSecurityValue(s, Level1Fields.LastTradePrice) < finishPrice);
			}
			else
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price + offset);
				return new SecurityLastTradeRule(security, provider, s => (decimal?)provider.GetSecurityValue(s, Level1Fields.LastTradePrice) > finishPrice);
			}
		}

		private sealed class TimeComeRule : MarketRule<IConnector, DateTimeOffset>
		{
			private readonly MarketTimer _timer;

			public TimeComeRule(IConnector connector, IEnumerable<DateTimeOffset> times)
				: base(connector)
			{
				if (times == null)
					throw new ArgumentNullException(nameof(times));

				var currentTime = connector.CurrentTime;

				var intervals = new SynchronizedQueue<TimeSpan>();
				var timesList = new SynchronizedList<DateTimeOffset>();

				foreach (var time in times)
				{
					var interval = time - currentTime;

					if (interval <= TimeSpan.Zero)
						continue;

					intervals.Enqueue(interval);
					currentTime = time;
					timesList.Add(time);
				}

				// все даты устарели
				if (timesList.IsEmpty())
					return;

				Name = LocalizedStrings.Time;

				var index = 0;

				_timer = new MarketTimer(connector, () =>
				{
					var activateTime = timesList[index++];

					Activate(activateTime);

					if (index == timesList.Count)
					{
						_timer.Stop();
					}
					else
					{
						_timer.Interval(intervals.Dequeue());
					}
				})
				.Interval(intervals.Dequeue())
				.Start();
			}

			protected override bool CanFinish()
			{
				return _timer == null || base.CanFinish();
			}

			protected override void DisposeManaged()
			{
				_timer?.Dispose();

				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule, activated at the exact time, specified through <paramref name="times" />.
		/// </summary>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="times">The exact time. Several values may be sent.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, DateTimeOffset> WhenTimeCome(this IConnector connector, params DateTimeOffset[] times)
		{
			return connector.WhenTimeCome((IEnumerable<DateTimeOffset>)times);
		}

		/// <summary>
		/// To create a rule, activated at the exact time, specified through <paramref name="times" />.
		/// </summary>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="times">The exact time. Several values may be sent.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, DateTimeOffset> WhenTimeCome(this IConnector connector, IEnumerable<DateTimeOffset> times)
		{
			return new TimeComeRule(connector, times);
		}
	}
}
