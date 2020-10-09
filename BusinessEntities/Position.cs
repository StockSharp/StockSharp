#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Position.cs
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
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The position by the instrument.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str862Key)]
	[DescriptionLoc(LocalizedStrings.PositionDescKey)]
	public class Position : NotifiableObject, IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Position"/>.
		/// </summary>
		public Position()
		{
		}

		private decimal? _beginValue;

		/// <summary>
		/// Position size at the beginning of the trading session.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str253Key)]
		[DescriptionLoc(LocalizedStrings.Str424Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? BeginValue
		{
			get => _beginValue;
			set
			{
				if (_beginValue == value)
					return;

				_beginValue = value;
				NotifyChanged();
			}
		}

		private decimal? _currentValue;

		/// <summary>
		/// Current position size.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str254Key)]
		[DescriptionLoc(LocalizedStrings.Str425Key)]
		[StatisticsCategory]
		[Nullable]
		//[Browsable(false)]
		public decimal? CurrentValue
		{
			get => _currentValue;
			set
			{
				if (_currentValue == value)
					return;

				_currentValue = value;
				NotifyChanged();
			}
		}

		private decimal? _blockedValue;

		/// <summary>
		/// Position size, registered for active orders.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str255Key)]
		[DescriptionLoc(LocalizedStrings.Str426Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? BlockedValue
		{
			get => _blockedValue;
			set
			{
				if (_blockedValue == value)
					return;

				_blockedValue = value;
				NotifyChanged();
			}
		}

		[field: NonSerialized]
		private IDictionary<string, object> _extensionInfo;

		/// <summary>
		/// Extended information.
		/// </summary>
		/// <remarks>
		/// Required if additional information is stored in the program. For example, the amount of commission paid.
		/// </remarks>
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
				_extensionInfo = value;
				NotifyChanged();
			}
		}

		private decimal? _currentPrice;

		/// <summary>
		/// Position price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str256Key)]
		[DescriptionLoc(LocalizedStrings.Str428Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? CurrentPrice
		{
			get => _currentPrice;
			set
			{
				if (_currentPrice == value)
					return;

				_currentPrice = value;
				NotifyChanged();
			}
		}

		private decimal? _averagePrice;

		/// <summary>
		/// Average price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		[DescriptionLoc(LocalizedStrings.Str429Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? AveragePrice
		{
			get => _averagePrice;
			set
			{
				if (_averagePrice == value)
					return;

				_averagePrice = value;
				NotifyChanged();
			}
		}

		private decimal? _unrealizedPnL;

		/// <summary>
		/// Unrealized profit.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str258Key)]
		[DescriptionLoc(LocalizedStrings.Str430Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? UnrealizedPnL
		{
			get => _unrealizedPnL;
			set
			{
				if (_unrealizedPnL == value)
					return;

				_unrealizedPnL = value;
				NotifyChanged();
			}
		}

		private decimal? _realizedPnL;

		/// <summary>
		/// Realized profit.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str259Key)]
		[DescriptionLoc(LocalizedStrings.Str431Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? RealizedPnL
		{
			get => _realizedPnL;
			set
			{
				if (_realizedPnL == value)
					return;

				_realizedPnL = value;
				NotifyChanged();
			}
		}

		private decimal? _variationMargin;

		/// <summary>
		/// Variation margin.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str260Key)]
		[DescriptionLoc(LocalizedStrings.Str432Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? VariationMargin
		{
			get => _variationMargin;
			set
			{
				if (_variationMargin == value)
					return;

				_variationMargin = value;
				NotifyChanged();
			}
		}

		private decimal? _commission;

		/// <summary>
		/// Total commission.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str433Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? Commission
		{
			get => _commission;
			set
			{
				if (_commission == value)
					return;

				_commission = value;
				NotifyChanged();
			}
		}

		private decimal? _settlementPrice;

		/// <summary>
		/// Settlement price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str312Key)]
		[DescriptionLoc(LocalizedStrings.SettlementPriceKey)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? SettlementPrice
		{
			get => _settlementPrice;
			set
			{
				if (_settlementPrice == value)
					return;

				_settlementPrice = value;
				NotifyChanged();
			}
		}

		private DateTimeOffset _lastChangeTime;

		/// <summary>
		/// Time of last position change.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str434Key)]
		[DescriptionLoc(LocalizedStrings.Str435Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public DateTimeOffset LastChangeTime
		{
			get => _lastChangeTime;
			set
			{
				_lastChangeTime = value;
				NotifyChanged();
			}
		}

		private DateTimeOffset _localTime;

		/// <summary>
		/// Local time of the last position change.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str530Key)]
		[DescriptionLoc(LocalizedStrings.Str530Key, true)]
		[StatisticsCategory]
		[Browsable(false)]
		public DateTimeOffset LocalTime
		{
			get => _localTime;
			set
			{
				_localTime = value;
				NotifyChanged();
			}
		}

		private string _description;

		/// <summary>
		/// Text position description.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				NotifyChanged();
			}
		}

		private CurrencyTypes? _currency;

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency
		{
			get => _currency;
			set
			{
				_currency = value;
				NotifyChanged();
			}
		}

		private DateTimeOffset? _expirationDate;

		/// <summary>
		/// Expiration date.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.ExpiryDateKey)]
		[MainCategory]
		[Nullable]
		public DateTimeOffset? ExpirationDate
		{
			get => _expirationDate;
			set
			{
				_expirationDate = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// Portfolio, in which position is created.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str270Key)]
		[MainCategory]
		public Portfolio Portfolio { get; set; }

		/// <summary>
		/// Security, for which a position was created.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str271Key)]
		[MainCategory]
		public Security Security { get; set; }

		/// <summary>
		/// The depositary where the physical security.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.DepoKey)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		[DataMember]
		public string DepoName { get; set; }

		/// <summary>
		/// Limit type for Т+ market.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str272Key)]
		[DescriptionLoc(LocalizedStrings.Str267Key)]
		[MainCategory]
		[Nullable]
		[DataMember]
		public TPlusLimits? LimitType { get; set; }

		/// <summary>
		/// Strategy id.
		/// </summary>
		[DataMember]
		public string StrategyId { get; set; }

		/// <summary>
		/// Side.
		/// </summary>
		[DataMember]
		public Sides? Side { get; set; }

		private decimal? _leverage;

		/// <summary>
		/// Margin leverage.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LeverageKey)]
		[DescriptionLoc(LocalizedStrings.Str261Key, true)]
		[MainCategory]
		[Nullable]
		public decimal? Leverage
		{
			get => _leverage;
			set
			{
				if (_leverage == value)
					return;

				//if (value < 0)
				//	throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_leverage = value;
				NotifyChanged();
			}
		}

		private decimal? _commissionTaker;

		/// <summary>
		/// Commission (taker).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? CommissionTaker
		{
			get => _commissionTaker;
			set
			{
				_commissionTaker = value;
				NotifyChanged();
			}
		}

		private decimal? _commissionMaker;

		/// <summary>
		/// Commission (maker).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? CommissionMaker
		{
			get => _commissionMaker;
			set
			{
				_commissionMaker = value;
				NotifyChanged();
			}
		}

		private int? _buyOrdersCount;

		/// <summary>
		/// Orders (bids).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public int? BuyOrdersCount
		{
			get => _buyOrdersCount;
			set
			{
				_buyOrdersCount = value;
				NotifyChanged();
			}
		}

		private int? _sellOrdersCount;

		/// <summary>
		/// Orders (asks).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public int? SellOrdersCount
		{
			get => _sellOrdersCount;
			set
			{
				_sellOrdersCount = value;
				NotifyChanged();
			}
		}

		private decimal? _buyOrdersMargin;

		/// <summary>
		/// Margin (buy).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? BuyOrdersMargin
		{
			get => _buyOrdersMargin;
			set
			{
				_buyOrdersMargin = value;
				NotifyChanged();
			}
		}

		private decimal? _sellOrdersMargin;

		/// <summary>
		/// Margin (sell).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? SellOrdersMargin
		{
			get => _sellOrdersMargin;
			set
			{
				_sellOrdersMargin = value;
				NotifyChanged();
			}
		}

		private decimal? _ordersMargin;

		/// <summary>
		/// Orders (margin).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? OrdersMargin
		{
			get => _ordersMargin;
			set
			{
				_ordersMargin = value;
				NotifyChanged();
			}
		}

		private int? _ordersCount;

		/// <summary>
		/// Orders.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public int? OrdersCount
		{
			get => _ordersCount;
			set
			{
				_ordersCount = value;
				NotifyChanged();
			}
		}

		private int? _tradesCount;

		/// <summary>
		/// Trades.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public int? TradesCount
		{
			get => _tradesCount;
			set
			{
				_tradesCount = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Create a copy of <see cref="Position"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public virtual Position Clone()
		{
			var clone = new Position();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// To copy fields of the current position to <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The position in which you should to copy fields.</param>
		public void CopyTo(Position destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			destination.CurrentValue = CurrentValue;
			destination.BeginValue = BeginValue;
			destination.BlockedValue = BlockedValue;
			destination.Commission = Commission;
			destination.VariationMargin = VariationMargin;
			destination.RealizedPnL = RealizedPnL;
			destination.UnrealizedPnL = UnrealizedPnL;
			destination.AveragePrice = AveragePrice;
			destination.CurrentPrice = CurrentPrice;
			destination.SettlementPrice = SettlementPrice;
			destination.Description = Description;
			destination.Currency = Currency;
			destination.ExpirationDate = ExpirationDate;
			destination.ClientCode = ClientCode;
			//destination.LastChangeTime = LastChangeTime;
			//destination.LocalTime = LocalTime;

			destination.Portfolio = Portfolio;
			destination.Security = Security;
			destination.DepoName = DepoName;
			destination.LimitType = LimitType;
			destination.StrategyId = StrategyId;
			destination.Side = Side;

			destination.Leverage = Leverage;
			destination.CommissionMaker = CommissionMaker;
			destination.CommissionTaker = CommissionTaker;

			destination.BuyOrdersCount = BuyOrdersCount;
			destination.SellOrdersCount = SellOrdersCount;
			destination.BuyOrdersMargin = BuyOrdersMargin;
			destination.SellOrdersMargin = SellOrdersMargin;
			destination.OrdersCount = OrdersCount;
			destination.TradesCount = TradesCount;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = $"{Portfolio}-{Security}";

			if (!StrategyId.IsEmpty())
				str += $"-{StrategyId}";

			if (Side != null)
				str += $"-{Side.Value}";

			return str;
		}
	}
}
