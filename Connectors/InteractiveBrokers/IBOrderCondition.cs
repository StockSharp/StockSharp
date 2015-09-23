namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// <see cref="InteractiveBrokers"/> order condition.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Interactive Brokers")]
	public class IBOrderCondition : OrderCondition
	{
		/// <summary>
		/// Base condition.
		/// </summary>
		public abstract class BaseCondition
		{
			private readonly IBOrderCondition _condition;

			internal BaseCondition(IBOrderCondition condition)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;
			}

			/// <summary>
			/// Get parameter value.
			/// </summary>
			/// <typeparam name="T">Value type.</typeparam>
			/// <param name="name">Parameter name.</param>
			/// <returns>The parameter value.</returns>
			protected T GetValue<T>(string name)
			{
				if (!_condition.Parameters.ContainsKey(name))
					throw new ArgumentException(LocalizedStrings.Str2311Params.Put(name), "name");

				return (T)_condition.Parameters[name];
			}

			/// <summary>
			/// To get the parameter value. If the value does not exist the <see langword="null" /> will be returned.
			/// </summary>
			/// <typeparam name="T">Value type.</typeparam>
			/// <param name="name">Parameter name.</param>
			/// <returns>The parameter value.</returns>
			protected T TryGetValue<T>(string name)
			{
				return (T)_condition.Parameters.TryGetValue(name);
			}

			/// <summary>
			/// To set a new parameter value.
			/// </summary>
			/// <typeparam name="T">Value type.</typeparam>
			/// <param name="name">Parameter name.</param>
			/// <param name="value">The parameter value.</param>
			protected void SetValue<T>(string name, T value)
			{
				_condition.Parameters[name] = value;
			}
		}

		/// <summary>
		/// Extended orders types which are specific to <see cref="IBTrader"/>.
		/// </summary>
		public enum ExtendedOrderTypes
		{
			/// <summary>
			/// To match at the market price, if the closing price is higher than the expected price.
			/// </summary>
			/// <remarks>
			/// Not valid for US <see cref="SecurityTypes.Future"/>, US <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str2312Key)]
			MarketOnClose,

			/// <summary>
			/// To match at the specified price, if the closing price is higher than the expected price.
			/// </summary>
			/// <remarks>
			/// Not valid for US <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.LimitOnCloseKey)]
			LimitOnClose,

			/// <summary>
			/// At best price.
			/// </summary>
			/// <remarks>
			/// Valid until <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str2314Key)]
			PeggedToMarket,

			/// <summary>
			/// The stop with the market activation price.
			/// </summary>
			/// <remarks>
			/// Valid for <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>, <see cref="SecurityTypes.Warrant"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
			Stop,

			/// <summary>
			/// Stop with the specified activation price.
			/// </summary>
			/// <remarks>
			/// Valid for <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str1733Key)]
			StopLimit,

			/// <summary>
			/// Trailing stop-loss.
			/// </summary>
			/// <remarks>
			/// Valid for <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>, <see cref="SecurityTypes.Warrant"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.TrailingKey)]
			TrailingStop,

			/// <summary>
			/// With offset.
			/// </summary>
			/// <remarks>
			/// Valid for <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str2316Key)]
			Relative,

			/// <summary>
			/// VWAP.
			/// </summary>
			/// <remarks>
			/// Valid until <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayName("VWAP")]
			VolumeWeightedAveragePrice,

			/// <summary>
			/// Limit trailing stop.
			/// </summary>
			/// <remarks>
			/// Valid for <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>, <see cref="SecurityTypes.Warrant"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.TrailingStopLimitKey)]
			TrailingStopLimit,

			/// <summary>
			/// Volatility.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.VolatilityKey)]
			Volatility,

			/// <summary>
			/// It used for delta orders.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str1658Key)]
			Empty,

			/// <summary>
			/// It used for delta neutral orders types.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2319Key)]
			Default,

			/// <summary>
			/// To be changed on price increment.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2320Key)]
			Scale,

			/// <summary>
			/// With the market price when the condition is fulfilled.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.MarketOnTouchKey)]
			MarketIfTouched,

			/// <summary>
			/// With the specified price when the condition is fulfilled.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.LimitOnTouchKey)]
			LimitIfTouched
		}

		/// <summary>
		/// Orders modes such as OCA (One-Cancels All).
		/// </summary>
		public enum OcaTypes
		{
			/// <summary>
			/// To cancel all remaining blocks.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.CancelAllKey)]
			CancelAll = 1,

			/// <summary>
			/// The remaining orders proportionally to decrease by the size of the block.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2324Key)]
			ReduceWithBlock = 2,

			/// <summary>
			/// The remaining orders proportionally to decrease by the size out of the block.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2325Key)]
			ReduceWithNoBlock = 3
		}

		/// <summary>
		/// OCA (One-Cancels All) settings.
		/// </summary>
		public class OcaCondition : BaseCondition
		{
			internal OcaCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Group ID.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.GroupIdKey)]
			[DescriptionLoc(LocalizedStrings.GroupIdKey, true)]
			public string Group
			{
				get { return TryGetValue<string>("OcaGroup"); }
				set { SetValue("OcaGroup", value); }
			}

			/// <summary>
			/// Group type.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.GroupTypeKey)]
			[DescriptionLoc(LocalizedStrings.GroupTypeKey, true)]
			public OcaTypes? Type
			{
				get { return TryGetValue<OcaTypes?>("OcaType"); }
				set { SetValue("OcaType", value); }
			}
		}

		/// <summary>
		/// Conditions for stop orders activation.
		/// </summary>
		public enum TriggerMethods
		{
			/// <summary>
			/// For NASDAQ <see cref="SecurityTypes.Stock"/> and US <see cref="SecurityTypes.Option"/> the <see cref="TriggerMethods.DoubleBidAsk"/> condition is used. Otherwise, the <see cref="TriggerMethods.BidAsk"/> condition is used.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2319Key)]
			Default = 0,

			/// <summary>
			/// Double increase or decrease of the current best price before the stop price.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2330Key)]
			DoubleBidAsk = 1,

			/// <summary>
			/// Increase or decrease of the last trade price before the stop price.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2331Key)]
			Last = 2,

			/// <summary>
			/// Double increase or decrease of the last trade price before the stop price.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2332Key)]
			DoubleLast = 3,

			/// <summary>
			/// Increase or decrease of the current best price before the stop price.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str273Key)]
			BidAsk = 4,

			/// <summary>
			/// Increase or decrease of the current best price or the last trade price before the stop price.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2333Key)]
			LastOrBidAsk = 7,

			/// <summary>
			/// Increase or decrease of the mid-spread before the stop price.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str500Key)]
			MidpointMethod = 8
		}

		/// <summary>
		/// Descriptions of trader type by the 80A rule.
		/// </summary>
		public enum AgentDescriptions
		{
			/// <summary>
			/// Private trader.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2334Key)]
			Individual,

			/// <summary>
			/// Agency.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.AgencyKey)]
			Agency,

			/// <summary>
			/// Agency of other type.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2336Key)]
			AgentOtherMember,

			/// <summary>
			/// Individual PTIA.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2337Key)]
			IndividualPTIA,

			/// <summary>
			/// Agency PTIA.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2338Key)]
			AgencyPTIA,

			/// <summary>
			/// Agency of other type PTIA.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2339Key)]
			AgentOtherMemberPTIA,

			/// <summary>
			/// Individual PT.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2340Key)]
			IndividualPT,

			/// <summary>
			/// Agency PT.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2341Key)]
			AgencyPT,

			/// <summary>
			/// Agency of other type PT.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2342Key)]
			AgentOtherMemberPT,
		}

		/// <summary>
		/// Methods for volumes automatic calculation for the accounts group.
		/// </summary>
		public enum FinancialAdvisorAllocations
		{
			/// <summary>
			/// Percentage change.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2343Key)]
			PercentChange,

			/// <summary>
			/// Using free cash plus borrowed.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.EquityKey)]
			AvailableEquity,

			/// <summary>
			/// Using free cash.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2345Key)]
			NetLiquidity,

			/// <summary>
			/// An equal volume.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.VolumeKey)]
			EqualQuantity,
		}

		/// <summary>
		/// Settings for automatic order volume calculation.
		/// </summary>
		public class FinancialAdvisorCondition : BaseCondition
		{
			internal FinancialAdvisorCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Group.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.GroupKey)]
			[DescriptionLoc(LocalizedStrings.GroupKey, true)]
			public string Group
			{
				get { return TryGetValue<string>("FAGroup"); }
				set { SetValue("FAGroup", value); }
			}

			/// <summary>
			/// Profile.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ProfileKey)]
			[DescriptionLoc(LocalizedStrings.ProfileKey, true)]
			public string Profile
			{
				get { return TryGetValue<string>("FAProfile"); }
				set { SetValue("FAProfile", value); }
			}

			/// <summary>
			/// Calculation method.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2349Key)]
			[DescriptionLoc(LocalizedStrings.Str2349Key, true)]
			public FinancialAdvisorAllocations? Allocation
			{
				get { return TryGetValue<FinancialAdvisorAllocations?>("FAAllocation"); }
				set { SetValue("FAMethod", value); }
			}

			/// <summary>
			/// Ration percentage to filled volume.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2351Key)]
			[DescriptionLoc(LocalizedStrings.Str2351Key, true)]
			public string Percentage
			{
				get { return TryGetValue<string>("FAPercentage"); }
				set { SetValue("FAPercentage", value); }
			}
		}

		/// <summary>
		/// Senders.
		/// </summary>
		public enum OrderOrigins
		{
			/// <summary>
			/// Client.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.ClientKey)]
			Customer,

			/// <summary>
			/// Firm.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.FirmKey)]
			Firm
		}

		/// <summary>
		/// Trading.
		/// </summary>
		public enum AuctionStrategies
		{
			/// <summary>
			/// Match.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2355Key)]
			AuctionMatch,

			/// <summary>
			/// Better.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.BetterKey)]
			AuctionImprovement,

			/// <summary>
			/// Transparent.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.TransparentKey)]
			AuctionTransparent
		}

		/// <summary>
		/// Volatility timeframes.
		/// </summary>
		public enum VolatilityTimeFrames
		{
			/// <summary>
			/// Daily.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.DailyKey)]
			Daily = 1,

			/// <summary>
			/// Average annual.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2359Key)]
			Annual = 2
		}

		/// <summary>
		/// The settings for the orders type <see cref="ExtendedOrderTypes.Volatility"/>.
		/// </summary>
		public class VolatilityCondition : BaseCondition
		{
			internal VolatilityCondition(IBOrderCondition condition)
				: base(condition)
			{
				OrderType = OrderTypes.Conditional;
				ConId = 0;
				ContinuousUpdate = false;

				IsShortSale = false;
				ShortSale = new ShortSaleCondition(condition, "DeltaNeutral");
			}

			/// <summary>
			/// Refresh limit price if underlying asset price has changed.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2360Key)]
			[DescriptionLoc(LocalizedStrings.Str2361Key)]
			public bool ContinuousUpdate
			{
				get { return GetValue<bool>("ContinuousUpdate"); }
				set { SetValue("ContinuousUpdate", value); }
			}

			/// <summary>
			/// Average best price or best price.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2362Key)]
			[DescriptionLoc(LocalizedStrings.Str2363Key)]
			public bool? IsAverageBestPrice
			{
				get { return TryGetValue<bool?>("ReferencePriceType"); }
				set { SetValue("ReferencePriceType", value); }
			}

			/// <summary>
			/// Volatility.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.VolatilityKey)]
			[DescriptionLoc(LocalizedStrings.VolatilityKey, true)]
			public decimal? Volatility
			{
				get { return TryGetValue<decimal?>("Volatility"); }
				set { SetValue("Volatility", value); }
			}

			/// <summary>
			/// Volatility time-frame.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2365Key)]
			[DescriptionLoc(LocalizedStrings.Str2366Key)]
			public VolatilityTimeFrames? VolatilityTimeFrame
			{
				get { return TryGetValue<VolatilityTimeFrames?>("VolatilityTimeFrame"); }
				set { SetValue("VolatilityTimeFrame", value); }
			}

			/// <summary>
			/// Order type.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str132Key)]
			[DescriptionLoc(LocalizedStrings.Str132Key, true)]
			public OrderTypes OrderType
			{
				get { return GetValue<OrderTypes>("DeltaNeutralOrderType"); }
				set { SetValue("DeltaNeutralOrderType", value); }
			}

			/// <summary>
			/// Extended type of order.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2368Key)]
			[DescriptionLoc(LocalizedStrings.Str2369Key)]
			public ExtendedOrderTypes? ExtendedOrderType
			{
				get { return TryGetValue<ExtendedOrderTypes?>("DeltaNeutralExtendedOrderType"); }
				set { SetValue("DeltaNeutralExtendedOrderType", value); }
			}

			/// <summary>
			/// Stop-price.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
			[DescriptionLoc(LocalizedStrings.StopPriceKey, true)]
			public decimal? StopPrice
			{
				get { return TryGetValue<decimal?>("DeltaNeutralAuxPrice"); }
				set { SetValue("DeltaNeutralAuxPrice", value); }
			}

			/// <summary>
			/// </summary>
			[DisplayName("ConId")]
			[Description("ConId.")]
			public int ConId
			{
				get { return GetValue<int>("DeltaNeutralConId"); }
				set { SetValue("DeltaNeutralConId", value); }
			}

			/// <summary>
			/// Firm.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.FirmKey)]
			[DescriptionLoc(LocalizedStrings.FirmKey, true)]
			public string SettlingFirm
			{
				get { return TryGetValue<string>("DeltaNeutralSettlingFirm"); }
				set { SetValue("DeltaNeutralSettlingFirm", value); }
			}

			/// <summary>
			/// Clearing account.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2372Key)]
			[DescriptionLoc(LocalizedStrings.Str2372Key, true)]
			public string ClearingPortfolio
			{
				get { return TryGetValue<string>("DeltaNeutralClearingPortfolio"); }
				set { SetValue("DeltaNeutralClearingPortfolio", value); }
			}

			/// <summary>
			/// Clearing chain.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2374Key)]
			[DescriptionLoc(LocalizedStrings.Str2374Key, true)]
			public string ClearingIntent
			{
				get { return TryGetValue<string>("DeltaNeutralClearingIntent"); }
				set { SetValue("DeltaNeutralClearingIntent", value); }
			}

			/// <summary>
			/// Is the order a short sell.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2376Key)]
			[DescriptionLoc(LocalizedStrings.Str2377Key)]
			public bool IsShortSale
			{
				get { return GetValue<bool>("DeltaNeutralOpenClose"); }
				set { SetValue("DeltaNeutralOpenClose", value); }
			}

			/// <summary>
			/// Condition for short sales of combined legs.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2378Key)]
			[DescriptionLoc(LocalizedStrings.Str2379Key)]
			public ShortSaleCondition ShortSale { get; private set; }
		}

		/// <summary>
		/// Short sales types of combined legs.
		/// </summary>
		public enum ShortSaleSlots
		{
			/// <summary>
			/// Private trader or not short leg.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str1658Key)]
			Unapplicable,

			/// <summary>
			/// Clearing broker.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.ClearingKey)]
			ClearingBroker,

			/// <summary>
			/// Other.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2381Key)]
			ThirdParty
		}

		/// <summary>
		/// Condition for short sales of combined legs.
		/// </summary>
		public class ShortSaleCondition : BaseCondition
		{
			private readonly string _prefix;

			internal ShortSaleCondition(IBOrderCondition condition, string prefix)
				: base(condition)
			{
				if (prefix == null)
					throw new ArgumentNullException("prefix");

				_prefix = prefix;

				Slot = ShortSaleSlots.Unapplicable;
				ExemptCode = 0;
			}

			/// <summary>
			/// Short sale type of combined legs.
			/// </summary>
			public ShortSaleSlots Slot
			{
				get { return GetValue<ShortSaleSlots>(_prefix + "ShortSaleSlot"); }
				set { SetValue(_prefix + "ShortSaleSlot", value); }
			}

			/// <summary>
			/// Clarification of the short sale type of combined legs.
			/// </summary>
			/// <remarks>
			/// Used when <see cref="ShortSaleCondition.Slot"/> equals to <see cref="ShortSaleSlots.ThirdParty"/>.
			/// </remarks>
			public string Location
			{
				get { return TryGetValue<string>(_prefix + "ShortSaleSlotLocation"); }
				set { SetValue(_prefix + "ShortSaleSlotLocation", value); }
			}

			/// <summary>
			/// Exempt Code for Short Sale Exemption Orders.
			/// </summary>
			public int ExemptCode
			{
				get { return GetValue<int>(_prefix + "ExemptCode"); }
				set { SetValue(_prefix + "ExemptCode", value); }
			}

			/// <summary>
			/// Is the order opening or closing.
			/// </summary>
			public bool? IsOpenOrClose
			{
				get { return TryGetValue<bool?>(_prefix + "OpenClose"); }
				set { SetValue(_prefix + "OpenClose", value); }
			}
		}

		/// <summary>
		/// EFP orders settings.
		/// </summary>
		public class ComboCondition : BaseCondition
		{
			internal ComboCondition(IBOrderCondition condition)
				: base(condition)
			{
				BasisPoints = 0;
				BasisPointsType = 0;
			}

			/// <summary>
			/// Basic points.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2382Key)]
			[DescriptionLoc(LocalizedStrings.Str2382Key, true)]
			public decimal BasisPoints
			{
				get { return GetValue<decimal>("ComboBasisPoints"); }
				set { SetValue("ComboBasisPoints", value); }
			}

			/// <summary>
			/// Base points type.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2382Key)]
			[DescriptionLoc(LocalizedStrings.Str2382Key, true)]
			public int BasisPointsType
			{
				get { return GetValue<int>("ComboBasisPointsType"); }
				set { SetValue("ComboBasisPointsType", value); }
			}

			/// <summary>
			/// Legs description.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2386Key)]
			[DescriptionLoc(LocalizedStrings.Str2386Key, true)]
			public string LegsDescription
			{
				get { return TryGetValue<string>("ComboLegsDescription"); }
				set { SetValue("ComboLegsDescription", value); }
			}

			/// <summary>
			/// Legs prices.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2388Key)]
			[DescriptionLoc(LocalizedStrings.Str2388Key, true)]
			public IEnumerable<decimal?> Legs
			{
				get { return TryGetValue<IEnumerable<decimal?>>("ComboLegs"); }
				set { SetValue("ComboLegs", value); }
			}

			/// <summary>
			/// Condition for short sales.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2378Key)]
			[DescriptionLoc(LocalizedStrings.Str2390Key)]
			public IDictionary<SecurityId, ShortSaleCondition> ShortSales
			{
				get { return TryGetValue<IDictionary<SecurityId, ShortSaleCondition>>("ShortSales"); }
				set { SetValue("ShortSales", value); }
			}
		}

		/// <summary>
		/// Settings for orders that are sent to the Smart exchange.
		/// </summary>
		public class SmartRoutingCondition : BaseCondition
		{
			internal SmartRoutingCondition(IBOrderCondition condition)
				: base(condition)
			{
				DiscretionaryAmount = 0;
				ETradeOnly = false;
				FirmQuoteOnly = false;
				NotHeld = false;
				OptOutSmartRouting = false;
			}

			/// <summary>
			/// Order price shift range.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2391Key)]
			[DescriptionLoc(LocalizedStrings.Str2392Key)]
			public decimal DiscretionaryAmount
			{
				get { return GetValue<decimal>("DiscretionaryAmount"); }
				set { SetValue("DiscretionaryAmount", value); }
			}

			/// <summary>
			/// Electronic trading.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ElectronicTradingKey)]
			[DescriptionLoc(LocalizedStrings.ElectronicTradingKey, true)]
			public bool ETradeOnly
			{
				get { return GetValue<bool>("ETradeOnly"); }
				set { SetValue("ETradeOnly", value); }
			}

			/// <summary>
			/// Company quotes.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2395Key)]
			[DescriptionLoc(LocalizedStrings.Str2395Key, true)]
			public bool FirmQuoteOnly
			{
				get { return GetValue<bool>("FirmQuoteOnly"); }
				set { SetValue("FirmQuoteOnly", value); }
			}

			/// <summary>
			/// Maximum offset from best pairs.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2397Key)]
			[DescriptionLoc(LocalizedStrings.Str2398Key)]
			public decimal? NbboPriceCap
			{
				get { return TryGetValue<decimal?>("NbboPriceCap"); }
				set { SetValue("NbboPriceCap", value); }
			}

			/// <summary>
			/// Keep in market depth.
			/// </summary>
			/// <remarks>
			/// Only for the IBDARK exchange.
			/// </remarks>
			[DisplayNameLoc(LocalizedStrings.Str2399Key)]
			[DescriptionLoc(LocalizedStrings.Str2400Key)]
			public bool NotHeld
			{
				get { return GetValue<bool>("NotHeld"); }
				set { SetValue("NotHeld", value); }
			}

			/// <summary>
			/// Direct sending of ASX orders.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2401Key)]
			[DescriptionLoc(LocalizedStrings.Str2402Key)]
			public bool OptOutSmartRouting
			{
				get { return GetValue<bool>("OptOutSmartRouting"); }
				set { SetValue("OptOutSmartRouting", value); }
			}

			/// <summary>
			/// Parameters.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str225Key)]
			[DescriptionLoc(LocalizedStrings.Str2403Key)]
			public IEnumerable<Tuple<string, string>> ComboParams
			{
				get { return TryGetValue<IEnumerable<Tuple<string, string>>>("SmartComboRoutingParams") ?? Enumerable.Empty<Tuple<string, string>>(); }
				set { SetValue("SmartComboRoutingParams", value); }
			}
		}

		/// <summary>
		/// Condition for order being changed.
		/// </summary>
		public class ScaleCondition : BaseCondition
		{
			internal ScaleCondition(IBOrderCondition condition)
				: base(condition)
			{
				PriceAdjustInterval = 0;
				AutoReset = false;
				RandomPercent = false;
			}

			/// <summary>
			/// split order into X buckets.
			/// </summary>
			public int? InitLevelSize
			{
				get { return TryGetValue<int?>("ScaleInitLevelSize"); }
				set { SetValue("ScaleInitLevelSize", value); }
			}

			/// <summary>
			/// split order so each bucket is of the size X.
			/// </summary>
			public int? SubsLevelSize
			{
				get { return TryGetValue<int?>("ScaleSubsLevelSize"); }
				set { SetValue("ScaleSubsLevelSize", value); }
			}

			/// <summary>
			/// price increment per bucket.
			/// </summary>
			public decimal? PriceIncrement
			{
				get { return TryGetValue<decimal?>("ScalePriceIncrement"); }
				set { SetValue("ScalePriceIncrement", value); }
			}

			/// <summary>
			/// </summary>
			public decimal? PriceAdjustValue
			{
				get { return TryGetValue<decimal?>("ScalePriceAdjustValue"); }
				set { SetValue("ScalePriceAdjustValue", value); }
			}

			/// <summary>
			/// </summary>
			public int PriceAdjustInterval
			{
				get { return GetValue<int>("ScalePriceAdjustInterval"); }
				set { SetValue("ScalePriceAdjustInterval", value); }
			}

			/// <summary>
			/// </summary>
			public decimal? ProfitOffset
			{
				get { return TryGetValue<decimal?>("ScaleProfitOffset"); }
				set { SetValue("ScaleProfitOffset", value); }
			}

			/// <summary>
			/// </summary>
			public bool AutoReset
			{
				get { return GetValue<bool>("ScaleAutoReset"); }
				set { SetValue("ScaleAutoReset", value); }
			}

			/// <summary>
			/// </summary>
			public int? InitPosition
			{
				get { return TryGetValue<int?>("ScaleInitPosition"); }
				set { SetValue("ScaleInitPosition", value); }
			}

			/// <summary>
			/// </summary>
			public int? InitFillQty
			{
				get { return TryGetValue<int?>("ScaleInitFillQty"); }
				set { SetValue("ScaleInitFillQty", value); }
			}

			/// <summary>
			/// </summary>
			public bool RandomPercent
			{
				get { return GetValue<bool>("ScaleRandomPercent"); }
				set { SetValue("ScaleRandomPercent", value); }
			}

			/// <summary>
			/// </summary>
			public string Table
			{
				get { return TryGetValue<string>("Table"); }
				set { SetValue("Table", value); }
			}
		}

		/// <summary>
		/// Parameters types for hedging.
		/// </summary>
		public enum HedgeTypes
		{
			/// <summary>
			/// Delta.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.DeltaKey)]
			Delta,

			/// <summary>
			/// Beta.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.BetaKey)]
			Beta,

			/// <summary>
			/// Currency.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.CurrencyKey)]
			FX,

			/// <summary>
			/// Pair.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.PairKey)]
			Pair
		}

		/// <summary>
		/// Condition for hedge-orders.
		/// </summary>
		public class HedgeCondition : BaseCondition
		{
			internal HedgeCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Parameter type for hedging.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ParameterTypeKey)]
			[DescriptionLoc(LocalizedStrings.Str2406Key)]
			public HedgeTypes? Type
			{
				get { return TryGetValue<HedgeTypes?>("HedgeType"); }
				set { SetValue("HedgeType", value); }
			}

			/// <summary>
			/// Parameter.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ParameterKey)]
			[DescriptionLoc(LocalizedStrings.ParameterKey, true)]
			public string Param
			{
				get { return TryGetValue<string>("HedgeParam"); }
				set { SetValue("HedgeParam", value); }
			} 
		}

		/// <summary>
		/// Condition for algo-orders.
		/// </summary>
		public class AlgoCondition : BaseCondition
		{
			internal AlgoCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Strategy.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.StrategyKey)]
			[DescriptionLoc(LocalizedStrings.StrategyKey, true)]
			public string Strategy
			{
				get { return TryGetValue<string>("AlgoStrategy"); }
				set { SetValue("AlgoStrategy", value); }
			}

			/// <summary>
			/// Parameters.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str225Key)]
			[DescriptionLoc(LocalizedStrings.Str2403Key)]
			public IEnumerable<Tuple<string, string>> Params
			{
				get { return TryGetValue<IEnumerable<Tuple<string, string>>>("AlgoParams") ?? Enumerable.Empty<Tuple<string, string>>(); }
				set { SetValue("AlgoParams", value); }
			}
		}

		/// <summary>
		/// Clearing objectives.
		/// </summary>
		public enum ClearingIntents
		{
			/// <summary>
			/// Broker.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.BrokerKey)]
			Broker,

			/// <summary>
			/// Other.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2381Key)]
			Away,

			/// <summary>
			/// Post-trading placement.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2411Key)]
			PostTradeAllocation
		}

		/// <summary>
		/// Condition for clearing information.
		/// </summary>
		/// <remarks>
		/// Only for institutional clients.
		/// </remarks>
		public class ClearingCondition : BaseCondition
		{
			internal ClearingCondition(IBOrderCondition condition)
				: base(condition)
			{
				Intent = ClearingIntents.Broker;
			}

			/// <summary>
			/// Account.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.AccountKey)]
			[DescriptionLoc(LocalizedStrings.AccountKey, true)]
			public string Portfolio
			{
				get { return TryGetValue<string>("Portfolio"); }
				set { SetValue("Portfolio", value); }
			}

			/// <summary>
			/// Firm.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.FirmKey)]
			[DescriptionLoc(LocalizedStrings.FirmKey, true)]
			public string SettlingFirm
			{
				get { return TryGetValue<string>("SettlingFirm"); }
				set { SetValue("SettlingFirm", value); }
			}

			/// <summary>
			/// Clearing account.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2414Key)]
			[DescriptionLoc(LocalizedStrings.Str2372Key, true)]
			public string ClearingPortfolio
			{
				get { return TryGetValue<string>("ClearingPortfolio"); }
				set { SetValue("ClearingPortfolio", value); }
			}

			/// <summary>
			/// Aim of clearing.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2415Key)]
			[DescriptionLoc(LocalizedStrings.Str2416Key)]
			public ClearingIntents Intent
			{
				get { return GetValue<ClearingIntents>("ClearingIntent"); }
				set { SetValue("ClearingIntent", value); }
			}
		}

		/// <summary>
		/// Order condition <see cref="OrderTypes.Execute"/>.
		/// </summary>
		public class OptionExerciseCondition : BaseCondition
		{
			internal OptionExerciseCondition(IBOrderCondition condition)
				: base(condition)
			{
				IsExercise = true;
				IsOverride = false;
			}

			/// <summary>
			/// Exercise the option.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2417Key)]
			[DescriptionLoc(LocalizedStrings.Str2418Key)]
			public bool IsExercise
			{
				get { return GetValue<bool>("OptionIsExercise"); }
				set { SetValue("OptionIsExercise", value); }
			}

			/// <summary>
			/// Replace action.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2419Key)]
			[DescriptionLoc(LocalizedStrings.Str2420Key)]
			public bool IsOverride
			{
				get { return GetValue<bool>("OptionIsOverride"); }
				set { SetValue("OptionIsOverride", value); }
			} 
		}

		/// <summary>
		/// The condition for GTC orders.
		/// </summary>
		public class ActiveCondition : BaseCondition
		{
			internal ActiveCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Start time.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2421Key)]
			[DescriptionLoc(LocalizedStrings.Str2422Key)]
			public DateTimeOffset? Start
			{
				get { return TryGetValue<DateTimeOffset?>("Start"); }
				set { SetValue("Start", value); }
			}

			/// <summary>
			/// Ending time.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str242Key)]
			[DescriptionLoc(LocalizedStrings.Str727Key, true)]
			public DateTimeOffset? Stop
			{
				get { return TryGetValue<DateTimeOffset?>("Stop"); }
				set { SetValue("Stop", value); }
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IBOrderCondition"/>.
		/// </summary>
		public IBOrderCondition()
		{
			StopPrice = 0;
			IsMarketOnOpen = false;
			Oca = new OcaCondition(this);
			Transmit = true;
			BlockOrder = false;
			SweepToFill = false;
			TriggerMethod = TriggerMethods.Default;
			OutsideRth = false;
			Hidden = false;
			OverridePercentageConstraints = false;
			AllOrNone = false;
			FinancialAdvisor = new FinancialAdvisorCondition(this);
			IsOpenOrClose = true;
			Origin = OrderOrigins.Customer;
			StockRangeLower = 0;
			StockRangeUpper = 0;
			Volatility = new VolatilityCondition(this);
			SmartRouting = new SmartRoutingCondition(this);
			Combo = new ComboCondition(this);
			Scale = new ScaleCondition(this);
			WhatIf = false;
			Hedge = new HedgeCondition(this);
			Algo = new AlgoCondition(this);
			Clearing = new ClearingCondition(this);
			ShortSale = new ShortSaleCondition(this, string.Empty);
			OptionExercise = new OptionExerciseCondition(this);
			Active = new ActiveCondition(this);
		}

		/// <summary>
		/// Extended condition.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2424Key)]
		[DescriptionLoc(LocalizedStrings.Str2425Key)]
		public ExtendedOrderTypes? ExtendedType
		{
			get { return (ExtendedOrderTypes?)Parameters.TryGetValue("ExtendedType"); }
			set { Parameters["ExtendedType"] = value; }
		}

		/// <summary>
		/// Stop-price.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str1693Key)]
		public decimal StopPrice
		{
			get { return (decimal)Parameters["StopPrice"]; }
			set { Parameters["StopPrice"] = value; }
		}

		/// <summary>
		/// At trading opening.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2426Key)]
		[DescriptionLoc(LocalizedStrings.Str2427Key)]
		public bool IsMarketOnOpen
		{
			get { return (bool)Parameters["IsMarketOnOpen"]; }
			set { Parameters["IsMarketOnOpen"] = value; }
		}

		/// <summary>
		/// OCA (One-Cancels All) settings.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2428Key)]
		[DescriptionLoc(LocalizedStrings.Str2429Key)]
		[ExpandableObject]
		public OcaCondition Oca { get; private set; }

		/// <summary>
		/// Send order in TWS.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2430Key)]
		[DescriptionLoc(LocalizedStrings.Str2431Key)]
		public bool Transmit
		{
			get { return (bool)Parameters["Transmit"]; }
			set { Parameters["Transmit"] = value; }
		}

		/// <summary>
		/// Parent order ID.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2432Key)]
		[DescriptionLoc(LocalizedStrings.Str2433Key)]
		public int? ParentId
		{
			get { return (int?)Parameters.TryGetValue("ParentId"); }
			set { Parameters["ParentId"] = value; }
		}

		/// <summary>
		/// Split order volume.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2434Key)]
		[DescriptionLoc(LocalizedStrings.Str2435Key)]
		public bool BlockOrder
		{
			get { return (bool)Parameters["BlockOrder"]; }
			set { Parameters["BlockOrder"] = value; }
		}

		/// <summary>
		/// At best price.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2314Key)]
		[DescriptionLoc(LocalizedStrings.Str2436Key)]
		public bool SweepToFill
		{
			get { return (bool)Parameters["SweepToFill"]; }
			set { Parameters["SweepToFill"] = value; }
		}

		/// <summary>
		/// Stop-order activation condition.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2437Key)]
		[DescriptionLoc(LocalizedStrings.Str2438Key)]
		public TriggerMethods TriggerMethod
		{
			get { return (TriggerMethods)Parameters["TriggerMethod"]; }
			set { Parameters["TriggerMethod"] = value; }
		}

		/// <summary>
		/// Allow to activate a stop-order outside of trading time.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2439Key)]
		[DescriptionLoc(LocalizedStrings.Str2440Key)]
		public bool OutsideRth
		{
			get { return (bool)Parameters["OutsideRth"]; }
			set { Parameters["OutsideRth"] = value; }
		}

		/// <summary>
		/// Hide order in market depth.
		/// </summary>
		/// <remarks>
		/// It is possible only when the order is sending to the ISLAND exchange.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2441Key)]
		[DescriptionLoc(LocalizedStrings.Str2442Key)]
		public bool Hidden
		{
			get { return (bool)Parameters["Hidden"]; }
			set { Parameters["Hidden"] = value; }
		}

		/// <summary>
		/// Activate after given time.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2443Key)]
		[DescriptionLoc(LocalizedStrings.Str2444Key)]
		public DateTimeOffset? GoodAfterTime
		{
			get { return (DateTimeOffset?)Parameters.TryGetValue("GoodAfterTime"); }
			set { Parameters["GoodAfterTime"] = value; }
		}

		/// <summary>
		/// Cancel orders with wrong price.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2445Key)]
		[DescriptionLoc(LocalizedStrings.Str2446Key)]
		public bool OverridePercentageConstraints
		{
			get { return (bool)Parameters["OverridePercentageConstraints"]; }
			set { Parameters["OverridePercentageConstraints"] = value; }
		}

		/// <summary>
		/// Trader ID.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2447Key)]
		[DescriptionLoc(LocalizedStrings.Str2448Key)]
		public AgentDescriptions? Agent
		{
			get { return (AgentDescriptions?)Parameters.TryGetValue("Rule80A"); }
			set { Parameters["Rule80A"] = value; }
		}

		/// <summary>
		/// Wait for required volume to appear.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2449Key)]
		[DescriptionLoc(LocalizedStrings.Str2450Key)]
		public bool AllOrNone
		{
			get { return (bool)Parameters["AllOrNone"]; }
			set { Parameters["AllOrNone"] = value; }
		}

		/// <summary>
		/// Minimum order volume.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2451Key)]
		[DescriptionLoc(LocalizedStrings.Str2452Key)]
		public int? MinVolume
		{
			get { return (int?)Parameters.TryGetValue("MinVolume"); }
			set { Parameters["MinQty"] = value; }
		}

		/// <summary>
		/// The shift in the price for the order type <see cref="ExtendedOrderTypes.Relative"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2453Key)]
		[DescriptionLoc(LocalizedStrings.Str2454Key)]
		public decimal? PercentOffset
		{
			get { return (decimal?)Parameters.TryGetValue("PercentOffset"); }
			set { Parameters["PercentOffset"] = value; }
		}

		/// <summary>
		/// Moving stop activation price.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2455Key)]
		[DescriptionLoc(LocalizedStrings.Str2456Key)]
		public decimal? TrailStopPrice
		{
			get { return (decimal?)Parameters.TryGetValue("TrailStopPrice"); }
			set { Parameters["TrailStopPrice"] = value; }
		}

		/// <summary>
		/// Trailing stop volume вas percentage.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2457Key)]
		[DescriptionLoc(LocalizedStrings.Str2458Key)]
		public decimal? TrailStopVolumePercentage
		{
			get { return (decimal?)Parameters.TryGetValue("TrailStopVolumePercentage"); }
			set { Parameters["TrailStopVolumePercentage"] = value; }
		}

		/// <summary>
		/// Settings for automatic order volume calculation.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2459Key)]
		[DescriptionLoc(LocalizedStrings.Str2460Key)]
		[ExpandableObject]
		public FinancialAdvisorCondition FinancialAdvisor { get; private set; }

		/// <summary>
		/// Is the order opening or closing.
		/// </summary>
		/// <remarks>
		/// Only for institutional clients.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2461Key)]
		[DescriptionLoc(LocalizedStrings.Str2462Key)]
		public bool IsOpenOrClose
		{
			get { return (bool)Parameters["OpenClose"]; }
			set { Parameters["OpenClose"] = value; }
		}

		/// <summary>
		/// Sender.
		/// </summary>
		/// <remarks>
		/// Only for institutional clients.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1664Key)]
		[DescriptionLoc(LocalizedStrings.Str2463Key)]
		public OrderOrigins Origin
		{
			get { return (OrderOrigins)Parameters["Origin"]; }
			set { Parameters["Origin"] = value; }
		}

		/// <summary>
		/// Condition for short sales of combined legs.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2378Key)]
		[DescriptionLoc(LocalizedStrings.Str2379Key)]
		[ExpandableObject]
		public ShortSaleCondition ShortSale { get; private set; }

		/// <summary>
		/// Trading.
		/// </summary>
		/// <remarks>
		/// Only BOX board.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2464Key)]
		[DescriptionLoc(LocalizedStrings.Str2465Key)]
		public AuctionStrategies? AuctionStrategy
		{
			get { return (AuctionStrategies?)Parameters.TryGetValue("AuctionStrategy"); }
			set { Parameters["AuctionStrategy"] = value; }
		}

		/// <summary>
		/// Starting price.
		/// </summary>
		/// <remarks>
		/// Only BOX board.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2466Key)]
		[DescriptionLoc(LocalizedStrings.Str2467Key)]
		public decimal? StartingPrice
		{
			get { return (decimal?)Parameters.TryGetValue("StartingPrice"); }
			set { Parameters["StartingPrice"] = value; }
		}

		/// <summary>
		/// Underlying asset price.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2468Key)]
		[DescriptionLoc(LocalizedStrings.Str2469Key)]
		public decimal? StockRefPrice
		{
			get { return (decimal?)Parameters.TryGetValue("StockRefPrice"); }
			set { Parameters["StockRefPrice"] = value; }
		}

		/// <summary>
		/// Underlying asset delta.
		/// </summary>
		/// <remarks>
		/// Only BOX board.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2470Key)]
		[DescriptionLoc(LocalizedStrings.Str2470Key, true)]
		public decimal? Delta
		{
			get { return (decimal?)Parameters.TryGetValue("Delta"); }
			set { Parameters["Delta"] = value; }
		}

		/// <summary>
		/// Minimum price of underlying asset.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2472Key)]
		[DescriptionLoc(LocalizedStrings.Str2473Key)]
		public decimal StockRangeLower
		{
			get { return (decimal)Parameters["StockRangeLower"]; }
			set { Parameters["StockRangeLower"] = value; }
		}

		/// <summary>
		/// Maximum price of underlying asset.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2474Key)]
		[DescriptionLoc(LocalizedStrings.Str2475Key)]
		public decimal StockRangeUpper
		{
			get { return (decimal)Parameters["StockRangeUpper"]; }
			set { Parameters["StockRangeUpper"] = value; }
		}

		/// <summary>
		/// The settings for the orders type <see cref="ExtendedOrderTypes.Volatility"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2476Key)]
		[DescriptionLoc(LocalizedStrings.Str2477Key)]
		[ExpandableObject]
		public VolatilityCondition Volatility { get; private set; }

		/// <summary>
		/// Settings for orders that are sent to the Smart exchange.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2478Key)]
		[DescriptionLoc(LocalizedStrings.Str2479Key)]
		[ExpandableObject]
		public SmartRoutingCondition SmartRouting { get; private set; }

		/// <summary>
		/// EFP orders settings.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2480Key)]
		[DescriptionLoc(LocalizedStrings.Str2481Key)]
		[ExpandableObject]
		public ComboCondition Combo { get; private set; }

		/// <summary>
		/// Condition for order being changed.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2482Key)]
		[DescriptionLoc(LocalizedStrings.Str2483Key)]
		[ExpandableObject]
		public ScaleCondition Scale { get; private set; }

		/// <summary>
		/// Condition for clearing information.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2484Key)]
		[DescriptionLoc(LocalizedStrings.Str2485Key)]
		[ExpandableObject]
		public ClearingCondition Clearing { get; private set; }

		/// <summary>
		/// Condition for algo-orders.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2486Key)]
		[DescriptionLoc(LocalizedStrings.Str2487Key)]
		[ExpandableObject]
		public AlgoCondition Algo { get; private set; }

		/// <summary>
		/// For order return information about commission and margin.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2488Key)]
		[DescriptionLoc(LocalizedStrings.Str2489Key)]
		public bool WhatIf
		{
			get { return (bool)Parameters["WhatIf"]; }
			set { Parameters["WhatIf"] = value; }
		}

		/// <summary>
		/// Algorithm ID.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2490Key)]
		[DescriptionLoc(LocalizedStrings.Str2491Key)]
		public string AlgoId
		{
			get { return (string)Parameters.TryGetValue("AlgoId"); }
			set { Parameters["AlgoId"] = value; }
		}

		/// <summary>
		/// Additional parameters.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str225Key)]
		[DescriptionLoc(LocalizedStrings.Str2492Key)]
		public IEnumerable<Tuple<string, string>> MiscOptions
		{
			get { return (IEnumerable<Tuple<string, string>>)Parameters.TryGetValue("MiscOptions") ?? Enumerable.Empty<Tuple<string, string>>(); }
			set { Parameters["MiscOptions"] = value; }
		}

		/// <summary>
		/// Solicited.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.SolicitedKey)]
		[DescriptionLoc(LocalizedStrings.SolicitedKey, true)]
		public bool Solicited
		{
			get { return (bool)Parameters["Solicited"]; }
			set { Parameters["Solicited"] = value; }
		}

		/// <summary>
		/// Randomize size.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.RandomizeSizeKey)]
		[DescriptionLoc(LocalizedStrings.RandomizeSizeKey, true)]
		public bool RandomizeSize
		{
			get { return (bool)Parameters["RandomizeSize"]; }
			set { Parameters["RandomizeSize"] = value; }
		}

		/// <summary>
		/// Randomize price books.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.RandomizePriceKey)]
		[DescriptionLoc(LocalizedStrings.RandomizePriceKey, true)]
		public bool RandomizePrice
		{
			get { return (bool)Parameters["RandomizePrice"]; }
			set { Parameters["RandomizePrice"] = value; }
		}

		/// <summary>
		/// Condition for hedge-orders.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2493Key)]
		[DescriptionLoc(LocalizedStrings.Str2494Key)]
		[ExpandableObject]
		public HedgeCondition Hedge { get; private set; }

		/// <summary>
		/// Order condition <see cref="OrderTypes.Execute"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2495Key)]
		[DescriptionLoc(LocalizedStrings.Str2496Key)]
		[ExpandableObject]
		public OptionExerciseCondition OptionExercise { get; private set; }

		/// <summary>
		/// Condition for GTC orders.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2497Key)]
		[DescriptionLoc(LocalizedStrings.Str2498Key)]
		[ExpandableObject]
		public ActiveCondition Active { get; private set; }
	}
}