#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Security.cs
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

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Security (shares, futures, options etc.).
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str546Key)]
	[CategoryOrderLoc(MainCategoryAttribute.NameKey, 0)]
	[CategoryOrderLoc(DerivativesCategoryAttribute.NameKey, 1)]
	[CategoryOrderLoc(StatisticsCategoryAttribute.NameKey, 2)]
	public class Security : Cloneable<Security>, IExtendableEntity, INotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Security"/>.
		/// </summary>
		public Security()
		{
		}

		private string _id = string.Empty;

		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[Identity]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		[ReadOnly(true)]
		[PropertyOrder(0)]
		public string Id
		{
			get { return _id; }
			set
			{
				if (_id == value)
					return;

				_id = value;
				Notify("Id");
			}
		}

		private string _name = string.Empty;

		/// <summary>
		/// Security name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str362Key)]
		[MainCategory]
		[PropertyOrder(3)]
		public string Name
		{
			get { return _name; }
			set
			{
				if (_name == value)
					return;

				_name = value;
				Notify("Name");
			}
		}

		private string _shortName = string.Empty;

		/// <summary>
		/// Short security name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str363Key)]
		[DescriptionLoc(LocalizedStrings.Str364Key)]
		[MainCategory]
		public string ShortName
		{
			get { return _shortName; }
			set
			{
				if (_shortName == value)
					return;

				_shortName = value;
				Notify("ShortName");
			}
		}

		private string _code = string.Empty;

		/// <summary>
		/// Security code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CodeKey)]
		[DescriptionLoc(LocalizedStrings.Str349Key, true)]
		[MainCategory]
		[PropertyOrder(1)]
		public string Code
		{
			get { return _code; }
			set
			{
				if (_code == value)
					return;

				_code = value;
				Notify("Code");
			}
		}

		private string _class = string.Empty;

		/// <summary>
		/// Security class.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClassKey)]
		[DescriptionLoc(LocalizedStrings.SecurityClassKey)]
		[MainCategory]
		public string Class
		{
			get { return _class; }
			set
			{
				if (_class == value)
					return;

				_class = value;
				Notify("Class");
			}
		}

		private decimal? _priceStep;

		/// <summary>
		/// Minimum price step.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceStepKey)]
		[DescriptionLoc(LocalizedStrings.MinPriceStepKey)]
		[MainCategory]
		[PropertyOrder(5)]
		[Nullable]
		public decimal? PriceStep
		{
			get { return _priceStep; }
			set
			{
				if (_priceStep == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_priceStep = value;
				Notify("PriceStep");
			}
		}

		private decimal? _volumeStep;

		/// <summary>
		/// Minimum volume step.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeStepKey)]
		[DescriptionLoc(LocalizedStrings.Str366Key)]
		[MainCategory]
		[PropertyOrder(6)]
		[Nullable]
		public decimal? VolumeStep
		{
			get { return _volumeStep; }
			set
			{
				if (_volumeStep == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_volumeStep = value;
				Notify("VolumeStep");
			}
		}

		private decimal? _multiplier;

		/// <summary>
		/// Lot multiplier.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str330Key)]
		[DescriptionLoc(LocalizedStrings.LotVolumeKey)]
		[MainCategory]
		[PropertyOrder(7)]
		[Nullable]
		public decimal? Multiplier
		{
			get { return _multiplier; }
			set
			{
				if (_multiplier == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_multiplier = value;
				Notify("Multiplier");
			}
		}

		private int? _decimals;

		/// <summary>
		/// Number of digits in price after coma.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DecimalsKey)]
		[DescriptionLoc(LocalizedStrings.Str548Key)]
		[MainCategory]
		[PropertyOrder(7)]
		//[ReadOnly(true)]
		[Nullable]
		public int? Decimals
		{
			get { return _decimals; }
			set
			{
				if (_decimals == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_decimals = value;
				Notify("Decimals");
			}
		}

		private SecurityTypes? _type;

		/// <summary>
		/// Security type.
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[MainCategory]
		[PropertyOrder(4)]
		public SecurityTypes? Type
		{
			get { return _type; }
			set
			{
				if (_type == value)
					return;

				_type = value;
				Notify("Type");
			}
		}

		private DateTimeOffset? _expiryDate;

		/// <summary>
		/// Security expiration date (for derivatives - expiration, for bonds — redemption).
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str371Key)]
		[MainCategory]
		public DateTimeOffset? ExpiryDate
		{
			get { return _expiryDate; }
			set
			{
				if (_expiryDate == value)
					return;

				_expiryDate = value;
				Notify("ExpiryDate");
			}
		}

		private DateTimeOffset? _settlementDate;

		/// <summary>
		/// Settlement date for security (for derivatives and bonds).
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.SettlementDateKey)]
		[DescriptionLoc(LocalizedStrings.Str373Key)]
		[MainCategory]
		public DateTimeOffset? SettlementDate
		{
			get { return _settlementDate; }
			set
			{
				if (_settlementDate == value)
					return;

				_settlementDate = value;
				Notify("SettlementDate");
			}
		}

		private ExchangeBoard _board;

		/// <summary>
		/// Exchange board where the security is traded.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.Str549Key)]
		[MainCategory]
		[PropertyOrder(2)]
		public ExchangeBoard Board
		{
			get { return _board; }
			set
			{
				if (_board == value)
					return;

				_board = value;
				Notify("Board");
			}
		}

		private string _underlyingSecurityId = string.Empty;

		/// <summary>
		/// Underlying asset on which the current security is built.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.UnderlyingAssetKey)]
		[DescriptionLoc(LocalizedStrings.Str550Key)]
		[DerivativesCategory]
		[PropertyOrder(2)]
		public string UnderlyingSecurityId
		{
			get { return _underlyingSecurityId; }
			set
			{
				if (_underlyingSecurityId == value)
					return;

				_underlyingSecurityId = value;
				Notify("UnderlyingSecurityCode");
			}
		}

		private OptionTypes? _optionType;

		/// <summary>
		/// Option type.
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str551Key)]
		[DescriptionLoc(LocalizedStrings.OptionContractTypeKey)]
		[DerivativesCategory]
		[PropertyOrder(0)]
		public OptionTypes? OptionType
		{
			get { return _optionType; }
			set
			{
				if (_optionType == value)
					return;

				_optionType = value;
				Notify("OptionType");
			}
		}

		private decimal? _strike;

		/// <summary>
		/// Option strike price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StrikeKey)]
		[DescriptionLoc(LocalizedStrings.OptionStrikePriceKey)]
		[DerivativesCategory]
		[PropertyOrder(1)]
		[Nullable]
		public decimal? Strike
		{
			get { return _strike; }
			set
			{
				if (_strike == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_strike = value;
				Notify("Strike");
			}
		}

		private CurrencyTypes? _currency;

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str382Key)]
		[MainCategory]
		public CurrencyTypes? Currency
		{
			get { return _currency; }
			set
			{
				_currency = value;
				Notify("Currency");
			}
		}

		private string _binaryOptionType;

		/// <summary>
		/// Type of binary option.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str552Key)]
		[DescriptionLoc(LocalizedStrings.TypeBinaryOptionKey)]
		[DerivativesCategory]
		[PropertyOrder(2)]
		public string BinaryOptionType
		{
			get { return _binaryOptionType; }
			set
			{
				if (_binaryOptionType == value)
					return;

				_binaryOptionType = value;
				Notify("BinaryOptionType");
			}
		}

		private SecurityExternalId _externalId;

		/// <summary>
		/// Security ID in other systems.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str553Key)]
		[DescriptionLoc(LocalizedStrings.Str554Key)]
		[MainCategory]
		[ExpandableObject]
		[InnerSchema(NullWhenAllEmpty = false)]
		public SecurityExternalId ExternalId
		{
			get { return _externalId; }
			set
			{
				_externalId = value;
				Notify("ExternalId");
			}
		}

		[field: NonSerialized]
		private SynchronizedDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended security info.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the instrument is stored in the program. For example, the date of instrument expiration (if it is option) or information about the underlying asset if it is the futures contract.
		/// </remarks>
		[XmlIgnore]
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set
			{
				_extensionInfo = value.Sync();
				Notify("ExtensionInfo");
			}
		}

		private decimal? _stepPrice;

		//[DataMember]
		/// <summary>
		/// Step price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str290Key)]
		[DescriptionLoc(LocalizedStrings.Str555Key)]
		[StatisticsCategory]
		[PropertyOrder(0)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? StepPrice
		{
			get { return _stepPrice; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str556);

				if (_stepPrice == value)
					return;

				_stepPrice = value;
				Notify("StepPrice");
			}
		}

		private Trade _lastTrade;

		//[DataMember]
		/// <summary>
		/// Information about the last trade. If during the session on the instrument there were no trades, the value equals to <see langword="null" />.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str289Key)]
		[DescriptionLoc(LocalizedStrings.Str557Key)]
		[ExpandableObject]
		[StatisticsCategory]
		[PropertyOrder(3)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public Trade LastTrade
		{
			get { return _lastTrade; }
			set
			{
				if (_lastTrade == value)
					return;

				_lastTrade = value;
				Notify("LastTrade");

			    if (value == null)
			        return;

				if (!value.Time.IsDefault())
					LastChangeTime = value.Time;
			}
		}

		private decimal? _openPrice;

		//[DataMember]
		/// <summary>
		/// First trade price for the session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str558Key)]
		[DescriptionLoc(LocalizedStrings.Str559Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(11)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? OpenPrice
		{
			get { return _openPrice; }
			set
			{
				if (_openPrice == value)
					return;

				_openPrice = value;
				Notify("OpenPrice");
			}
		}

		private decimal? _closePrice;

		//[DataMember]
		/// <summary>
		/// Last trade price for the previous session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str560Key)]
		[DescriptionLoc(LocalizedStrings.Str561Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(14)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? ClosePrice
		{
			get { return _closePrice; }
			set
			{
				if (_closePrice == value)
					return;

				_closePrice = value;
				Notify("ClosePrice");
			}
		}

		private decimal? _lowPrice;

		//[DataMember]
		/// <summary>
		/// Lowest price for the session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str288Key)]
		[DescriptionLoc(LocalizedStrings.Str562Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(13)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? LowPrice
		{
			get { return _lowPrice; }
			set
			{
				if (_lowPrice == value)
					return;

				_lowPrice = value;
				Notify("LowPrice");
			}
		}

		private decimal? _highPrice;

		//[DataMember]
		/// <summary>
		/// Highest price for the session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str563Key)]
		[DescriptionLoc(LocalizedStrings.Str564Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(12)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? HighPrice
		{
			get { return _highPrice; }
			set
			{
				if (_highPrice == value)
					return;

				_highPrice = value;
				Notify("HighPrice");
			}
		}

		private Quote _bestBid;

		//[DataMember]
		/// <summary>
		/// Best bid in market depth.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str565Key)]
		[DescriptionLoc(LocalizedStrings.Str566Key)]
		[StatisticsCategory]
		[PropertyOrder(1)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public Quote BestBid
		{
			get { return _bestBid; }
			set
			{
				//TODO: решить другим методом, OnEquals не тормозит, медленно работает GUI
				//PYH: Тормозит OnEquals

				//if (_bestBid == value)
				//	return;

				_bestBid = value;
				Notify("BestBid");
			}
		}

		private Quote _bestAsk;

		//[DataMember]
		/// <summary>
		/// Best ask in market depth.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.BestAskKey)]
		[DescriptionLoc(LocalizedStrings.BestAskDescKey)]
		[StatisticsCategory]
		[PropertyOrder(2)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public Quote BestAsk
		{
			get { return _bestAsk; }
			set
			{
				// if (_bestAsk == value)
				//	return;

				_bestAsk = value;
				Notify("BestAsk");
			}
		}

		//[DisplayName("Лучшая пара")]
		//[Description("Лучшая пара котировок.")]
		//[ExpandableObject]
		//[StatisticsCategory]
		/// <summary>
		/// Best pair quotes.
		/// </summary>
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		[DisplayNameLoc(LocalizedStrings.BestPairKey)]
		[DescriptionLoc(LocalizedStrings.BestPairKey, true)]
		public MarketDepthPair BestPair => new MarketDepthPair(this, BestBid, BestAsk);

		private SecurityStates? _state;

		//[DataMember]
		//[Enum]
		/// <summary>
		/// Current state of security.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str569Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public SecurityStates? State
		{
			get { return _state; }
			set
			{
				if (_state == value)
					return;

				_state = value;
				Notify("State");
			}
		}

		private decimal? _minPrice;

		//[DataMember]
		/// <summary>
		/// Lower price limit.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.PriceMinKey)]
		[DescriptionLoc(LocalizedStrings.PriceMinLimitKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? MinPrice
		{
			get { return _minPrice; }
			set
			{
				if (_minPrice == value)
					return;

				_minPrice = value;
				Notify("MinPrice");
			}
		}

		private decimal? _maxPrice;

		//[DataMember]
		/// <summary>
		/// Upper price limit.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.PriceMaxKey)]
		[DescriptionLoc(LocalizedStrings.PriceMaxLimitKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? MaxPrice
		{
			get { return _maxPrice; }
			set
			{
				if (_maxPrice == value)
					return;

				_maxPrice = value;
				Notify("MaxPrice");
			}
		}

		private decimal? _marginBuy;

		//[DataMember]
		/// <summary>
		/// Initial margin to buy.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str304Key)]
		[DescriptionLoc(LocalizedStrings.MarginBuyKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? MarginBuy
		{
			get { return _marginBuy; }
			set
			{
				if (_marginBuy == value)
					return;

				_marginBuy = value;
				Notify("MarginBuy");
			}
		}

		private decimal? _marginSell;

		//[DataMember]
		/// <summary>
		/// Initial margin to sell.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str305Key)]
		[DescriptionLoc(LocalizedStrings.MarginSellKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? MarginSell
		{
			get { return _marginSell; }
			set
			{
				if (_marginSell == value)
					return;

				_marginSell = value;
				Notify("MarginSell");
			}
		}

		[field: NonSerialized]
		private IConnector _connector;

		/// <summary>
		/// Connection to the trading system, through which this instrument has been downloaded.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		[Obsolete("The property Connector was obsoleted and is always null.")]
		public IConnector Connector
		{
			get { return _connector; }
			set
			{
				if (_connector == value)
					return;

				_connector = value;
				Notify("Trader");
			}
		}

		private decimal? _impliedVolatility;

		//[DataMember]
		/// <summary>
		/// Volatility (implied).
		/// </summary>
		[DisplayName("IV")]
		[DescriptionLoc(LocalizedStrings.Str293Key, true)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(9)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? ImpliedVolatility
		{
			get { return _impliedVolatility; }
			set
			{
				if (_impliedVolatility == value)
					return;

				_impliedVolatility = value;
				Notify("ImpliedVolatility");
			}
		}

		private decimal? _historicalVolatility;

		//[DataMember]
		/// <summary>
		/// Volatility (historic).
		/// </summary>
		[DisplayName("HV")]
		[DescriptionLoc(LocalizedStrings.Str299Key, true)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(10)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? HistoricalVolatility
		{
			get { return _historicalVolatility; }
			set
			{
				if (_historicalVolatility == value)
					return;

				_historicalVolatility = value;
				Notify("HistoricalVolatility");
			}
		}

		private decimal? _theorPrice;

		//[DataMember]
		/// <summary>
		/// Theoretical price.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str294Key)]
		[DescriptionLoc(LocalizedStrings.TheoreticalPriceKey)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(7)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? TheorPrice
		{
			get { return _theorPrice; }
			set
			{
				if (_theorPrice == value)
					return;

				_theorPrice = value;
				Notify("TheorPrice");
			}
		}

		private decimal? _delta;

		//[DataMember]
		/// <summary>
		/// Option delta.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.DeltaKey)]
		[DescriptionLoc(LocalizedStrings.OptionDeltaKey)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(3)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? Delta
		{
			get { return _delta; }
			set
			{
				if (_delta == value)
					return;

				_delta = value;
				Notify("Delta");
			}
		}

		private decimal? _gamma;

		//[DataMember]
		/// <summary>
		/// Option gamma.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.GammaKey)]
		[DescriptionLoc(LocalizedStrings.OptionGammaKey)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(4)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? Gamma
		{
			get { return _gamma; }
			set
			{
				if (_gamma == value)
					return;

				_gamma = value;
				Notify("Gamma");
			}
		}

		private decimal? _vega;

		//[DataMember]
		/// <summary>
		/// Option vega.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.VegaKey)]
		[DescriptionLoc(LocalizedStrings.OptionVegaKey)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(5)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? Vega
		{
			get { return _vega; }
			set
			{
				if (_vega == value)
					return;

				_vega = value;
				Notify("Vega");
			}
		}

		private decimal? _theta;

		//[DataMember]
		/// <summary>
		/// Option theta.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.ThetaKey)]
		[DescriptionLoc(LocalizedStrings.OptionThetaKey)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(6)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? Theta
		{
			get { return _theta; }
			set
			{
				if (_theta == value)
					return;

				_theta = value;
				Notify("Theta");
			}
		}

		private decimal? _rho;

		//[DataMember]
		/// <summary>
		/// Option rho.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.RhoKey)]
		[DescriptionLoc(LocalizedStrings.OptionRhoKey)]
		[DerivativesCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(7)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? Rho
		{
			get { return _rho; }
			set
			{
				if (_rho == value)
					return;

				_rho = value;
				Notify("Rho");
			}
		}

		private decimal? _openInterest;

		//[DataMember]
		/// <summary>
		/// Number of open positions (open interest).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str150Key)]
		[DescriptionLoc(LocalizedStrings.Str151Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? OpenInterest
		{
			get { return _openInterest; }
			set
			{
				if (_openInterest == value)
					return;

				_openInterest = value;
				Notify("OpenInterest");
			}
		}

		private DateTimeOffset _localTime;

		/// <summary>
		/// Local time of the last instrument change.
		/// </summary>
		[Browsable(false)]
		[Ignore]
		[XmlIgnore]
		public DateTimeOffset LocalTime
		{
			get { return _localTime; }
			set
			{
				_localTime = value;
				Notify("LocalTime");
			}
		}

		private DateTimeOffset _lastChangeTime;

		//[DisplayName("Изменен")]
		//[Description("Время последнего изменения инструмента.")]
		//[StatisticsCategory]
		/// <summary>
		/// Time of the last instrument change.
		/// </summary>
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		[Ignore]
		[XmlIgnore]
		public DateTimeOffset LastChangeTime
		{
			get { return _lastChangeTime; }
			set
			{
				_lastChangeTime = value;
				Notify("LastChangeTime");
			}
		}

		private decimal? _bidsVolume;

		//[DataMember]
		/// <summary>
		/// Total volume in all buy orders.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str295Key)]
		[DescriptionLoc(LocalizedStrings.BidsVolumeKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(6)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? BidsVolume
		{
			get { return _bidsVolume; }
			set
			{
				_bidsVolume = value;
				Notify("BidsVolume");
			}
		}

		private int? _bidsCount;

		//[DataMember]
		/// <summary>
		/// Number of buy orders.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.BidsKey)]
		[DescriptionLoc(LocalizedStrings.BidsCountKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(8)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public int? BidsCount
		{
			get { return _bidsCount; }
			set
			{
				_bidsCount = value;
				Notify("BidsCount");
			}
		}

		private decimal? _asksVolume;

		//[DataMember]
		/// <summary>
		/// Total volume in all sell orders.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str297Key)]
		[DescriptionLoc(LocalizedStrings.AsksVolumeKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(7)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? AsksVolume
		{
			get { return _asksVolume; }
			set
			{
				_asksVolume = value;
				Notify("AsksVolume");
			}
		}

		private int? _asksCount;

		//[DataMember]
		/// <summary>
		/// Number of sell orders.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.AsksKey)]
		[DescriptionLoc(LocalizedStrings.AsksCountKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(9)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public int? AsksCount
		{
			get { return _asksCount; }
			set
			{
				_asksCount = value;
				Notify("AsksCount");
			}
		}

		private int? _tradesCount;

		//[DataMember]
		/// <summary>
		/// Number of trades.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TradesOfKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key, true)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(10)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public int? TradesCount
		{
			get { return _tradesCount; }
			set
			{
				_tradesCount = value;
				Notify("TradesCount");
			}
		}

		private decimal? _highBidPrice;

		//[DataMember]
		/// <summary>
		/// Maximum bid during the session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str319Key)]
		[DescriptionLoc(LocalizedStrings.Str594Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(4)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? HighBidPrice
		{
			get { return _highBidPrice; }
			set
			{
				_highBidPrice = value;
				Notify("HighBidPrice");
			}
		}

		private decimal? _lowAskPrice;

		//[DataMember]
		/// <summary>
		/// Maximum ask during the session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str320Key)]
		[DescriptionLoc(LocalizedStrings.Str595Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(5)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? LowAskPrice
		{
			get { return _lowAskPrice; }
			set
			{
				_lowAskPrice = value;
				Notify("LowAskPrice");
			}
		}

		private decimal? _yield;

		//[DataMember]
		/// <summary>
		/// Yield.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str321Key)]
		[DescriptionLoc(LocalizedStrings.Str321Key, true)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? Yield
		{
			get { return _yield; }
			set
			{
				_yield = value;
				Notify("Yield");
			}
		}

		private decimal? _vwap;

		//[DataMember]
		/// <summary>
		/// Average price.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		[DescriptionLoc(LocalizedStrings.AveragePriceKey, true)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? VWAP
		{
			get { return _vwap; }
			set
			{
				_vwap = value;
				Notify("VWAP");
			}
		}

		private decimal? _settlementPrice;

		//[DataMember]
		/// <summary>
		/// Settlement price.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str312Key)]
		[DescriptionLoc(LocalizedStrings.SettlementPriceKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? SettlementPrice
		{
			get { return _settlementPrice; }
			set
			{
				_settlementPrice = value;
				Notify("SettlementPrice");
			}
		}

		private decimal? _averagePrice;

		//[DataMember]
		/// <summary>
		/// Average price per session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		[DescriptionLoc(LocalizedStrings.Str600Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? AveragePrice
		{
			get { return _averagePrice; }
			set
			{
				_averagePrice = value;
				Notify("AveragePrice");
			}
		}

		private decimal? _volume;

		//[DataMember]
		/// <summary>
		/// Volume per session.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str601Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal? Volume
		{
			get { return _volume; }
			set
			{
				_volume = value;
				Notify("Volume");
			}
		}

		[field: NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

	    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Id;
		}

		/// <summary>
		/// Create a copy of <see cref="Security"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Security Clone()
		{
			var clone = new Security();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// To copy fields of the current instrument to <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The instrument in which you should to copy fields.</param>
		public void CopyTo(Security destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			destination.Id = Id;
			destination.Name = Name;
			destination.Type = Type;
			destination.Code = Code;
			destination.Class = Class;
			destination.ShortName = ShortName;
			destination.VolumeStep = VolumeStep;
			destination.Multiplier = Multiplier;
			destination.PriceStep = PriceStep;
			destination.Decimals = Decimals;
			destination.SettlementDate = SettlementDate;
			destination.Board = Board;
			destination.ExpiryDate = ExpiryDate;
			destination.OptionType = OptionType;
			destination.Strike = Strike;
			destination.BinaryOptionType = BinaryOptionType;
			destination.UnderlyingSecurityId = UnderlyingSecurityId;
			destination.ExternalId = ExternalId.Clone();
			destination.Currency = Currency;
			destination.StepPrice = StepPrice;
			destination.LowPrice = LowPrice;
			destination.HighPrice = HighPrice;
			destination.ClosePrice = ClosePrice;
			destination.OpenPrice = OpenPrice;
			destination.MinPrice = MinPrice;
			destination.MaxPrice = MaxPrice;
			destination.State = State;
			destination.TheorPrice = TheorPrice;
			destination.ImpliedVolatility = ImpliedVolatility;
			destination.HistoricalVolatility = HistoricalVolatility;
			destination.MarginBuy = MarginBuy;
			destination.MarginSell = MarginSell;
			destination.OpenInterest = OpenInterest;
			destination.BidsCount = BidsCount;
			destination.BidsVolume = BidsVolume;
			destination.AsksCount = AsksCount;
			destination.AsksVolume = AsksVolume;

			//if (destination.ExtensionInfo == null)
			//	destination.ExtensionInfo = new SynchronizedDictionary<object, object>();

			//if (LastTrade != null)
			//{
			//	destination.LastTrade = LastTrade.Clone();
			//	destination.LastTrade.Security = destination;
			//}

			//if (BestBid != null)
			//{
			//	destination.BestBid = BestBid.Clone();
			//	destination.BestBid.Security = destination;
			//}

			//if (BestAsk != null)
			//{
			//	destination.BestAsk = BestAsk.Clone();
			//	destination.BestAsk.Security = destination;
			//}
		}

		/// <summary>
		/// To call the event <see cref="INotifyPropertyChanged.PropertyChanged"/>.
		/// </summary>
		/// <param name="propName">Property name.</param>
		protected void Notify(string propName)
		{
			_propertyChanged.SafeInvoke(this, propName);
		}
	}
}