namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Поля маркет-данных первого уровня.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum Level1Fields
	{
		/// <summary>
		/// Цена открытия.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str79Key)]
		OpenPrice,

		/// <summary>
		/// Наибольшая цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str287Key)]
		HighPrice,

		/// <summary>
		/// Наименьшая цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str288Key)]
		LowPrice,

		/// <summary>
		/// Цена закрытия.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		ClosePrice,

		/// <summary>
		/// Последняя сделка.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str289Key)]
		LastTrade,

		/// <summary>
		/// Стоимость шага.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str290Key)]
		StepPrice,

		/// <summary>
		/// Лучший бид.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str291Key)]
		BestBid,

		/// <summary>
		/// Лучший оффер.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str292Key)]
		BestAsk,

		/// <summary>
		/// Волатильность (подразумеваемая).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str293Key)]
		ImpliedVolatility,

		/// <summary>
		/// Теоретическая цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str294Key)]
		TheorPrice,

		/// <summary>
		/// Открытый интерес.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str150Key)]
		OpenInterest,

		/// <summary>
		/// Минимальная цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str83Key)]
		MinPrice,

		/// <summary>
		/// Максимальная цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str81Key)]
		MaxPrice,

		/// <summary>
		/// Объем бидов.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str295Key)]
		BidsVolume,

		/// <summary>
		/// Количество бидов.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str296Key)]
		BidsCount,

		/// <summary>
		/// Объем офферов.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str297Key)]
		AsksVolume,

		/// <summary>
		/// Количество офферов.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str298Key)]
		AsksCount,

		/// <summary>
		/// Волатильность (историческая).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str299Key)]
		HistoricalVolatility,

		/// <summary>
		/// Дельта.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.DeltaKey)]
		Delta,

		/// <summary>
		/// Гамма.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.GammaKey)]
		Gamma,

		/// <summary>
		/// Вега.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.VegaKey)]
		Vega,

		/// <summary>
		/// Тета.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ThetaKey)]
		Theta,

		/// <summary>
		/// ГО (покупка).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str304Key)]
		MarginBuy,

		/// <summary>
		/// ГО (продажа).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str305Key)]
		MarginSell,

		/// <summary>
		/// Минимальный шаг цены.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str306Key)]
		PriceStep,

		/// <summary>
		/// Минимальный шаг объема.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str307Key)]
		VolumeStep,

		/// <summary>
		/// Расширенная информация.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		ExtensionInfo,

		/// <summary>
		/// Состояние.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.StateKey)]
		State,

		/// <summary>
		/// Цена последней сделки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str308Key)]
		LastTradePrice,

		/// <summary>
		/// Объем последней сделки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str309Key)]
		LastTradeVolume,

		/// <summary>
		/// Объем за сессию.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str310Key)]
		Volume,

		/// <summary>
		/// Средняя цена за сессию.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str311Key)]
		AveragePrice,

		/// <summary>
		/// Рассчетная цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str312Key)]
		SettlementPrice,

		/// <summary>
		/// Change,%.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Change,%")]
		Change,

		/// <summary>
		/// Лучшая цена покупки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str313Key)]
		BestBidPrice,

		/// <summary>
		/// Лучшая объем покупки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str314Key)]
		BestBidVolume,

		/// <summary>
		/// Лучшая цена продажи.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str315Key)]
		BestAskPrice,

		/// <summary>
		/// Лучшая объем продажи.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str316Key)]
		BestAskVolume,

		/// <summary>
		/// Ро.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.RhoKey)]
		Rho,

		/// <summary>
		/// Накопленный купонный доход (НКД).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str318Key)]
		AccruedCouponIncome,

		/// <summary>
		/// Максимальный бид за сессию.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str319Key)]
		HighBidPrice,

		/// <summary>
		/// Минимальный оффер за сессию.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str320Key)]
		LowAskPrice,

		/// <summary>
		/// Доходность.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str321Key)]
		Yield,

		/// <summary>
		/// Время последней сделки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str322Key)]
		LastTradeTime,

		/// <summary>
		/// Количество сделок.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str323Key)]
		TradesCount,

		/// <summary>
		/// Средневзвешенная цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		VWAP,

		/// <summary>
		/// Идентификатор последней сделки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str325Key)]
		LastTradeId,

		/// <summary>
		/// Время лучшего бида.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str326Key)]
		BestBidTime,

		/// <summary>
		/// Время лучшего офера.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str327Key)]
		BestAskTime,

		/// <summary>
		/// Является ли тик восходящим или нисходящим в цене.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str328Key)]
		LastTradeUpDown,

		/// <summary>
		/// Инициатор последней сделки (покупатель или продавец).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str329Key)]
		LastTradeOrigin,

		/// <summary>
		/// Коэфициент объема между лотом и активом.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str330Key)]
		Multiplier,

		/// <summary>
		/// Цена/прибыль.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/E")]
		PriceEarnings,

		/// <summary>
		/// Прогнозируемая цена/прибыль.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Forward P/E")]
		ForwardPriceEarnings,

		/// <summary>
		/// Цена/прибыль (рост).
		/// </summary>
		[EnumMember]
		[EnumDisplayName("PEG")]
		PriceEarningsGrowth,

		/// <summary>
		/// Цена/продажа.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/S")]
		PriceSales,

		/// <summary>
		/// Цена/покупка.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/B")]
		PriceBook,

		/// <summary>
		/// Цена/деньги.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/CF")]
		PriceCash,

		/// <summary>
		/// Цена/деньги (свободные).
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/FCF")]
		PriceFreeCash,

		/// <summary>
		/// Выплаты.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Payout")]
		Payout,

		/// <summary>
		/// Количество акций.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str331Key)]
		SharesOutstanding,

		/// <summary>
		/// Shares Float.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Shares Float")]
		SharesFloat,

		/// <summary>
		/// Float Short.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Float Short")]
		FloatShort,

		/// <summary>
		/// Short.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Short")]
		ShortRatio,

		/// <summary>
		/// Рентабельность активов.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ROA")]
		ReturnOnAssets,

		/// <summary>
		/// Рентабельность капитала.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ROE")]
		ReturnOnEquity,

		/// <summary>
		/// Возврат инвестиций.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ROI")]
		ReturnOnInvestment,

		/// <summary>
		/// Ликвидность (текущая).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str332Key)]
		CurrentRatio,

		/// <summary>
		/// Ликвидность (мгновенная).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str333Key)]
		QuickRatio,

		/// <summary>
		/// Капитал (долгосрочный долг).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str334Key)]
		LongTermDebtEquity,

		/// <summary>
		/// Капитал (долг).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str335Key)]
		TotalDebtEquity,

		/// <summary>
		/// Маржа активов (гросс).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str336Key)]
		GrossMargin,

		/// <summary>
		/// Маржа активов.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str337Key)]
		OperatingMargin,

		/// <summary>
		/// Маржа прибыли.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str338Key)]
		ProfitMargin,

		/// <summary>
		/// Бета.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.BetaKey)]
		Beta,

		/// <summary>
		/// ATR.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ATR")]
		AverageTrueRange,

		/// <summary>
		/// Волатильность (неделя).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str340Key)]
		HistoricalVolatilityWeek,

		/// <summary>
		/// Волатильность (месяц).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str341Key)]
		HistoricalVolatilityMonth,

		/// <summary>
		/// Системная информация.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str342Key)]
		IsSystem,

		/// <summary>
		/// Количество знаков в цене после запятой.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.DecimalsKey)]
		Decimals
	}

	/// <summary>
	/// Сообщение, содержащее первый уровень маркет-данных.
	/// </summary>
	[DataContract]
	[Serializable]
	public class Level1ChangeMessage : BaseChangeMessage<Level1Fields>
	{
		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Создать <see cref="Level1ChangeMessage"/>.
		/// </summary>
		public Level1ChangeMessage()
			: base(MessageTypes.Level1Change)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var msg = new Level1ChangeMessage
			{
				LocalTime = LocalTime,
				SecurityId = SecurityId,
				ServerTime = ServerTime,
			};

			msg.Changes.AddRange(Changes);

			return msg;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0},Changes={1}".Put(SecurityId, Changes.Select(c => c.ToString()).Join(","));
		}
	}
}