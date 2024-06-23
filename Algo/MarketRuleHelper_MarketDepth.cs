namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class MarketRuleHelper
	{
		[Obsolete]
		private abstract class MarketDepthRule : MarketRule<MarketDepth, MarketDepth>
		{
			protected MarketDepthRule(MarketDepth depth)
				: base(depth)
			{
				Depth = depth ?? throw new ArgumentNullException(nameof(depth));
			}

			protected MarketDepth Depth { get; }
		}

		[Obsolete]
		private class MarketDepthChangedRule : MarketDepthRule
		{
			private readonly Func<MarketDepth, bool> _condition;
			private readonly IMarketDataProvider _provider;

			public MarketDepthChangedRule(MarketDepth depth, IMarketDataProvider provider)
				: this(depth, provider, d => true)
			{
			}

			public MarketDepthChangedRule(MarketDepth depth, IMarketDataProvider provider, Func<MarketDepth, bool> condition)
				: base(depth)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.MarketDepth + " " + depth.Security;

				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_provider.MarketDepthChanged += ProviderOnMarketDepthChanged;
			}

			private void ProviderOnMarketDepthChanged(MarketDepth depth)
			{
				if (Depth == depth)
					OnQuotesChanged();
			}

			private void OnQuotesChanged()
			{
				if (_condition(Depth))
					Activate(Depth);
			}

			protected override void DisposeManaged()
			{
				_provider.MarketDepthChanged -= ProviderOnMarketDepthChanged;

				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the order book change event.
		/// </summary>
		/// <param name="depth">The order book to be traced for change event.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<MarketDepth, MarketDepth> WhenChanged(this MarketDepth depth, IMarketDataProvider provider)
		{
			return new MarketDepthChangedRule(depth, provider);
		}

		/// <summary>
		/// To create a rule for the event of order book spread size increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the spread change event.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadMore(this MarketDepth depth, Unit price, IMarketDataProvider provider)
		{
			var pair = depth.BestPair;
			var firstPrice = pair?.SpreadPrice ?? 0;
			return new MarketDepthChangedRule(depth, provider, d => d.BestPair != null && d.BestPair.SpreadPrice > (firstPrice + price))
			{
				Name = $"{depth.Security} spread > {price}"
			};
		}

		/// <summary>
		/// To create a rule for the event of order book spread size decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the spread change event.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadLess(this MarketDepth depth, Unit price, IMarketDataProvider provider)
		{
			var pair = depth.BestPair;
			var firstPrice = pair?.SpreadPrice ?? 0;
			return new MarketDepthChangedRule(depth, provider, d => d.BestPair != null && d.BestPair.SpreadPrice < (firstPrice - price))
			{
				Name = $"{depth.Security} spread < {price}"
			};
		}

		/// <summary>
		/// To create a rule for the event of the best bid increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best bid increase on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceMore(this MarketDepth depth, Unit price, IMarketDataProvider provider)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestBid, false))
			{
				Name = $"{depth.Security} {LocalizedStrings.BestBid} > {price}"
			};
		}

		/// <summary>
		/// To create a rule for the event of the best bid decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best bid decrease on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceLess(this MarketDepth depth, Unit price, IMarketDataProvider provider)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestBid, true))
			{
				Name = $"{depth.Security} {LocalizedStrings.BestBid} < {price}"
			};
		}

		/// <summary>
		/// To create a rule for the event of the best offer increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best offer increase on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceMore(this MarketDepth depth, Unit price, IMarketDataProvider provider)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestAsk, false))
			{
				Name = $"{depth.Security} {LocalizedStrings.BestAsk} > {price}"
			};
		}

		/// <summary>
		/// To create a rule for the event of the best offer decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best offer decrease on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceLess(this MarketDepth depth, Unit price, IMarketDataProvider provider)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestAsk, true))
			{
				Name = $"{depth.Security} {LocalizedStrings.BestAsk} < {price}"
			};
		}

		[Obsolete]
		private static Func<MarketDepth, bool> CreateDepthCondition(Unit price, Func<QuoteChange?> currentQuote, bool isLess)
		{
			if (price == null)
				throw new ArgumentNullException(nameof(price));

			if (currentQuote == null)
				throw new ArgumentNullException(nameof(currentQuote));

			if (price <= 0)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

			var q = currentQuote();
			if (q == null)
				throw new ArgumentException(LocalizedStrings.QuoteMissed, nameof(currentQuote));

			var curQuote = q.Value;

			if (isLess)
			{
				var finishPrice = (decimal)(curQuote.Price - price);
				return depth =>
				{
					var quote = currentQuote();
					return quote != null && quote.Value.Price < finishPrice;
				};
			}
			else
			{
				var finishPrice = (decimal)(curQuote.Price + price);
				return depth =>
				{
					var quote = currentQuote();
					return quote != null && quote.Value.Price > finishPrice;
				};
			}
		}

		[Obsolete]
		private class MarketDepthReceivedRule : SubscriptionRule<MarketDepth>
		{
			public MarketDepthReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.MarketDepthReceived)}";
				Provider.MarketDepthReceived += ProviderOnMarketDepthReceived;
			}

			private void ProviderOnMarketDepthReceived(Subscription subscription, MarketDepth depth)
			{
				if (Subscription == subscription)
					Activate(depth);
			}

			protected override void DisposeManaged()
			{
				Provider.MarketDepthReceived -= ProviderOnMarketDepthReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.MarketDepthReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		[Obsolete("Use WhenOrderBookReceived method.")]
		public static MarketRule<Subscription, MarketDepth> WhenMarketDepthReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new MarketDepthReceivedRule(subscription, provider);
		}
	}
}
