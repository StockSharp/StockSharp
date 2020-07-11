namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class MarketRuleHelper
	{
		private abstract class MarketDepthRule : MarketRule<MarketDepth, MarketDepth>
		{
			protected MarketDepthRule(MarketDepth depth)
				: base(depth)
			{
				Depth = depth ?? throw new ArgumentNullException(nameof(depth));
			}

			protected MarketDepth Depth { get; }
		}

		private sealed class MarketDepthChangedRule : MarketDepthRule
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

				Name = LocalizedStrings.Str1056 + " " + depth.Security;

				if (provider == null)
				{
#pragma warning disable 612
					Depth.QuotesChanged += OnQuotesChanged;
#pragma warning restore 612
				}
				else
				{
					_provider = provider;
#pragma warning disable CS0618 // Type or member is obsolete
					_provider.MarketDepthChanged += ProviderOnMarketDepthChanged;
#pragma warning restore CS0618 // Type or member is obsolete
				}
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
				if (_provider == null)
				{
#pragma warning disable 612
					Depth.QuotesChanged -= OnQuotesChanged;
#pragma warning restore 612
				}
				else
				{
#pragma warning disable CS0618 // Type or member is obsolete
					_provider.MarketDepthChanged -= ProviderOnMarketDepthChanged;
#pragma warning restore CS0618 // Type or member is obsolete
				}

				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the order book change event.
		/// </summary>
		/// <param name="depth">The order book to be traced for change event.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenChanged(this MarketDepth depth, IMarketDataProvider provider = null)
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
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadMore(this MarketDepth depth, Unit price, IMarketDataProvider provider = null)
		{
			var pair = depth.BestPair;
			var firstPrice = pair?.SpreadPrice ?? 0;
			return new MarketDepthChangedRule(depth, provider, d => d.BestPair != null && d.BestPair.SpreadPrice > (firstPrice + price))
			{
				Name = LocalizedStrings.Str1057Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of order book spread size decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the spread change event.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadLess(this MarketDepth depth, Unit price, IMarketDataProvider provider = null)
		{
			var pair = depth.BestPair;
			var firstPrice = pair?.SpreadPrice ?? 0;
			return new MarketDepthChangedRule(depth, provider, d => d.BestPair != null && d.BestPair.SpreadPrice < (firstPrice - price))
			{
				Name = LocalizedStrings.Str1058Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best bid increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best bid increase on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceMore(this MarketDepth depth, Unit price, IMarketDataProvider provider = null)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestBid2, false))
			{
				Name = LocalizedStrings.Str1059Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best bid decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best bid decrease on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceLess(this MarketDepth depth, Unit price, IMarketDataProvider provider = null)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestBid2, true))
			{
				Name = LocalizedStrings.Str1060Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best offer increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best offer increase on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceMore(this MarketDepth depth, Unit price, IMarketDataProvider provider = null)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestAsk2, false))
			{
				Name = LocalizedStrings.Str1061Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best offer decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best offer decrease on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <param name="provider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceLess(this MarketDepth depth, Unit price, IMarketDataProvider provider = null)
		{
			return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestAsk2, true))
			{
				Name = LocalizedStrings.Str1062Params.Put(depth.Security, price)
			};
		}

		private static Func<MarketDepth, bool> CreateDepthCondition(Unit price, Func<QuoteChange?> currentQuote, bool isLess)
		{
			if (price == null)
				throw new ArgumentNullException(nameof(price));

			if (currentQuote == null)
				throw new ArgumentNullException(nameof(currentQuote));

			if (price.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, nameof(price));

			if (price.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, nameof(price));

			var q = currentQuote();
			if (q == null)
				throw new ArgumentException(LocalizedStrings.Str1063, nameof(currentQuote));

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
	}
}
