#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Order.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Order.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.Str504Key)]
	[DescriptionLoc(LocalizedStrings.Str516Key)]
	public class Order : NotifiableObject, IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Order"/>.
		/// </summary>
		public Order()
		{
		}

		private TimeSpan? _latencyRegistration;

		/// <summary>
		/// Time taken to register an order.
		/// </summary>
		[TimeSpan]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str538Key,
			Description = LocalizedStrings.Str518Key,
			GroupName = LocalizedStrings.Str161Key,
			Order = 1000)]
		[Nullable]
		public TimeSpan? LatencyRegistration
		{
			get => _latencyRegistration;
			set
			{
				if (_latencyRegistration == value)
					return;

				_latencyRegistration = value;
				NotifyChanged();
			}
		}

		private TimeSpan? _latencyCancellation;

		/// <summary>
		/// Time taken to cancel an order.
		/// </summary>
		[TimeSpan]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str537Key,
			Description = LocalizedStrings.Str520Key,
			GroupName = LocalizedStrings.Str161Key,
			Order = 1001)]
		[Nullable]
		public TimeSpan? LatencyCancellation
		{
			get => _latencyCancellation;
			set
			{
				if (_latencyCancellation == value)
					return;

				_latencyCancellation = value;
				NotifyChanged();
			}
		}

		private TimeSpan? _latencyEdition;

		/// <summary>
		/// Time taken to edit an order.
		/// </summary>
		[TimeSpan]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.EditionKey,
			Description = LocalizedStrings.EditionLatencyKey,
			GroupName = LocalizedStrings.Str161Key,
			Order = 1002)]
		[Nullable]
		public TimeSpan? LatencyEdition
		{
			get => _latencyEdition;
			set
			{
				if (_latencyEdition == value)
					return;

				_latencyEdition = value;
				NotifyChanged();
			}
		}

		private long? _id;

		/// <summary>
		/// Order ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.OrderIdStringKey, true)]
		[MainCategory]
		public long? Id
		{
			get => _id;
			set
			{
				if (_id == value)
					return;

				_id = value;
				NotifyChanged();
			}
		}

		private string _stringId;

		/// <summary>
		/// Order ID (as string, if electronic board does not use numeric order ID representation).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str521Key)]
		[DescriptionLoc(LocalizedStrings.OrderIdStringDescKey)]
		[MainCategory]
		public string StringId
		{
			get => _stringId;
			set
			{
				_stringId = value;
				NotifyChanged();
			}
		}

		private string _boardId;

		/// <summary>
		/// Board order id. Uses in case of <see cref="Order.Id"/> and <see cref="Order.StringId"/> is a brokerage system ids.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str117Key)]
		[DescriptionLoc(LocalizedStrings.Str118Key)]
		[MainCategory]
		public string BoardId
		{
			get => _boardId;
			set
			{
				_boardId = value;
				NotifyChanged();
			}
		}

		private DateTimeOffset _time;

		/// <summary>
		/// Order placing time on exchange.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str522Key)]
		[DescriptionLoc(LocalizedStrings.Str523Key)]
		[MainCategory]
		public DateTimeOffset Time
		{
			get => _time;
			set
			{
				if (_time == value)
					return;

				_time = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Transaction ID. Automatically set when the <see cref="ITransactionProvider.RegisterOrder"/> method called.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		[Identity]
		public long TransactionId { get; set; }

		/// <summary>
		/// Security, for which an order is being placed.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str524Key)]
		[MainCategory]
		public Security Security { get; set; }

		private OrderStates _state;

		/// <summary>
		/// Order state.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str134Key)]
		[MainCategory]
		public OrderStates State
		{
			get => _state;
			set
			{
				if (_state == value)
					return;

				_state = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Portfolio, in which the order is being traded.
		/// </summary>
		[DataMember]
		[RelationSingle(IdentityType = typeof(string))]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str525Key)]
		[MainCategory]
		public Portfolio Portfolio { get; set; }

		[field: NonSerialized]
		[Obsolete]
		private readonly Lazy<SynchronizedList<string>> _messages = new Lazy<SynchronizedList<string>>(() => new SynchronizedList<string>());

		/// <summary>
		/// Messages for order (created by the trading system when registered, changed or cancelled).
		/// </summary>
		[XmlIgnore]
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str526Key)]
		[DescriptionLoc(LocalizedStrings.Str527Key)]
		[MainCategory]
		[Obsolete]
		public ISynchronizedCollection<string> Messages => _messages.Value;

		private DateTimeOffset _lastChangeTime;

		/// <summary>
		/// Time of last order change (Cancellation, Fill).
		/// </summary>
		[DataMember]
		//[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str528Key)]
		[DescriptionLoc(LocalizedStrings.Str529Key)]
		[MainCategory]
		public DateTimeOffset LastChangeTime
		{
			get => _lastChangeTime;
			set
			{
				if (_lastChangeTime == value)
					return;

				_lastChangeTime = value;
				NotifyChanged();
			}
		}

		private DateTimeOffset _localTime;

		/// <summary>
		/// Last order change local time (Cancellation, Fill).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str530Key)]
		[DescriptionLoc(LocalizedStrings.Str531Key)]
		[MainCategory]
		public DateTimeOffset LocalTime
		{
			get => _localTime;
			set
			{
				if (_localTime == value)
					return;

				_localTime = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Order price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[MainCategory]
		public decimal Price { get; set; }

		/// <summary>
		/// Number of contracts in the order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.OrderVolumeKey)]
		[MainCategory]
		public decimal Volume { get; set; }

		/// <summary>
		/// Visible quantity of contracts in order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VisibleVolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str127Key)]
		[MainCategory]
		[Nullable]
		public decimal? VisibleVolume { get; set; }

		/// <summary>
		/// Order side (buy or sell).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str129Key)]
		[MainCategory]
		public Sides Direction { get; set; }

		private decimal _balance;

		/// <summary>
		/// Order contracts balance.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str130Key)]
		[DescriptionLoc(LocalizedStrings.Str131Key)]
		[MainCategory]
		public decimal Balance
		{
			get => _balance;
			set
			{
				if (_balance == value)
					return;

				_balance = value;
				NotifyChanged();
			}
		}

		private long? _status;

		/// <summary>
		/// System order status.
		/// </summary>
		[DataMember]
		[Nullable]
		[Browsable(false)]
		public long? Status
		{
			get => _status;
			set
			{
				if (_status == value)
					return;

				_status = value;
				NotifyChanged();
			}
		}

		private bool? _isSystem;

		/// <summary>
		/// Is a system trade.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str139Key)]
		[DescriptionLoc(LocalizedStrings.Str140Key)]
		[MainCategory]
		[Nullable]
		public bool? IsSystem
		{
			get => _isSystem;
			set
			{
				if (_isSystem == value)
					return;

				_isSystem = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Placed order comment.
		/// </summary>
		[DataMember]
		[Primitive]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str136Key)]
		[MainCategory]
		public string Comment { get; set; }

		/// <summary>
		/// Order type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str133Key)]
		[MainCategory]
		public OrderTypes? Type { get; set; }

		private DateTimeOffset? _expiryDate;

		/// <summary>
		/// Order expiry time. The default is <see langword="null" />, which mean (GTC).
		/// </summary>
		/// <remarks>
		/// If the value is <see langword="null"/>, then the order is registered until cancel. Otherwise, the period is specified.
		/// </remarks>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str141Key)]
		[DescriptionLoc(LocalizedStrings.Str142Key)]
		[MainCategory]
		public DateTimeOffset? ExpiryDate
		{
			get => _expiryDate;
			set
			{
				if (_expiryDate == value)
					return;

				_expiryDate = value;
				NotifyChanged();
			}
		}

		//[DataMember]
		//[InnerSchema(IsNullable = true)]
		/// <summary>
		/// Order condition (e.g., stop- and algo- orders parameters).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str155Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		public OrderCondition Condition { get; set; }

		/// <summary>
		/// Limit order time in force.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TimeInForceKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key)]
		[MainCategory]
		[Nullable]
		public TimeInForce? TimeInForce { get; set; }

		private Order _derivedOrder;

		/// <summary>
		/// Exchange order that was created by the stop-order when the condition is activated (<see langword="null" /> if a stop condition has not been activated).
		/// </summary>
		//[DataMember]
		//[InnerSchema]
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str532Key)]
		[DescriptionLoc(LocalizedStrings.Str533Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		[Obsolete("No longer used.")]
		public Order DerivedOrder
		{
			get => _derivedOrder;
			set
			{
				if (_derivedOrder == value)
					return;

				_derivedOrder = value;
				NotifyChanged();
			}
		}

		[field: NonSerialized]
		private SynchronizedDictionary<string, object> _extensionInfo;

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		[Obsolete]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set
			{
				_extensionInfo = value.Sync();
				NotifyChanged();
			}
		}

		/// <summary>
		/// Commission (broker, exchange etc.).
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str160Key)]
		[MainCategory]
		public decimal? Commission { get; set; }

		/// <summary>
		/// Commission currency. Can be <see lnagword="null"/>.
		/// </summary>
		public string CommissionCurrency { get; set; }

		/// <summary>
		/// User's order ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

		/// <summary>
		/// Strategy id.
		/// </summary>
		[DataMember]
		public string StrategyId { get; set; }

		/// <summary>
		/// Broker firm code.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.BrokerKey)]
		[DescriptionLoc(LocalizedStrings.Str2619Key)]
		public string BrokerCode { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str382Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Is the order of market-maker.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketMakerKey)]
		[DescriptionLoc(LocalizedStrings.MarketMakerOrderKey, true)]
		public bool? IsMarketMaker { get; set; }

		/// <summary>
		/// Is margin enabled.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarginKey)]
		[DescriptionLoc(LocalizedStrings.IsMarginKey)]
		public bool? IsMargin { get; set; }

		/// <summary>
		/// Slippage in trade price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str164Key)]
		public decimal? Slippage { get; set; }

		/// <summary>
		/// Is order manual.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ManualKey)]
		[DescriptionLoc(LocalizedStrings.IsOrderManualKey)]
		public bool? IsManual { get; set; }

		/// <summary>
		/// Average execution price.
		/// </summary>
		[DataMember]
		public decimal? AveragePrice { get; set; }

		/// <summary>
		/// Yield.
		/// </summary>
		[DataMember]
		public decimal? Yield { get; set; }

		/// <summary>
		/// Minimum quantity of an order to be executed.
		/// </summary>
		[DataMember]
		public decimal? MinVolume { get; set; }

		/// <summary>
		/// Position effect.
		/// </summary>
		[DataMember]
		public OrderPositionEffects? PositionEffect { get; set; }

		/// <summary>
		/// Post-only order.
		/// </summary>
		[DataMember]
		public bool? PostOnly { get; set; }

		/// <summary>
		/// Sequence number.
		/// </summary>
		/// <remarks>Zero means no information.</remarks>
		[DataMember]
		public long SeqNum { get; set; }

		/// <summary>
		/// Margin leverage.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LeverageKey)]
		[DescriptionLoc(LocalizedStrings.Str261Key)]
		[MainCategory]
		public int? Leverage { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			var str = LocalizedStrings.Str534Params
				.Put(TransactionId, Id == null ? StringId : Id.To<string>(), Security?.Id, Portfolio?.Name, Direction == Sides.Buy ? LocalizedStrings.Str403 : LocalizedStrings.Str404, Price, Volume, State, Balance, Type);

			if (!UserOrderId.IsEmpty())
				str += $" UID={UserOrderId}";

			if (!StrategyId.IsEmpty())
				str += $" Strategy={StrategyId}";

			if (Condition != null)
				str += $" Condition={Condition}";

			if (AveragePrice != null)
				str += $" AvgPrice={AveragePrice}";

			if (MinVolume != null)
				str += $" MinVolume={MinVolume}";

			if (PositionEffect != null)
				str += $" PosEffect={PositionEffect.Value}";

			if (PostOnly != null)
				str += $",PostOnly={PostOnly.Value}";

			if (SeqNum != 0)
				str += $",SeqNum={SeqNum}";

			if (Leverage != null)
				str += $",Leverage={Leverage.Value}";

			return str;
		}
	}
}
