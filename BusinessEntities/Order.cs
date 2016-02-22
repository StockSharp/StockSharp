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
	[CategoryOrderLoc(MainCategoryAttribute.NameKey, 0)]
	[CategoryOrderLoc(StatisticsCategoryAttribute.NameKey, 1)]
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
		[DisplayNameLoc(LocalizedStrings.Str517Key)]
		[DescriptionLoc(LocalizedStrings.Str518Key)]
		[StatisticsCategory]
		[Nullable]
		public TimeSpan? LatencyRegistration
		{
			get { return _latencyRegistration; }
			set
			{
				if (_latencyRegistration == value)
					return;

				_latencyRegistration = value;
				NotifyChanged(nameof(LatencyRegistration));
			}
		}

		private TimeSpan? _latencyCancellation;

		/// <summary>
		/// Time taken to cancel an order.
		/// </summary>
		[TimeSpan]
		[DisplayNameLoc(LocalizedStrings.Str519Key)]
		[DescriptionLoc(LocalizedStrings.Str520Key)]
		[StatisticsCategory]
		[Nullable]
		public TimeSpan? LatencyCancellation
		{
			get { return _latencyCancellation; }
			set
			{
				if (_latencyCancellation == value)
					return;

				_latencyCancellation = value;
				NotifyChanged(nameof(LatencyCancellation));
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
			get { return _id; }
			set
			{
				if (_id == value)
					return;

				_id = value;
				NotifyChanged(nameof(Id));
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
			get { return _stringId; }
			set
			{
				_stringId = value;
				NotifyChanged(nameof(StringId));
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
			get { return _boardId; }
			set
			{
				_boardId = value;
				NotifyChanged(nameof(BoardId));
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
			get { return _time; }
			set
			{
				if (_time == value)
					return;

				_time = value;
				NotifyChanged(nameof(Time));

				if (LastChangeTime.IsDefault())
					LastChangeTime = value;
			}
		}

		/// <summary>
		/// Transaction ID. Automatically set when the <see cref="IConnector.RegisterOrder"/> method called.
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
			get { return _state; }
			set
			{
				if (_state == value)
					return;

				_state = value;
				NotifyChanged(nameof(State));
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

		private readonly Lazy<SynchronizedList<string>> _messages = new Lazy<SynchronizedList<string>>(() => new SynchronizedList<string>());

		/// <summary>
		/// Messages for order (created by the trading system when registered, changed or cancelled).
		/// </summary>
		[XmlIgnore]
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str526Key)]
		[DescriptionLoc(LocalizedStrings.Str527Key)]
		[MainCategory]
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
			get { return _lastChangeTime; }
			set
			{
				if (_lastChangeTime == value)
					return;

				_lastChangeTime = value;
				NotifyChanged(nameof(LastChangeTime));
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
			get { return _localTime; }
			set
			{
				if (_localTime == value)
					return;

				_localTime = value;
				NotifyChanged(nameof(LocalTime));
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

		private decimal _volume;

		/// <summary>
		/// Number of contracts in an order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.OrderVolumeKey)]
		[MainCategory]
		public decimal Volume
		{
			get { return _volume; }
			set
			{
				_volume = value;
				VisibleVolume = value;
			}
		}

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
		/// Order contracts remainder.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str130Key)]
		[DescriptionLoc(LocalizedStrings.Str131Key)]
		[MainCategory]
		public decimal Balance
		{
			get { return _balance; }
			set
			{
				if (_balance == value)
					return;

				_balance = value;
				NotifyChanged(nameof(Balance));
			}
		}

		private OrderStatus? _status;

		/// <summary>
		/// System order status.
		/// </summary>
		[DataMember]
		[Nullable]
		[Browsable(false)]
		public OrderStatus? Status
		{
			get { return _status; }
			set
			{
				if (_status == value)
					return;

				_status = value;
				NotifyChanged(nameof(Status));
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
			get { return _isSystem; }
			set
			{
				if (_isSystem == value)
					return;

				_isSystem = value;
				NotifyChanged(nameof(IsSystem));
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
		/// If the value is <see cref="DateTimeOffset.MaxValue"/>, then the order is registered until cancel. Otherwise, the period is specified.
		/// </remarks>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str141Key)]
		[DescriptionLoc(LocalizedStrings.Str142Key)]
		[MainCategory]
		public DateTimeOffset? ExpiryDate
		{
			get { return _expiryDate; }
			set
			{
				if (_expiryDate == value)
					return;

				_expiryDate = value;
				NotifyChanged(nameof(ExpiryDate));
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
		[DataMember]
		[InnerSchema]
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str532Key)]
		[DescriptionLoc(LocalizedStrings.Str533Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		public Order DerivedOrder
		{
			get { return _derivedOrder; }
			set
			{
				if (_derivedOrder == value)
					return;

				_derivedOrder = value;
				NotifyChanged(nameof(DerivedOrder));
			}
		}

		/// <summary>
		/// Information for REPO\REPO-M orders.
		/// </summary>
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str233Key)]
		[DescriptionLoc(LocalizedStrings.Str234Key)]
		[MainCategory]
		public RepoOrderInfo RepoInfo { get; set; }

		/// <summary>
		/// Information for Negotiate Deals Mode orders.
		/// </summary>
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str235Key)]
		[DescriptionLoc(LocalizedStrings.Str236Key)]
		[MainCategory]
		public RpsOrderInfo RpsInfo { get; set; }

		[field: NonSerialized]
		private SynchronizedDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended information on order.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the order is stored in the program. For example, the activation time, the yield for usual orders or the condition order ID for a stop order.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set
			{
				_extensionInfo = value.Sync();
				NotifyChanged(nameof(ExtensionInfo));
			}
		}

		/// <summary>
		/// Comission (broker, exchange etc.).
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str160Key)]
		[MainCategory]
		public decimal? Commission { get; set; }

		//[field: NonSerialized]
		//private IConnector _connector;

		///// <summary>
		///// Connection to the trading system, through which this order has been registered.
		///// </summary>
		//[Ignore]
		//[XmlIgnore]
		//[Browsable(false)]
		//[Obsolete("The property Connector was obsoleted and is always null.")]
		//public IConnector Connector
		//{
		//	get { return _connector; }
		//	set
		//	{
		//		if (_connector == value)
		//			return;

		//		_connector = value;

		//		NotifyChanged(nameof(Connector));
		//	}
		//}

		/// <summary>
		/// User's order ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

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
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str534Params
				.Put(TransactionId, Id == null ? StringId : Id.To<string>(), Direction == Sides.Buy ? LocalizedStrings.Str403 : LocalizedStrings.Str404, Price, Volume, State, Balance);
		}
	}
}
