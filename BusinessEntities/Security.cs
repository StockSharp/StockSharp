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
	/// Инструмент (акция, фьючерс, опцион и т.д.).
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
		/// Создать <see cref="Security"/>.
		/// </summary>
		public Security()
		{
		}

		private string _id = string.Empty;

		/// <summary>
		/// Идентификатор инструмента.
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
		/// Название инструмента.
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
		/// Короткое название инструмента.
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
		/// Код инструмента.
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
		/// Класс инструмента.
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

		private decimal _priceStep = 0.01m;

		/// <summary>
		/// Минимальный шаг цены. По-умолчанию равно 0.01.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceStepKey)]
		[DescriptionLoc(LocalizedStrings.MinPriceStepKey)]
		[MainCategory]
		[PropertyOrder(5)]
		public decimal PriceStep
		{
			get { return _priceStep; }
			set
			{
				if (_priceStep == value)
					return;

				_priceStep = value;
				Decimals = _priceStep.GetCachedDecimals();
				Notify("PriceStep");
			}
		}

		private decimal _volumeStep = 1;

		/// <summary>
		/// Минимальный шаг объема. По-умолчанию равно 1.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str365Key)]
		[DescriptionLoc(LocalizedStrings.Str366Key)]
		[MainCategory]
		[PropertyOrder(6)]
		public decimal VolumeStep
		{
			get { return _volumeStep; }
			set
			{
				if (_volumeStep == value)
					return;

				_volumeStep = value;
				Notify("VolumeStep");
			}
		}

		private decimal _multiplier = 1;

		/// <summary>
		/// Коэфициент объема между лотом и активом. По-умолчанию равен 1.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str330Key)]
		[DescriptionLoc(LocalizedStrings.LotVolumeKey)]
		[MainCategory]
		[PropertyOrder(7)]
		public decimal Multiplier
		{
			get { return _multiplier; }
			set
			{
				if (_multiplier == value)
					return;

				_multiplier = value;
				Notify("Multiplier");
			}
		}

		private int _decimals = 2;

		/// <summary>
		/// Количество знаков в цене после запятой. Автоматически выставляется при установке <see cref="Security.PriceStep"/>.
		/// По-умолчанию равно 2-ум знакам.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str547Key)]
		[DescriptionLoc(LocalizedStrings.Str548Key)]
		[MainCategory]
		[PropertyOrder(7)]
		[ReadOnly(true)]
		public int Decimals
		{
			get { return _decimals; }
			set
			{
				if (_decimals == value)
					return;

				_decimals = value;
				Notify("Decimals");
			}
		}

		private SecurityTypes? _type;

		/// <summary>
		/// Тип инструмента.
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
		/// Дата экспирация инструмента (для деривативов - экспирация, для облигаций - погашение).
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
		/// Дата выплат по инструмента (для деривативов и облигаций).
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.PaymentDateKey)]
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
		/// Биржевая площадка, на которой торгуется инструмент.
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
		/// Базовый актив, на основе которого построен данный инструмент.
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
		/// Тип опциона.
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

		private decimal _strike;

		/// <summary>
		/// Страйк цена опциона.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StrikeKey)]
		[DescriptionLoc(LocalizedStrings.OptionStrikePriceKey)]
		[DerivativesCategory]
		[PropertyOrder(1)]
		public decimal Strike
		{
			get { return _strike; }
			set
			{
				if (_strike == value)
					return;

				_strike = value;
				Notify("Strike");
			}
		}

		private CurrencyTypes? _currency;

		/// <summary>
		/// Валюта торгового инструмента.
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str250Key)]
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
		/// Тип бинарного опциона.
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

		private SecurityExternalId _externalId = new SecurityExternalId();

		/// <summary>
		/// Идентификатор инструмента в других системах.
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
				if (value == null)
					throw new ArgumentNullException("value");

				_externalId = value;
				Notify("ExternalId");
			}
		}

		[field: NonSerialized]
		private SynchronizedDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Расширенная информация по инструменту.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации, ассоциированной с инструментом.
		/// Например, дата экспирации инструмента (если эта опцион) или информация о базовом активе, если это фьючерсный контракт.
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

		private decimal _stepPrice = 1;

		/// <summary>
		/// Стоимость шага цены. По-умолчанию равно 1.
		/// </summary>
		//[DataMember]
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str290Key)]
		[DescriptionLoc(LocalizedStrings.Str555Key)]
		[StatisticsCategory]
		[PropertyOrder(0)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal StepPrice
		{
			get { return _stepPrice; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str556);

				if (_stepPrice == value)
					return;

				_stepPrice = value;
				Notify("StepPrice");
			}
		}

		private Trade _lastTrade;

		/// <summary>
		/// Информация о последней сделке. Если за сессию по инструменту не было сделок, то значение равно null.
		/// </summary>
		//[DataMember]
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

		private decimal _openPrice;

		/// <summary>
		/// Первая цена сделки за сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str558Key)]
		[DescriptionLoc(LocalizedStrings.Str559Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(11)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal OpenPrice
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

		private decimal _closePrice;

		/// <summary>
		/// Последняя цена сделки за предыдущую сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str560Key)]
		[DescriptionLoc(LocalizedStrings.Str561Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(14)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal ClosePrice
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

		private decimal _lowPrice;

		/// <summary>
		/// Наименьшая цена сделки за сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str288Key)]
		[DescriptionLoc(LocalizedStrings.Str562Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(13)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal LowPrice
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

		private decimal _highPrice;

		/// <summary>
		/// Наивысшая цена сделки за сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str563Key)]
		[DescriptionLoc(LocalizedStrings.Str564Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(12)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal HighPrice
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

		/// <summary>
		/// Лучшая покупка в стакане.
		/// </summary>
		//[DataMember]
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

		/// <summary>
		/// Лучшая продажа в стакане.
		/// </summary>
		//[DataMember]
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

		/// <summary>
		/// Лучшая пара котировок.
		/// </summary>
		//[DisplayName("Лучшая пара")]
		//[Description("Лучшая пара котировок.")]
		//[ExpandableObject]
		//[StatisticsCategory]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public MarketDepthPair BestPair
		{
			get { return new MarketDepthPair(this, BestBid, BestAsk); }
		}

		private SecurityStates _state;

		/// <summary>
		/// Текущее состояние инструмента.
		/// </summary>
		//[DataMember]
		//[Enum]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str569Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public SecurityStates State
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

		private decimal _minPrice;

		/// <summary>
		/// Нижний лимит цены. По-умолчанию равно 0.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceMinKey)]
		[DescriptionLoc(LocalizedStrings.PriceMinLimitKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal MinPrice
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

		private decimal _maxPrice = decimal.MaxValue;

		/// <summary>
		/// Верхний лимит цены. По-умолчанию равно <see cref="decimal.MaxValue"/>.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceMaxKey)]
		[DescriptionLoc(LocalizedStrings.PriceMaxLimitKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal MaxPrice
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

		private decimal _marginBuy;

		/// <summary>
		/// Гарантийное обеспечение на покупку.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str304Key)]
		[DescriptionLoc(LocalizedStrings.MarginBuyKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal MarginBuy
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

		private decimal _marginSell;

		/// <summary>
		/// Гарантийное обеспечение на продажу.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str305Key)]
		[DescriptionLoc(LocalizedStrings.MarginSellKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal MarginSell
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
		/// Подключение к торговой системе, через который был загружен данный инструмент.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		[Obsolete("Security.Connector устарел и всегда равен null.")]
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

		/// <summary>
		/// Волатильность (подразумеваемая).
		/// </summary>
		//[DataMember]
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

		/// <summary>
		/// Волатильность (историческая).
		/// </summary>
		//[DataMember]
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

		/// <summary>
		/// Теоретическая цена.
		/// </summary>
		//[DataMember]
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

		/// <summary>
		/// Дельта опциона.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str300Key)]
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

		/// <summary>
		/// Гамма опциона.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str301Key)]
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

		/// <summary>
		/// Вега опциона.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str302Key)]
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

		/// <summary>
		/// Тета опциона.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str303Key)]
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

		/// <summary>
		/// Ро опциона.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str317Key)]
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

		/// <summary>
		/// Количество открытых позиций (открытый интерес).
		/// </summary>
		//[DataMember]
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

		private DateTime _localTime;

		/// <summary>
		/// Локальное время последнего изменения инструмента.
		/// </summary>
		[Browsable(false)]
		[Ignore]
		[XmlIgnore]
		public DateTime LocalTime
		{
			get { return _localTime; }
			set
			{
				_localTime = value;
				Notify("LocalTime");
			}
		}

		private DateTimeOffset _lastChangeTime;

		/// <summary>
		/// Время последнего изменения инструмента.
		/// </summary>
		//[DisplayName("Изменен")]
		//[Description("Время последнего изменения инструмента.")]
		//[StatisticsCategory]
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

		private decimal _bidsVolume;

		/// <summary>
		/// Суммарный объем во всех заявках на покупку.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str295Key)]
		[DescriptionLoc(LocalizedStrings.BidsVolumeKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(6)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal BidsVolume
		{
			get { return _bidsVolume; }
			set
			{
				_bidsVolume = value;
				Notify("BidsVolume");
			}
		}

		private int _bidsCount;

		/// <summary>
		/// Количество заявок на покупку.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.BidsKey)]
		[DescriptionLoc(LocalizedStrings.BidsCountKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(8)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public int BidsCount
		{
			get { return _bidsCount; }
			set
			{
				_bidsCount = value;
				Notify("BidsCount");
			}
		}

		private decimal _asksVolume;

		/// <summary>
		/// Суммарный объем во всех заявках на продажу.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str297Key)]
		[DescriptionLoc(LocalizedStrings.AsksVolumeKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(7)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal AsksVolume
		{
			get { return _asksVolume; }
			set
			{
				_asksVolume = value;
				Notify("AsksVolume");
			}
		}

		private int _asksCount;

		/// <summary>
		/// Количество заявок на продажу.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.AsksKey)]
		[DescriptionLoc(LocalizedStrings.AsksCountKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(9)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public int AsksCount
		{
			get { return _asksCount; }
			set
			{
				_asksCount = value;
				Notify("AsksCount");
			}
		}

		private int _tradesCount;

		/// <summary>
		/// Количество сделок.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.TradesOfKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key, true)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(10)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public int TradesCount
		{
			get { return _tradesCount; }
			set
			{
				_tradesCount = value;
				Notify("TradesCount");
			}
		}

		private decimal _highBidPrice;

		/// <summary>
		/// Максимальный бид за сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str319Key)]
		[DescriptionLoc(LocalizedStrings.Str594Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(4)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal HighBidPrice
		{
			get { return _highBidPrice; }
			set
			{
				_highBidPrice = value;
				Notify("HighBidPrice");
			}
		}

		private decimal _lowAskPrice;

		/// <summary>
		/// Минимальный оффер за сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str320Key)]
		[DescriptionLoc(LocalizedStrings.Str595Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[PropertyOrder(5)]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal LowAskPrice
		{
			get { return _lowAskPrice; }
			set
			{
				_lowAskPrice = value;
				Notify("LowAskPrice");
			}
		}

		private decimal _yield;

		/// <summary>
		/// Доходность.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str321Key)]
		[DescriptionLoc(LocalizedStrings.Str321Key, true)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal Yield
		{
			get { return _yield; }
			set
			{
				_yield = value;
				Notify("Yield");
			}
		}

		private decimal _vwap;

		/// <summary>
		/// Средневзвешенная цена.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		[DescriptionLoc(LocalizedStrings.AveragePriceKey, true)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal VWAP
		{
			get { return _vwap; }
			set
			{
				_vwap = value;
				Notify("VWAP");
			}
		}

		private decimal _settlementPrice;

		/// <summary>
		/// Рассчетная цена.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str312Key)]
		[DescriptionLoc(LocalizedStrings.SettlementPriceKey)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal SettlementPrice
		{
			get { return _settlementPrice; }
			set
			{
				_settlementPrice = value;
				Notify("SettlementPrice");
			}
		}

		private decimal _averagePrice;

		/// <summary>
		/// Средняя цена за сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		[DescriptionLoc(LocalizedStrings.Str600Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal AveragePrice
		{
			get { return _averagePrice; }
			set
			{
				_averagePrice = value;
				Notify("AveragePrice");
			}
		}

		private decimal _volume;

		/// <summary>
		/// Объем за сессию.
		/// </summary>
		//[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str601Key)]
		[StatisticsCategory]
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		//[Obsolete("Необходимо использовать метод IConnector.GetSecurityValue.")]
		public decimal Volume
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
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Id;
		}

		/// <summary>
		/// Создать копию объекта <see cref="Security"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public override Security Clone()
		{
			var clone = new Security();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Скопировать поля текущего инструмента в <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">Инструмент, в который необходимо скопировать поля.</param>
		public void CopyTo(Security destination)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

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
		/// Вызвать событие <see cref="INotifyPropertyChanged.PropertyChanged"/>.
		/// </summary>
		/// <param name="propName">Название свойства.</param>
		protected void Notify(string propName)
		{
			_propertyChanged.SafeInvoke(this, propName);
		}
	}
}