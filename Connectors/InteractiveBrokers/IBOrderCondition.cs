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
	/// Условие заявок, специфичных для <see cref="InteractiveBrokers"/>.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "FIX")]
	public class IBOrderCondition : OrderCondition
	{
		/// <summary>
		/// Базовое условие.
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
			/// Получить значение параметра.
			/// </summary>
			/// <typeparam name="T">Тип значения.</typeparam>
			/// <param name="name">Название параметра.</param>
			/// <returns>Значение параметра.</returns>
			protected T GetValue<T>(string name)
			{
				if (!_condition.Parameters.ContainsKey(name))
					throw new ArgumentException(LocalizedStrings.Str2311Params.Put(name), "name");

				return (T)_condition.Parameters[name];
			}

			/// <summary>
			/// Получить значение параметра. Если значение не существует, будет возвращено null.
			/// </summary>
			/// <typeparam name="T">Тип значения.</typeparam>
			/// <param name="name">Название параметра.</param>
			/// <returns>Значение параметра.</returns>
			protected T TryGetValue<T>(string name)
			{
				return (T)_condition.Parameters.TryGetValue(name);
			}

			/// <summary>
			/// Установить новое значение параметра.
			/// </summary>
			/// <typeparam name="T">Тип значения.</typeparam>
			/// <param name="name">Название параметра.</param>
			/// <param name="value">Значение параметра.</param>
			protected void SetValue<T>(string name, T value)
			{
				_condition.Parameters[name] = value;
			}
		}

		/// <summary>
		/// Расширенные типы заявок, специфичных для <see cref="IBTrader"/>.
		/// </summary>
		public enum ExtendedOrderTypes
		{
			/// <summary>
			/// Исполнить по рыночной цене, если цена закрытия превысила ожидаемую цену.
			/// </summary>
			/// <remarks>
			/// Не действует для US <see cref="SecurityTypes.Future"/>, US <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str2312Key)]
			MarketOnClose,

			/// <summary>
			/// Исполнить по заданной цене, если цена закрытия превысила ожидаемую цену.
			/// </summary>
			/// <remarks>
			/// Не действует для US <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.LimitOnCloseKey)]
			LimitOnClose,

			/// <summary>
			/// По лучшей цене.
			/// </summary>
			/// <remarks>
			/// Действует для <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str2314Key)]
			PeggedToMarket,

			/// <summary>
			/// Стоп с рыночной ценой активации.
			/// </summary>
			/// <remarks>
			/// Действует для <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>, <see cref="SecurityTypes.Warrant"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
			Stop,

			/// <summary>
			/// Стоп с заданной ценой активации.
			/// </summary>
			/// <remarks>
			/// Действует для <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str1733Key)]
			StopLimit,

			/// <summary>
			/// Скользящий стоп.
			/// </summary>
			/// <remarks>
			/// Действует для <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>, <see cref="SecurityTypes.Warrant"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.TrailingKey)]
			TrailingStop,

			/// <summary>
			/// Со сдвигом.
			/// </summary>
			/// <remarks>
			/// Действует для <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.Str2316Key)]
			Relative,

			/// <summary>
			/// VWAP.
			/// </summary>
			/// <remarks>
			/// Действует для <see cref="SecurityTypes.Stock"/>.
			/// </remarks>
			[EnumDisplayName("VWAP")]
			VolumeWeightedAveragePrice,

			/// <summary>
			/// Лимитированный скользящий стоп.
			/// </summary>
			/// <remarks>
			/// Действует для <see cref="SecurityTypes.Currency"/>, <see cref="SecurityTypes.Future"/>, <see cref="SecurityTypes.Option"/>, <see cref="SecurityTypes.Stock"/>, <see cref="SecurityTypes.Warrant"/>.
			/// </remarks>
			[EnumDisplayNameLoc(LocalizedStrings.TrailingStopLimitKey)]
			TrailingStopLimit,

			/// <summary>
			/// Волатильность.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.VolatilityKey)]
			Volatility,

			/// <summary>
			/// Используется для дельта заявок.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str1658Key)]
			Empty,

			/// <summary>
			/// Используется для дельта-нейтральных типов заявок.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2319Key)]
			Default,

			/// <summary>
			/// Изменяемый на шаг цены.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2320Key)]
			Scale,

			/// <summary>
			/// С рыночной ценой при исполнении условия.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.MarketOnTouchKey)]
			MarketIfTouched,

			/// <summary>
			/// С заданной ценой при исполнении условия.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.LimitOnTouchKey)]
			LimitIfTouched
		}

		/// <summary>
		/// Режимы заявок типа OCA (One-Cancels All).
		/// </summary>
		public enum OcaTypes
		{
			/// <summary>
			/// Отменить все оставшиеся блоки.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.CancelAllKey)]
			CancelAll = 1,

			/// <summary>
			/// Оставшиеся заявки пропорционально уменьшить на размер блока.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2324Key)]
			ReduceWithBlock = 2,

			/// <summary>
			/// Оставшиеся заявки пропорционально уменьшить на размер вне блока.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2325Key)]
			ReduceWithNoBlock = 3
		}

		/// <summary>
		/// Настройки OCA (One-Cancels All).
		/// </summary>
		public class OcaCondition : BaseCondition
		{
			internal OcaCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Идентификатор группы.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.GroupIdKey)]
			[DescriptionLoc(LocalizedStrings.GroupIdKey, true)]
			public string Group
			{
				get { return TryGetValue<string>("OcaGroup"); }
				set { SetValue("OcaGroup", value); }
			}

			/// <summary>
			/// Тип группы.
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
		/// Условия для активации стоп-заявок.
		/// </summary>
		public enum TriggerMethods
		{
			/// <summary>
			/// Для NASDAQ <see cref="SecurityTypes.Stock"/> и US <see cref="SecurityTypes.Option"/> используется условие <see cref="DoubleBidAsk"/>.
			/// Иначе, используется условие <see cref="BidAsk"/>.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2319Key)]
			Default = 0,

			/// <summary>
			/// Двойное превышение или понижение текущей лучшей цены перед стоп-ценой.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2330Key)]
			DoubleBidAsk = 1,

			/// <summary>
			/// Превышение или понижение цены последней сделки перед стоп-ценой.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2331Key)]
			Last = 2,

			/// <summary>
			/// Двойное превышение или понижение цены последней сделки перед стоп-ценой.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2332Key)]
			DoubleLast = 3,

			/// <summary>
			/// Превышение или понижение текущей лучшей цены перед стоп-ценой.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str273Key)]
			BidAsk = 4,

			/// <summary>
			/// Превышение или понижение текущей лучшей цены или цены последней сделки перед стоп-ценой.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2333Key)]
			LastOrBidAsk = 7,

			/// <summary>
			/// Превышение или понижение середины спреда перед стоп-ценой.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str500Key)]
			MidpointMethod = 8
		}

		/// <summary>
		/// Описания типа трейдера по правилу 80A.
		/// </summary>
		public enum AgentDescriptions
		{
			/// <summary>
			/// Частный трейдер.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2334Key)]
			Individual,

			/// <summary>
			/// Агенство.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.AgencyKey)]
			Agency,

			/// <summary>
			/// Агенство или другой тип.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2336Key)]
			AgentOtherMember,

			/// <summary>
			/// Частный PTIA.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2337Key)]
			IndividualPTIA,

			/// <summary>
			/// Агенство PTIA.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2338Key)]
			AgencyPTIA,

			/// <summary>
			/// Агенство или другой тип PTIA.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2339Key)]
			AgentOtherMemberPTIA,

			/// <summary>
			/// Частный PT.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2340Key)]
			IndividualPT,

			/// <summary>
			/// Агенство PT.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2341Key)]
			AgencyPT,

			/// <summary>
			/// Агенство или другой тип PT.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2342Key)]
			AgentOtherMemberPT,
		}

		/// <summary>
		/// Методы автоматических расчетов объемов для группы счетов.
		/// </summary>
		public enum FinancialAdvisorAllocations
		{
			/// <summary>
			/// Процентное изменение.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2343Key)]
			PercentChange,

			/// <summary>
			/// Через свободные денежные средства плюс заемные.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.EquityKey)]
			AvailableEquity,

			/// <summary>
			/// Через свободные денежные средства.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2345Key)]
			NetLiquidity,

			/// <summary>
			/// Равный объем.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.VolumeKey)]
			EqualQuantity,
		}

		/// <summary>
		/// Найстройки для автоматического расчета объема заявок.
		/// </summary>
		public class FinancialAdvisorCondition : BaseCondition
		{
			internal FinancialAdvisorCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Группа.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.GroupKey)]
			[DescriptionLoc(LocalizedStrings.GroupKey, true)]
			public string Group
			{
				get { return TryGetValue<string>("FAGroup"); }
				set { SetValue("FAGroup", value); }
			}

			/// <summary>
			/// Профайл.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ProfileKey)]
			[DescriptionLoc(LocalizedStrings.ProfileKey, true)]
			public string Profile
			{
				get { return TryGetValue<string>("FAProfile"); }
				set { SetValue("FAProfile", value); }
			}

			/// <summary>
			/// Метод расчета.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2349Key)]
			[DescriptionLoc(LocalizedStrings.Str2349Key, true)]
			public FinancialAdvisorAllocations? Allocation
			{
				get { return TryGetValue<FinancialAdvisorAllocations?>("FAAllocation"); }
				set { SetValue("FAMethod", value); }
			}

			/// <summary>
			/// Процент отношения к заполнению объема.
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
		/// Отправители.
		/// </summary>
		public enum OrderOrigins
		{
			/// <summary>
			/// Клиент.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.ClientKey)]
			Customer,

			/// <summary>
			/// Фирма.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.FirmKey)]
			Firm
		}

		/// <summary>
		/// Торги.
		/// </summary>
		public enum AuctionStrategies
		{
			/// <summary>
			/// Совпадение.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2355Key)]
			AuctionMatch,

			/// <summary>
			/// Лучше.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.BetterKey)]
			AuctionImprovement,

			/// <summary>
			/// Прозрачный.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.TransparentKey)]
			AuctionTransparent
		}

		/// <summary>
		/// Тайм-фреймы волатильности.
		/// </summary>
		public enum VolatilityTimeFrames
		{
			/// <summary>
			/// Дневной.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.DailyKey)]
			Daily = 1,

			/// <summary>
			/// Среднегодовой.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2359Key)]
			Annual = 2
		}

		/// <summary>
		/// Настройки для заявок типа <see cref="ExtendedOrderTypes.Volatility"/>.
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
			/// Обновлять цену лимита при изменении базового актива. 
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2360Key)]
			[DescriptionLoc(LocalizedStrings.Str2361Key)]
			public bool ContinuousUpdate
			{
				get { return GetValue<bool>("ContinuousUpdate"); }
				set { SetValue("ContinuousUpdate", value); }
			}

			/// <summary>
			/// Средняя лучшая цена или лучшая цена.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2362Key)]
			[DescriptionLoc(LocalizedStrings.Str2363Key)]
			public bool? IsAverageBestPrice
			{
				get { return TryGetValue<bool?>("ReferencePriceType"); }
				set { SetValue("ReferencePriceType", value); }
			}

			/// <summary>
			/// Волатильность.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.VolatilityKey)]
			[DescriptionLoc(LocalizedStrings.VolatilityKey, true)]
			public decimal? Volatility
			{
				get { return TryGetValue<decimal?>("Volatility"); }
				set { SetValue("Volatility", value); }
			}

			/// <summary>
			/// Тайм-фрейм волатильности. 
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2365Key)]
			[DescriptionLoc(LocalizedStrings.Str2366Key)]
			public VolatilityTimeFrames? VolatilityTimeFrame
			{
				get { return TryGetValue<VolatilityTimeFrames?>("VolatilityTimeFrame"); }
				set { SetValue("VolatilityTimeFrame", value); }
			}

			/// <summary>
			/// Тип заявки.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str132Key)]
			[DescriptionLoc(LocalizedStrings.Str132Key, true)]
			public OrderTypes OrderType
			{
				get { return GetValue<OrderTypes>("DeltaNeutralOrderType"); }
				set { SetValue("DeltaNeutralOrderType", value); }
			}

			/// <summary>
			/// Расширенный тип заявки.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2368Key)]
			[DescriptionLoc(LocalizedStrings.Str2369Key)]
			public ExtendedOrderTypes? ExtendedOrderType
			{
				get { return TryGetValue<ExtendedOrderTypes?>("DeltaNeutralExtendedOrderType"); }
				set { SetValue("DeltaNeutralExtendedOrderType", value); }
			}

			/// <summary>
			/// Стоп-цена. 
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
			[DescriptionLoc(LocalizedStrings.StopPriceKey, true)]
			public decimal? StopPrice
			{
				get { return TryGetValue<decimal?>("DeltaNeutralAuxPrice"); }
				set { SetValue("DeltaNeutralAuxPrice", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			[DisplayName("ConId")]
			[Description("ConId.")]
			public int ConId
			{
				get { return GetValue<int>("DeltaNeutralConId"); }
				set { SetValue("DeltaNeutralConId", value); }
			}

			/// <summary>
			/// Фирма.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.FirmKey)]
			[DescriptionLoc(LocalizedStrings.FirmKey, true)]
			public string SettlingFirm
			{
				get { return TryGetValue<string>("DeltaNeutralSettlingFirm"); }
				set { SetValue("DeltaNeutralSettlingFirm", value); }
			}

			/// <summary>
			/// Клиринговый счет.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2372Key)]
			[DescriptionLoc(LocalizedStrings.Str2372Key, true)]
			public string ClearingPortfolio
			{
				get { return TryGetValue<string>("DeltaNeutralClearingPortfolio"); }
				set { SetValue("DeltaNeutralClearingPortfolio", value); }
			}

			/// <summary>
			/// Клиринговая цель.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2374Key)]
			[DescriptionLoc(LocalizedStrings.Str2374Key, true)]
			public string ClearingIntent
			{
				get { return TryGetValue<string>("DeltaNeutralClearingIntent"); }
				set { SetValue("DeltaNeutralClearingIntent", value); }
			}

			/// <summary>
			/// Является ли заявка короткой продажей.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2376Key)]
			[DescriptionLoc(LocalizedStrings.Str2377Key)]
			public bool IsShortSale
			{
				get { return GetValue<bool>("DeltaNeutralOpenClose"); }
				set { SetValue("DeltaNeutralOpenClose", value); }
			}

			/// <summary>
			/// Условие для коротких продаж комбинированных ног.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2378Key)]
			[DescriptionLoc(LocalizedStrings.Str2379Key)]
			public ShortSaleCondition ShortSale { get; private set; }
		}

		/// <summary>
		/// Типы коротких продаж комбинированных ног.
		/// </summary>
		public enum ShortSaleSlots
		{
			/// <summary>
			/// Частрый трейдер или нет короткая нога.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str1658Key)]
			Unapplicable,

			/// <summary>
			/// Клиринговый брокер.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.ClearingKey)]
			ClearingBroker,

			/// <summary>
			/// Другое.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2381Key)]
			ThirdParty
		}

		/// <summary>
		/// Условие для коротких продаж комбинированных ног.
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
			/// Тип короткой продажи комбинированных ног.
			/// </summary>
			public ShortSaleSlots Slot
			{
				get { return GetValue<ShortSaleSlots>(_prefix + "ShortSaleSlot"); }
				set { SetValue(_prefix + "ShortSaleSlot", value); }
			}

			/// <summary>
			/// Уточнение типа короткой продажи комбинированных ног.
			/// </summary>
			/// <remarks>
			/// Используется при <see cref="Slot"/> равным <see cref="ShortSaleSlots.ThirdParty"/>.
			/// </remarks>
			public string Location
			{
				get { return TryGetValue<string>(_prefix + "ShortSaleSlotLocation"); }
				set { SetValue(_prefix + "ShortSaleSlotLocation", value); }
			}

			/// <summary>
			/// Exempt Code for Short Sale Exemption Orders
			/// </summary>
			public int ExemptCode
			{
				get { return GetValue<int>(_prefix + "ExemptCode"); }
				set { SetValue(_prefix + "ExemptCode", value); }
			}

			/// <summary>
			/// Является ли заявка открывающей или закрывающей.
			/// </summary>
			public bool? IsOpenOrClose
			{
				get { return TryGetValue<bool?>(_prefix + "OpenClose"); }
				set { SetValue(_prefix + "OpenClose", value); }
			}
		}

		/// <summary>
		/// Настройки EFP заявок.
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
			/// Базовые пункты.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2382Key)]
			[DescriptionLoc(LocalizedStrings.Str2382Key, true)]
			public decimal BasisPoints
			{
				get { return GetValue<decimal>("ComboBasisPoints"); }
				set { SetValue("ComboBasisPoints", value); }
			}

			/// <summary>
			/// Тип базовых пунктов.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2382Key)]
			[DescriptionLoc(LocalizedStrings.Str2382Key, true)]
			public int BasisPointsType
			{
				get { return GetValue<int>("ComboBasisPointsType"); }
				set { SetValue("ComboBasisPointsType", value); }
			}

			/// <summary>
			/// Описание ног.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2386Key)]
			[DescriptionLoc(LocalizedStrings.Str2386Key, true)]
			public string LegsDescription
			{
				get { return TryGetValue<string>("ComboLegsDescription"); }
				set { SetValue("ComboLegsDescription", value); }
			}

			/// <summary>
			/// Цены ног.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2388Key)]
			[DescriptionLoc(LocalizedStrings.Str2388Key, true)]
			public IEnumerable<decimal?> Legs
			{
				get { return TryGetValue<IEnumerable<decimal?>>("ComboLegs"); }
				set { SetValue("ComboLegs", value); }
			}

			/// <summary>
			/// Условие для коротких продаж.
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
		/// Настройки для заявок, отправляемых в Smart биржу.
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
			/// Диапазон сдвига цены заявки.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2391Key)]
			[DescriptionLoc(LocalizedStrings.Str2392Key)]
			public decimal DiscretionaryAmount
			{
				get { return GetValue<decimal>("DiscretionaryAmount"); }
				set { SetValue("DiscretionaryAmount", value); }
			}

			/// <summary>
			/// Электронные торги.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ElectronicTradingKey)]
			[DescriptionLoc(LocalizedStrings.ElectronicTradingKey, true)]
			public bool ETradeOnly
			{
				get { return GetValue<bool>("ETradeOnly"); }
				set { SetValue("ETradeOnly", value); }
			}

			/// <summary>
			/// Котировки фирмы.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2395Key)]
			[DescriptionLoc(LocalizedStrings.Str2395Key, true)]
			public bool FirmQuoteOnly
			{
				get { return GetValue<bool>("FirmQuoteOnly"); }
				set { SetValue("FirmQuoteOnly", value); }
			}

			/// <summary>
			/// Максимальный сдвиг от лучших пар.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2397Key)]
			[DescriptionLoc(LocalizedStrings.Str2398Key)]
			public decimal? NbboPriceCap
			{
				get { return TryGetValue<decimal?>("NbboPriceCap"); }
				set { SetValue("NbboPriceCap", value); }
			}

			/// <summary>
			/// Удерживать в стакане.
			/// </summary>
			/// <remarks>
			/// Только для биржи IBDARK.
			/// </remarks>
			[DisplayNameLoc(LocalizedStrings.Str2399Key)]
			[DescriptionLoc(LocalizedStrings.Str2400Key)]
			public bool NotHeld
			{
				get { return GetValue<bool>("NotHeld"); }
				set { SetValue("NotHeld", value); }
			}

			/// <summary>
			/// Прямая отправка ASX заявок.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2401Key)]
			[DescriptionLoc(LocalizedStrings.Str2402Key)]
			public bool OptOutSmartRouting
			{
				get { return GetValue<bool>("OptOutSmartRouting"); }
				set { SetValue("OptOutSmartRouting", value); }
			}

			/// <summary>
			/// Параметры.
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
		/// Условие для изменяемой заявки.
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
			/// split order into X buckets
			/// </summary>
			public int? InitLevelSize
			{
				get { return TryGetValue<int?>("ScaleInitLevelSize"); }
				set { SetValue("ScaleInitLevelSize", value); }
			}

			/// <summary>
			/// split order so each bucket is of the size X
			/// </summary>
			public int? SubsLevelSize
			{
				get { return TryGetValue<int?>("ScaleSubsLevelSize"); }
				set { SetValue("ScaleSubsLevelSize", value); }
			}

			/// <summary>
			/// price increment per bucket
			/// </summary>
			public decimal? PriceIncrement
			{
				get { return TryGetValue<decimal?>("ScalePriceIncrement"); }
				set { SetValue("ScalePriceIncrement", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public decimal? PriceAdjustValue
			{
				get { return TryGetValue<decimal?>("ScalePriceAdjustValue"); }
				set { SetValue("ScalePriceAdjustValue", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public int PriceAdjustInterval
			{
				get { return GetValue<int>("ScalePriceAdjustInterval"); }
				set { SetValue("ScalePriceAdjustInterval", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public decimal? ProfitOffset
			{
				get { return TryGetValue<decimal?>("ScaleProfitOffset"); }
				set { SetValue("ScaleProfitOffset", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public bool AutoReset
			{
				get { return GetValue<bool>("ScaleAutoReset"); }
				set { SetValue("ScaleAutoReset", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public int? InitPosition
			{
				get { return TryGetValue<int?>("ScaleInitPosition"); }
				set { SetValue("ScaleInitPosition", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public int? InitFillQty
			{
				get { return TryGetValue<int?>("ScaleInitFillQty"); }
				set { SetValue("ScaleInitFillQty", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public bool RandomPercent
			{
				get { return GetValue<bool>("ScaleRandomPercent"); }
				set { SetValue("ScaleRandomPercent", value); }
			}

			/// <summary>
			/// 
			/// </summary>
			public string Table
			{
				get { return TryGetValue<string>("Table"); }
				set { SetValue("Table", value); }
			}
		}

		/// <summary>
		/// Типы параметров для хеджирования.
		/// </summary>
		public enum HedgeTypes
		{
			/// <summary>
			/// Дельта.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str300Key)]
			Delta,

			/// <summary>
			/// Бета.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str339Key)]
			Beta,

			/// <summary>
			/// Валюта.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str250Key)]
			FX,

			/// <summary>
			/// Пара.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.PairKey)]
			Pair
		}

		/// <summary>
		/// Условие для хедж-заявок.
		/// </summary>
		public class HedgeCondition : BaseCondition
		{
			internal HedgeCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Тип параметра для хеджирования.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ParameterTypeKey)]
			[DescriptionLoc(LocalizedStrings.Str2406Key)]
			public HedgeTypes? Type
			{
				get { return TryGetValue<HedgeTypes?>("HedgeType"); }
				set { SetValue("HedgeType", value); }
			}

			/// <summary>
			/// Параметр.
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
		/// Условие для алго-заявок.
		/// </summary>
		public class AlgoCondition : BaseCondition
		{
			internal AlgoCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Стратегия.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.StrategyKey)]
			[DescriptionLoc(LocalizedStrings.StrategyKey, true)]
			public string Strategy
			{
				get { return TryGetValue<string>("AlgoStrategy"); }
				set { SetValue("AlgoStrategy", value); }
			}

			/// <summary>
			/// Параметры.
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
		/// Цели клиринга.
		/// </summary>
		public enum ClearingIntents
		{
			/// <summary>
			/// Брокер.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.BrokerKey)]
			Broker,

			/// <summary>
			/// Другое.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2381Key)]
			Away,

			/// <summary>
			/// После торговое размещение.
			/// </summary>
			[EnumDisplayNameLoc(LocalizedStrings.Str2411Key)]
			PostTradeAllocation
		}

		/// <summary>
		/// Условие для клиринговой информации.
		/// </summary>
		/// <remarks>
		/// Только для институциональных клиентов.
		/// </remarks>
		public class ClearingCondition : BaseCondition
		{
			internal ClearingCondition(IBOrderCondition condition)
				: base(condition)
			{
				Intent = ClearingIntents.Broker;
			}

			/// <summary>
			/// Счет.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.AccountKey)]
			[DescriptionLoc(LocalizedStrings.AccountKey, true)]
			public string Portfolio
			{
				get { return TryGetValue<string>("Portfolio"); }
				set { SetValue("Portfolio", value); }
			}

			/// <summary>
			/// Фирма.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.FirmKey)]
			[DescriptionLoc(LocalizedStrings.FirmKey, true)]
			public string SettlingFirm
			{
				get { return TryGetValue<string>("SettlingFirm"); }
				set { SetValue("SettlingFirm", value); }
			}

			/// <summary>
			/// Клиринговый счет.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2414Key)]
			[DescriptionLoc(LocalizedStrings.Str2372Key, true)]
			public string ClearingPortfolio
			{
				get { return TryGetValue<string>("ClearingPortfolio"); }
				set { SetValue("ClearingPortfolio", value); }
			}

			/// <summary>
			/// Цель клиринга.
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
		/// Условие для заявок типа <see cref="OrderTypes.Execute"/>.
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
			/// Исполнить опцион.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2417Key)]
			[DescriptionLoc(LocalizedStrings.Str2418Key)]
			public bool IsExercise
			{
				get { return GetValue<bool>("OptionIsExercise"); }
				set { SetValue("OptionIsExercise", value); }
			}

			/// <summary>
			/// Заместить действие.
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
		/// Условие для заявок GTC.
		/// </summary>
		public class ActiveCondition : BaseCondition
		{
			internal ActiveCondition(IBOrderCondition condition)
				: base(condition)
			{
			}

			/// <summary>
			/// Время старта.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str2421Key)]
			[DescriptionLoc(LocalizedStrings.Str2422Key)]
			public DateTimeOffset? Start
			{
				get { return TryGetValue<DateTimeOffset?>("Start"); }
				set { SetValue("Start", value); }
			}

			/// <summary>
			/// Время окончания.
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
		/// Создать <see cref="IBOrderCondition"/>.
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
		/// Расширенное условие.
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
		/// Стоп-цена.
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
		/// По открытию торгов.
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
		/// Настройки OCA (One-Cancels All).
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2428Key)]
		[DescriptionLoc(LocalizedStrings.Str2429Key)]
		[ExpandableObject]
		public OcaCondition Oca { get; private set; }

		/// <summary>
		/// Отправлять заявку в TWS.
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
		/// Идентификатор родительской заявки.
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
		/// Разбивать объем заявки.
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
		/// По лучшей цене.
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
		/// Условие активации стоп-заявки.
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
		/// Позволить активировать стоп-заявку вне торгового времени.
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
		/// Прятать заявку в стакане.
		/// </summary>
		/// <remarks>
		/// Возможно только при отправке заявки на биржу ISLAND.
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
		/// Активировать после заданного времени.
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
		/// Отменять заявки с некорректной ценой. 
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
		/// Идентификатор трейдера.
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
		/// Ожидать появления необходимого объема.
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
		/// Минимальный объем заявки.
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
		/// Сдвиг в цене для заявки типа <see cref="ExtendedOrderTypes.Relative"/>.
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
		/// Цена активации скользящего стопа.
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
		/// Объем скользящего стопа, выраженный в процентах.
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
		/// Найстройки для автоматического расчета объема заявок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2459Key)]
		[DescriptionLoc(LocalizedStrings.Str2460Key)]
		[ExpandableObject]
		public FinancialAdvisorCondition FinancialAdvisor { get; private set; }

		/// <summary>
		/// Является ли заявка открывающей или закрывающей.
		/// </summary>
		/// <remarks>
		/// Только для институциональных клиентов.
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
		/// Отправитель.
		/// </summary>
		/// <remarks>
		/// Только для институциональных клиентов.
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
		/// Условие для коротких продаж комбинированных ног.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2378Key)]
		[DescriptionLoc(LocalizedStrings.Str2379Key)]
		[ExpandableObject]
		public ShortSaleCondition ShortSale { get; private set; }

		/// <summary>
		/// Торги.
		/// </summary>
		/// <remarks>Только для биржи BOX.</remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2464Key)]
		[DescriptionLoc(LocalizedStrings.Str2465Key)]
		public AuctionStrategies? AuctionStrategy
		{
			get { return (AuctionStrategies?)Parameters.TryGetValue("AuctionStrategy"); }
			set { Parameters["AuctionStrategy"] = value; }
		}

		/// <summary>
		/// Стартовая цена.
		/// </summary>
		/// <remarks>Только для биржи BOX.</remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2466Key)]
		[DescriptionLoc(LocalizedStrings.Str2467Key)]
		public decimal? StartingPrice
		{
			get { return (decimal?)Parameters.TryGetValue("StartingPrice"); }
			set { Parameters["StartingPrice"] = value; }
		}

		/// <summary>
		/// Цена базового актива.
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
		/// Дельта базового актива.
		/// </summary>
		/// <remarks>Только для биржи BOX.</remarks>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2470Key)]
		[DescriptionLoc(LocalizedStrings.Str2470Key, true)]
		public decimal? Delta
		{
			get { return (decimal?)Parameters.TryGetValue("Delta"); }
			set { Parameters["Delta"] = value; }
		}

		/// <summary>
		/// Минимальная цена базового актива.
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
		/// Максимальная цена базового актива.
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
		/// Настройки для заявок типа <see cref="ExtendedOrderTypes.Volatility"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2476Key)]
		[DescriptionLoc(LocalizedStrings.Str2477Key)]
		[ExpandableObject]
		public VolatilityCondition Volatility { get; private set; }

		/// <summary>
		/// Настройки для заявок, отправляемых в Smart биржу.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2478Key)]
		[DescriptionLoc(LocalizedStrings.Str2479Key)]
		[ExpandableObject]
		public SmartRoutingCondition SmartRouting { get; private set; }

		/// <summary>
		/// Настройки EFP заявок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2480Key)]
		[DescriptionLoc(LocalizedStrings.Str2481Key)]
		[ExpandableObject]
		public ComboCondition Combo { get; private set; }

		/// <summary>
		/// Условие для изменяемой заявки.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2482Key)]
		[DescriptionLoc(LocalizedStrings.Str2483Key)]
		[ExpandableObject]
		public ScaleCondition Scale { get; private set; }

		/// <summary>
		/// Условие для клиринговой информации.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2484Key)]
		[DescriptionLoc(LocalizedStrings.Str2485Key)]
		[ExpandableObject]
		public ClearingCondition Clearing { get; private set; }

		/// <summary>
		/// Условие для алго-заявок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2486Key)]
		[DescriptionLoc(LocalizedStrings.Str2487Key)]
		[ExpandableObject]
		public AlgoCondition Algo { get; private set; }

		/// <summary>
		/// Вовзращать для заявки информацию о комиссии и марже.
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
		/// Идентификатор алгоритма.
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
		/// Дополнительные параметры.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str225Key)]
		[DescriptionLoc(LocalizedStrings.Str2492Key)]
		public IEnumerable<Tuple<string, string>> MiscOptions
		{
			get { return (IEnumerable<Tuple<string, string>>)Parameters.TryGetValue("MiscOptions") ?? Enumerable.Empty<Tuple<string, string>>(); }
			set { Parameters["MiscOptions"] = value; }
		}

		/// <summary>
		/// Условие для хедж-заявок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2493Key)]
		[DescriptionLoc(LocalizedStrings.Str2494Key)]
		[ExpandableObject]
		public HedgeCondition Hedge { get; private set; }

		/// <summary>
		/// Условие для заявок типа <see cref="OrderTypes.Execute"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2495Key)]
		[DescriptionLoc(LocalizedStrings.Str2496Key)]
		[ExpandableObject]
		public OptionExerciseCondition OptionExercise { get; private set; }

		/// <summary>
		/// Условие для GTC заявки.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2497Key)]
		[DescriptionLoc(LocalizedStrings.Str2498Key)]
		[ExpandableObject]
		public ActiveCondition Active { get; private set; }
	}
}