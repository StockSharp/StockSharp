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
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Security (shares, futures, options etc.).
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str546Key)]
	public class Security : Cloneable<Security>, IExtendableEntity, INotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Security"/>.
		/// </summary>
		public Security()
		{
		}

		private string _id;

		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[Identity]
		[ReadOnly(true)]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str361Key,
			Description = LocalizedStrings.SecurityIdKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public string Id
		{
			get => _id;
			set
			{
				if (_id == value)
					return;

				_id = value;
				Notify(nameof(Id));
			}
		}

		private string _code;

		/// <summary>
		/// Security code.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CodeKey,
			Description = LocalizedStrings.Str349Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 1)]
		[Required(AllowEmptyStrings = false)]
		public string Code
		{
			get => _code;
			set
			{
				if (_code == value)
					return;

				_code = value;
				Notify(nameof(Code));
			}
		}

		private ExchangeBoard _board;

		/// <summary>
		/// Exchange board where the security is traded.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BoardKey,
			Description = LocalizedStrings.Str549Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 2)]
		[Required]
		public virtual ExchangeBoard Board
		{
			get => _board;
			set
			{
				if (_board == value)
					return;

				_board = value;
				Notify(nameof(Board));
			}
		}

		private SecurityTypes? _type;

		/// <summary>
		/// Security type.
		/// </summary>
		[DataMember]
		[Nullable]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TypeKey,
			Description = LocalizedStrings.Str360Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 3)]
		public SecurityTypes? Type
		{
			get => _type;
			set
			{
				if (_type == value)
					return;

				_type = value;
				Notify(nameof(Type));
			}
		}

		private string _name;

		/// <summary>
		/// Security name.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.NameKey,
			Description = LocalizedStrings.Str362Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 4)]
		public string Name
		{
			get => _name;
			set
			{
				if (_name == value)
					return;

				_name = value;
				Notify(nameof(Name));
			}
		}

		private string _shortName;

		/// <summary>
		/// Short security name.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str363Key,
			Description = LocalizedStrings.Str364Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 5)]
		public string ShortName
		{
			get => _shortName;
			set
			{
				if (_shortName == value)
					return;

				_shortName = value;
				Notify(nameof(ShortName));
			}
		}

		private CurrencyTypes? _currency;

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[Nullable]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CurrencyKey,
			Description = LocalizedStrings.Str382Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 6)]
		public CurrencyTypes? Currency
		{
			get => _currency;
			set
			{
				_currency = value;
				Notify(nameof(Currency));
			}
		}

		private SecurityExternalId _externalId = new SecurityExternalId();

		/// <summary>
		/// Security ID in other systems.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str553Key,
			Description = LocalizedStrings.Str554Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 7)]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[InnerSchema(NullWhenAllEmpty = false)]
		public SecurityExternalId ExternalId
		{
			get => _externalId;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_externalId = value;
				Notify(nameof(ExternalId));
			}
		}

		private string _class;

		/// <summary>
		/// Security class.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ClassKey,
			Description = LocalizedStrings.SecurityClassKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 8)]
		public string Class
		{
			get => _class;
			set
			{
				if (_class == value)
					return;

				_class = value;
				Notify(nameof(Class));
			}
		}

		private decimal? _priceStep;

		/// <summary>
		/// Minimum price step.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceStepKey,
			Description = LocalizedStrings.MinPriceStepKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 9)]
		[Nullable]
		[GreaterThanZero]
		public decimal? PriceStep
		{
			get => _priceStep;
			set
			{
				if (_priceStep == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_priceStep = value;
				Notify(nameof(PriceStep));
			}
		}

		private decimal? _volumeStep;

		/// <summary>
		/// Minimum volume step.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeStepKey,
			Description = LocalizedStrings.Str366Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 10)]
		[Nullable]
		[GreaterThanZero]
		public decimal? VolumeStep
		{
			get => _volumeStep;
			set
			{
				if (_volumeStep == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_volumeStep = value;
				Notify(nameof(VolumeStep));
			}
		}

		private decimal? _multiplier;

		/// <summary>
		/// Lot multiplier.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str330Key,
			Description = LocalizedStrings.LotVolumeKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 11)]
		[Nullable]
		public decimal? Multiplier
		{
			get => _multiplier;
			set
			{
				if (_multiplier == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_multiplier = value;
				Notify(nameof(Multiplier));
			}
		}

		private int? _decimals;

		/// <summary>
		/// Number of digits in price after coma.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DecimalsKey,
			Description = LocalizedStrings.Str548Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 12)]
		//[ReadOnly(true)]
		[Nullable]
		public int? Decimals
		{
			get => _decimals;
			set
			{
				if (_decimals == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_decimals = value;
				Notify(nameof(Decimals));
			}
		}

		private DateTimeOffset? _expiryDate;

		/// <summary>
		/// Security expiration date (for derivatives - expiration, for bonds — redemption).
		/// </summary>
		[DataMember]
		[Nullable]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ExpiryDateKey,
			Description = LocalizedStrings.Str371Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 13)]
		public DateTimeOffset? ExpiryDate
		{
			get => _expiryDate;
			set
			{
				if (_expiryDate == value)
					return;

				_expiryDate = value;
				Notify(nameof(ExpiryDate));
			}
		}

		private DateTimeOffset? _settlementDate;

		/// <summary>
		/// Settlement date for security (for derivatives and bonds).
		/// </summary>
		[DataMember]
		[Nullable]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SettlementDateKey,
			Description = LocalizedStrings.Str373Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 14)]
		public DateTimeOffset? SettlementDate
		{
			get => _settlementDate;
			set
			{
				if (_settlementDate == value)
					return;

				_settlementDate = value;
				Notify(nameof(SettlementDate));
			}
		}

		private string _cfiCode;

		/// <summary>
		/// Type in ISO 10962 standard.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CfiCodeKey,
			Description = LocalizedStrings.CfiCodeDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 15)]
		public string CfiCode
		{
			get => _cfiCode;
			set
			{
				if (_cfiCode == value)
					return;

				_cfiCode = value;
				Notify(nameof(CfiCode));
			}
		}

		[field: NonSerialized]
		private SynchronizedDictionary<string, object> _extensionInfo;

		/// <summary>
		/// Extended security info.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the instrument is stored in the program. For example, the date of instrument expiration (if it is option) or information about the underlying asset if it is the futures contract.
		/// </remarks>
		[XmlIgnore]
		//[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ExtendedInfoKey,
			Description = LocalizedStrings.Str427Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 20)]
		[Ignore]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set
			{
				_extensionInfo = value.Sync();
				Notify(nameof(ExtensionInfo));
			}
		}

		private decimal? _stepPrice;

		//[DataMember]
		/// <summary>
		/// Step price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str290Key,
			Description = LocalizedStrings.Str555Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 200)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? StepPrice
		{
			get => _stepPrice;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str556);

				if (_stepPrice == value)
					return;

				_stepPrice = value;
				Notify(nameof(StepPrice));
			}
		}

		private Trade _lastTrade;

		//[DataMember]
		/// <summary>
		/// Information about the last trade. If during the session on the instrument there were no trades, the value equals to <see langword="null" />.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str289Key,
			Description = LocalizedStrings.Str557Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 201)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public Trade LastTrade
		{
			get => _lastTrade;
			set
			{
				if (_lastTrade == value)
					return;

				_lastTrade = value;
				Notify(nameof(LastTrade));

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
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str558Key,
			Description = LocalizedStrings.Str559Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 202)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? OpenPrice
		{
			get => _openPrice;
			set
			{
				if (_openPrice == value)
					return;

				_openPrice = value;
				Notify(nameof(OpenPrice));
			}
		}

		private decimal? _closePrice;

		//[DataMember]
		/// <summary>
		/// Last trade price for the previous session.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str560Key,
			Description = LocalizedStrings.Str561Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 203)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? ClosePrice
		{
			get => _closePrice;
			set
			{
				if (_closePrice == value)
					return;

				_closePrice = value;
				Notify(nameof(ClosePrice));
			}
		}

		private decimal? _lowPrice;

		//[DataMember]
		/// <summary>
		/// Lowest price for the session.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str288Key,
			Description = LocalizedStrings.Str562Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 204)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? LowPrice
		{
			get => _lowPrice;
			set
			{
				if (_lowPrice == value)
					return;

				_lowPrice = value;
				Notify(nameof(LowPrice));
			}
		}

		private decimal? _highPrice;

		//[DataMember]
		/// <summary>
		/// Highest price for the session.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str563Key,
			Description = LocalizedStrings.Str564Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 205)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? HighPrice
		{
			get => _highPrice;
			set
			{
				if (_highPrice == value)
					return;

				_highPrice = value;
				Notify(nameof(HighPrice));
			}
		}

		private Quote _bestBid;

		//[DataMember]
		/// <summary>
		/// Best bid in market depth.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str565Key,
			Description = LocalizedStrings.Str566Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 206)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public Quote BestBid
		{
			get => _bestBid;
			set
			{
				//TODO: решить другим методом, OnEquals не тормозит, медленно работает GUI
				//PYH: Тормозит OnEquals

				//if (_bestBid == value)
				//	return;

				_bestBid = value;
				Notify(nameof(BestBid));
			}
		}

		private Quote _bestAsk;

		//[DataMember]
		/// <summary>
		/// Best ask in market depth.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BestAskKey,
			Description = LocalizedStrings.BestAskDescKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 207)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public Quote BestAsk
		{
			get => _bestAsk;
			set
			{
				// if (_bestAsk == value)
				//	return;

				_bestAsk = value;
				Notify(nameof(BestAsk));
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
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BestPairKey,
			Description = LocalizedStrings.BestPairKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str436Key,
			Order = 208)]
		public MarketDepthPair BestPair => new MarketDepthPair(this, BestBid, BestAsk);

		private SecurityStates? _state;

		//[DataMember]
		//[Enum]
		/// <summary>
		/// Current state of security.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.StateKey,
			Description = LocalizedStrings.Str569Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 209)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public SecurityStates? State
		{
			get => _state;
			set
			{
				if (_state == value)
					return;

				_state = value;
				Notify(nameof(State));
			}
		}

		private decimal? _minPrice;

		//[DataMember]
		/// <summary>
		/// Lower price limit.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceMinKey,
			Description = LocalizedStrings.PriceMinLimitKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 210)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? MinPrice
		{
			get => _minPrice;
			set
			{
				if (_minPrice == value)
					return;

				_minPrice = value;
				Notify(nameof(MinPrice));
			}
		}

		private decimal? _maxPrice;

		//[DataMember]
		/// <summary>
		/// Upper price limit.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceMaxKey,
			Description = LocalizedStrings.PriceMaxLimitKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 211)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? MaxPrice
		{
			get => _maxPrice;
			set
			{
				if (_maxPrice == value)
					return;

				_maxPrice = value;
				Notify(nameof(MaxPrice));
			}
		}

		private decimal? _marginBuy;

		//[DataMember]
		/// <summary>
		/// Initial margin to buy.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str304Key,
			Description = LocalizedStrings.MarginBuyKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 212)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? MarginBuy
		{
			get => _marginBuy;
			set
			{
				if (_marginBuy == value)
					return;

				_marginBuy = value;
				Notify(nameof(MarginBuy));
			}
		}

		private decimal? _marginSell;

		//[DataMember]
		/// <summary>
		/// Initial margin to sell.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str305Key,
			Description = LocalizedStrings.MarginSellKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 213)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? MarginSell
		{
			get => _marginSell;
			set
			{
				if (_marginSell == value)
					return;

				_marginSell = value;
				Notify(nameof(MarginSell));
			}
		}

		//[field: NonSerialized]
		//private IConnector _connector;

		///// <summary>
		///// Connection to the trading system, through which this instrument has been downloaded.
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
		//		Notify(nameof(Trader));
		//	}
		//}

		private string _underlyingSecurityId;

		/// <summary>
		/// Underlying asset on which the current security is built.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.UnderlyingAssetKey,
			Description = LocalizedStrings.Str550Key,
			GroupName = LocalizedStrings.Str437Key,
			Order = 100)]
		public string UnderlyingSecurityId
		{
			get => _underlyingSecurityId;
			set
			{
				if (_underlyingSecurityId == value)
					return;

				_underlyingSecurityId = value;
				Notify(nameof(UnderlyingSecurityId));
			}
		}

		private OptionTypes? _optionType;

		/// <summary>
		/// Option type.
		/// </summary>
		[DataMember]
		[Nullable]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str551Key,
			Description = LocalizedStrings.OptionContractTypeKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 101)]
		public OptionTypes? OptionType
		{
			get => _optionType;
			set
			{
				if (_optionType == value)
					return;

				_optionType = value;
				Notify(nameof(OptionType));
			}
		}

		private decimal? _strike;

		/// <summary>
		/// Option strike price.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.StrikeKey,
			Description = LocalizedStrings.OptionStrikePriceKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 102)]
		[Nullable]
		public decimal? Strike
		{
			get => _strike;
			set
			{
				if (_strike == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_strike = value;
				Notify(nameof(Strike));
			}
		}

		private string _binaryOptionType;

		/// <summary>
		/// Type of binary option.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str552Key,
			Description = LocalizedStrings.TypeBinaryOptionKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 103)]
		public string BinaryOptionType
		{
			get => _binaryOptionType;
			set
			{
				if (_binaryOptionType == value)
					return;

				_binaryOptionType = value;
				Notify(nameof(BinaryOptionType));
			}
		}

		private decimal? _impliedVolatility;

		//[DataMember]
		/// <summary>
		/// Volatility (implied).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IVKey,
			Description = LocalizedStrings.Str293Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str437Key,
			Order = 104)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? ImpliedVolatility
		{
			get => _impliedVolatility;
			set
			{
				if (_impliedVolatility == value)
					return;

				_impliedVolatility = value;
				Notify(nameof(ImpliedVolatility));
			}
		}

		private decimal? _historicalVolatility;

		//[DataMember]
		/// <summary>
		/// Volatility (historical).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.HVKey,
			Description = LocalizedStrings.Str299Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str437Key,
			Order = 105)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? HistoricalVolatility
		{
			get => _historicalVolatility;
			set
			{
				if (_historicalVolatility == value)
					return;

				_historicalVolatility = value;
				Notify(nameof(HistoricalVolatility));
			}
		}

		private decimal? _theorPrice;

		//[DataMember]
		/// <summary>
		/// Theoretical price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str294Key,
			Description = LocalizedStrings.TheoreticalPriceKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 106)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? TheorPrice
		{
			get => _theorPrice;
			set
			{
				if (_theorPrice == value)
					return;

				_theorPrice = value;
				Notify(nameof(TheorPrice));
			}
		}

		private decimal? _delta;

		//[DataMember]
		/// <summary>
		/// Option delta.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DeltaKey,
			Description = LocalizedStrings.OptionDeltaKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 107)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Delta
		{
			get => _delta;
			set
			{
				if (_delta == value)
					return;

				_delta = value;
				Notify(nameof(Delta));
			}
		}

		private decimal? _gamma;

		//[DataMember]
		/// <summary>
		/// Option gamma.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.GammaKey,
			Description = LocalizedStrings.OptionGammaKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 108)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Gamma
		{
			get => _gamma;
			set
			{
				if (_gamma == value)
					return;

				_gamma = value;
				Notify(nameof(Gamma));
			}
		}

		private decimal? _vega;

		//[DataMember]
		/// <summary>
		/// Option vega.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VegaKey,
			Description = LocalizedStrings.OptionVegaKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 109)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Vega
		{
			get => _vega;
			set
			{
				if (_vega == value)
					return;

				_vega = value;
				Notify(nameof(Vega));
			}
		}

		private decimal? _theta;

		//[DataMember]
		/// <summary>
		/// Option theta.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ThetaKey,
			Description = LocalizedStrings.OptionThetaKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 110)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Theta
		{
			get => _theta;
			set
			{
				if (_theta == value)
					return;

				_theta = value;
				Notify(nameof(Theta));
			}
		}

		private decimal? _rho;

		//[DataMember]
		/// <summary>
		/// Option rho.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.RhoKey,
			Description = LocalizedStrings.OptionRhoKey,
			GroupName = LocalizedStrings.Str437Key,
			Order = 111)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Rho
		{
			get => _rho;
			set
			{
				if (_rho == value)
					return;

				_rho = value;
				Notify(nameof(Rho));
			}
		}

		private decimal? _openInterest;

		//[DataMember]
		/// <summary>
		/// Number of open positions (open interest).
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str150Key,
			Description = LocalizedStrings.Str151Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 220)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? OpenInterest
		{
			get => _openInterest;
			set
			{
				if (_openInterest == value)
					return;

				_openInterest = value;
				Notify(nameof(OpenInterest));
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
			get => _localTime;
			set
			{
				_localTime = value;
				Notify(nameof(LocalTime));
			}
		}

		private DateTimeOffset _lastChangeTime;

		//[StatisticsCategory]
		/// <summary>
		/// Time of the last instrument change.
		/// </summary>
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		[Ignore]
		[XmlIgnore]
		public DateTimeOffset LastChangeTime
		{
			get => _lastChangeTime;
			set
			{
				_lastChangeTime = value;
				Notify(nameof(LastChangeTime));
			}
		}

		private decimal? _bidsVolume;

		//[DataMember]
		/// <summary>
		/// Total volume in all buy orders.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str295Key,
			Description = LocalizedStrings.BidsVolumeKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 221)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? BidsVolume
		{
			get => _bidsVolume;
			set
			{
				_bidsVolume = value;
				Notify(nameof(BidsVolume));
			}
		}

		private int? _bidsCount;

		//[DataMember]
		/// <summary>
		/// Number of buy orders.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BidsKey,
			Description = LocalizedStrings.BidsCountKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 222)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public int? BidsCount
		{
			get => _bidsCount;
			set
			{
				_bidsCount = value;
				Notify(nameof(BidsCount));
			}
		}

		private decimal? _asksVolume;

		//[DataMember]
		/// <summary>
		/// Total volume in all sell orders.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str297Key,
			Description = LocalizedStrings.AsksVolumeKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 223)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? AsksVolume
		{
			get => _asksVolume;
			set
			{
				_asksVolume = value;
				Notify(nameof(AsksVolume));
			}
		}

		private int? _asksCount;

		//[DataMember]
		/// <summary>
		/// Number of sell orders.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AsksKey,
			Description = LocalizedStrings.AsksCountKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 224)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public int? AsksCount
		{
			get => _asksCount;
			set
			{
				_asksCount = value;
				Notify(nameof(AsksCount));
			}
		}

		private int? _tradesCount;

		//[DataMember]
		/// <summary>
		/// Number of trades.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TradesOfKey,
			Description = LocalizedStrings.Str232Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str436Key,
			Order = 225)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public int? TradesCount
		{
			get => _tradesCount;
			set
			{
				_tradesCount = value;
				Notify(nameof(TradesCount));
			}
		}

		private decimal? _highBidPrice;

		//[DataMember]
		/// <summary>
		/// Maximum bid during the session.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str319Key,
			Description = LocalizedStrings.Str594Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 226)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? HighBidPrice
		{
			get => _highBidPrice;
			set
			{
				_highBidPrice = value;
				Notify(nameof(HighBidPrice));
			}
		}

		private decimal? _lowAskPrice;

		//[DataMember]
		/// <summary>
		/// Minimum ask during the session.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str320Key,
			Description = LocalizedStrings.Str595Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 227)]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? LowAskPrice
		{
			get => _lowAskPrice;
			set
			{
				_lowAskPrice = value;
				Notify(nameof(LowAskPrice));
			}
		}

		private decimal? _yield;

		//[DataMember]
		/// <summary>
		/// Yield.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str321Key,
			Description = LocalizedStrings.Str321Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str436Key,
			Order = 228)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Yield
		{
			get => _yield;
			set
			{
				_yield = value;
				Notify(nameof(Yield));
			}
		}

		private decimal? _vwap;

		//[DataMember]
		/// <summary>
		/// Average price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AveragePriceKey,
			Description = LocalizedStrings.AveragePriceKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str436Key,
			Order = 229)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? VWAP
		{
			get => _vwap;
			set
			{
				_vwap = value;
				Notify(nameof(VWAP));
			}
		}

		private decimal? _settlementPrice;

		//[DataMember]
		/// <summary>
		/// Settlement price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str312Key,
			Description = LocalizedStrings.SettlementPriceKey,
			GroupName = LocalizedStrings.Str436Key,
			Order = 230)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? SettlementPrice
		{
			get => _settlementPrice;
			set
			{
				_settlementPrice = value;
				Notify(nameof(SettlementPrice));
			}
		}

		private decimal? _averagePrice;

		//[DataMember]
		/// <summary>
		/// Average price per session.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AveragePriceKey,
			Description = LocalizedStrings.Str600Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 231)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? AveragePrice
		{
			get => _averagePrice;
			set
			{
				_averagePrice = value;
				Notify(nameof(AveragePrice));
			}
		}

		private decimal? _volume;

		//[DataMember]
		/// <summary>
		/// Volume per session.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeKey,
			Description = LocalizedStrings.Str601Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 232)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Volume
		{
			get => _volume;
			set
			{
				_volume = value;
				Notify(nameof(Volume));
			}
		}

		private decimal? _turnover;

		/// <summary>
		/// Turnover.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TurnoverKey,
			Description = LocalizedStrings.TurnoverKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str436Key,
			Order = 232)]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Use the IConnector.GetSecurityValue.")]
		public decimal? Turnover
		{
			get => _turnover;
			set
			{
				_turnover = value;
				Notify(nameof(Turnover));
			}
		}

		private decimal? _issueSize;

		/// <summary>
		/// Number of issued contracts.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IssueSizeKey,
			Description = LocalizedStrings.IssueSizeKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 21)]
		[DataMember]
		[Nullable]
		public decimal? IssueSize
		{
			get => _issueSize;
			set
			{
				_issueSize = value;
				Notify(nameof(IssueSize));
			}
		}

		private DateTimeOffset? _issueDate;
		
		/// <summary>
		/// Date of issue.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IssueDateKey,
			Description = LocalizedStrings.IssueDateKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 22)]
		[DataMember]
		[Nullable]
		public DateTimeOffset? IssueDate
		{
			get => _issueDate;
			set
			{
				_issueDate = value;
				Notify(nameof(IssueDate));
			}
		}

		private SecurityTypes? _underlyingSecurityType;

		/// <summary>
		/// Underlying security type.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AssetTypeKey,
			Description = LocalizedStrings.UnderlyingSecurityTypeKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str437Key,
			Order = 103)]
		[DataMember]
		[Nullable]
		public SecurityTypes? UnderlyingSecurityType
		{
			get => _underlyingSecurityType;
			set
			{
				_underlyingSecurityType = value;
				Notify(nameof(UnderlyingSecurityType));
			}
		}

		private decimal? _buyBackPrice;

		/// <summary>
		/// BuyBack price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? BuyBackPrice
		{
			get => _buyBackPrice;
			set
			{
				_buyBackPrice = value;
				Notify(nameof(BuyBackPrice));
			}
		}

		private DateTimeOffset? _buyBackDate;
		
		/// <summary>
		/// BuyBack date.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public DateTimeOffset? BuyBackDate
		{
			get => _buyBackDate;
			set
			{
				_buyBackDate = value;
				Notify(nameof(BuyBackDate));
			}
		}

		/// <summary>
		/// Basket security type. Can be <see langword="null"/> in case of regular security.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CodeKey,
			Description = LocalizedStrings.BasketCodeKey,
			GroupName = LocalizedStrings.BasketKey,
			Order = 200)]
		public virtual string BasketCode { get; set; }

		/// <summary>
		/// Basket security expression. Can be <see langword="null"/> in case of regular security.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ExpressionKey,
			Description = LocalizedStrings.ExpressionDescKey,
			GroupName = LocalizedStrings.BasketKey,
			Order = 201)]
		public virtual string BasketExpression { get; set; }

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
				Notify(nameof(CommissionTaker));
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
				Notify(nameof(CommissionMaker));
			}
		}

		[field: NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;
			remove => _propertyChanged -= value;
		}

		/// <inheritdoc />
		public override string ToString() => Id;

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
			destination.CfiCode = CfiCode;
			destination.Turnover = Turnover;
			destination.IssueSize = IssueSize;
			destination.IssueDate = IssueDate;
			destination.UnderlyingSecurityType = UnderlyingSecurityType;
			destination.BuyBackDate = BuyBackDate;
			destination.BuyBackPrice = BuyBackPrice;
			destination.BasketCode = BasketCode;
			destination.BasketExpression = BasketExpression;
			destination.CommissionTaker = CommissionTaker;
			destination.CommissionMaker = CommissionMaker;

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
			_propertyChanged?.Invoke(this, propName);
		}
	}
}