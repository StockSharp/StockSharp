namespace StockSharp.Messages
{
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Поля маркет-данных первого уровня.
	/// </summary>
	public enum Level1Fields
	{
		/// <summary>
		/// Цена открытия.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str79Key)]
		OpenPrice,

		/// <summary>
		/// Наибольшая цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str287Key)]
		HighPrice,

		/// <summary>
		/// Наименьшая цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str288Key)]
		LowPrice,

		/// <summary>
		/// Цена закрытия.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		ClosePrice,

		/// <summary>
		/// Последняя сделка.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str289Key)]
		LastTrade,

		/// <summary>
		/// Стоимость шага.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str290Key)]
		StepPrice,

		/// <summary>
		/// Лучший бид.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str291Key)]
		BestBid,

		/// <summary>
		/// Лучший оффер.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str292Key)]
		BestAsk,

		/// <summary>
		/// Волатильность (подразумеваемая).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str293Key)]
		ImpliedVolatility,

		/// <summary>
		/// Теоретическая цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str294Key)]
		TheorPrice,

		/// <summary>
		/// Открытый интерес.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str150Key)]
		OpenInterest,

		/// <summary>
		/// Минимальная цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str83Key)]
		MinPrice,

		/// <summary>
		/// Максимальная цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str81Key)]
		MaxPrice,

		/// <summary>
		/// Объем бидов.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str295Key)]
		BidsVolume,

		/// <summary>
		/// Количество бидов.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str296Key)]
		BidsCount,

		/// <summary>
		/// Объем офферов.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str297Key)]
		AsksVolume,

		/// <summary>
		/// Количество офферов.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str298Key)]
		AsksCount,

		/// <summary>
		/// Волатильность (историческая).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str299Key)]
		HistoricalVolatility,

		/// <summary>
		/// Дельта.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str300Key)]
		Delta,

		/// <summary>
		/// Гамма.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str301Key)]
		Gamma,

		/// <summary>
		/// Вега.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str302Key)]
		Vega,

		/// <summary>
		/// Тета.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str303Key)]
		Theta,

		/// <summary>
		/// ГО (покупка).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str304Key)]
		MarginBuy,

		/// <summary>
		/// ГО (продажа).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str305Key)]
		MarginSell,

		/// <summary>
		/// Минимальный шаг цены.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str306Key)]
		PriceStep,

		/// <summary>
		/// Минимальный шаг объема.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str307Key)]
		VolumeStep,

		/// <summary>
		/// Расширенная информация.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		ExtensionInfo,

		/// <summary>
		/// Состояние.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.StateKey)]
		State,

		/// <summary>
		/// Цена последней сделки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str308Key)]
		LastTradePrice,

		/// <summary>
		/// Объем последней сделки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str309Key)]
		LastTradeVolume,

		/// <summary>
		/// Объем за сессию.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str310Key)]
		Volume,

		/// <summary>
		/// Средняя цена за сессию.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str311Key)]
		AveragePrice,

		/// <summary>
		/// Рассчетная цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str312Key)]
		SettlementPrice,

		/// <summary>
		/// Change,%.
		/// </summary>
		[EnumDisplayName("Change,%")]
		Change,

		/// <summary>
		/// Лучшая цена покупки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str313Key)]
		BestBidPrice,

		/// <summary>
		/// Лучшая объем покупки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str314Key)]
		BestBidVolume,

		/// <summary>
		/// Лучшая цена продажи.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str315Key)]
		BestAskPrice,

		/// <summary>
		/// Лучшая объем продажи.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str316Key)]
		BestAskVolume,

		/// <summary>
		/// Ро.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str317Key)]
		Rho,

		/// <summary>
		/// Накопленный купонный доход (НКД).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str318Key)]
		AccruedCouponIncome,

		/// <summary>
		/// Максимальный бид за сессию.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str319Key)]
		HighBidPrice,

		/// <summary>
		/// Минимальный оффер за сессию.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str320Key)]
		LowAskPrice,

		/// <summary>
		/// Доходность.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str321Key)]
		Yield,

		/// <summary>
		/// Время последней сделки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str322Key)]
		LastTradeTime,

		/// <summary>
		/// Количество сделок.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str323Key)]
		TradesCount,

		/// <summary>
		/// Средневзвешенная цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		VWAP,

		/// <summary>
		/// Идентификатор последней сделки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str325Key)]
		LastTradeId,

		/// <summary>
		/// Время лучшего бида.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str326Key)]
		BestBidTime,

		/// <summary>
		/// Время лучшего офера.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str327Key)]
		BestAskTime,

		/// <summary>
		/// Является ли тик восходящим или нисходящим в цене.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str328Key)]
		LastTradeUpDown,

		/// <summary>
		/// Инициатор последней сделки (покупатель или продавец).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str329Key)]
		LastTradeOrigin,

		/// <summary>
		/// Коэфициент объема между лотом и активом.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str330Key)]
		Multiplier,

		/// <summary>
		/// Цена/прибыль.
		/// </summary>
		[EnumDisplayName("P/E")]
		PriceEarnings,

		/// <summary>
		/// Прогнозируемая цена/прибыль.
		/// </summary>
		[EnumDisplayName("Forward P/E")]
		ForwardPriceEarnings,

		/// <summary>
		/// Цена/прибыль (рост).
		/// </summary>
		[EnumDisplayName("PEG")]
		PriceEarningsGrowth,

		/// <summary>
		/// Цена/продажа.
		/// </summary>
		[EnumDisplayName("P/S")]
		PriceSales,

		/// <summary>
		/// Цена/покупка.
		/// </summary>
		[EnumDisplayName("P/B")]
		PriceBook,

		/// <summary>
		/// Цена/деньги.
		/// </summary>
		[EnumDisplayName("P/CF")]
		PriceCash,

		/// <summary>
		/// Цена/деньги (свободные).
		/// </summary>
		[EnumDisplayName("P/FCF")]
		PriceFreeCash,

		/// <summary>
		/// Выплаты.
		/// </summary>
		[EnumDisplayName("Payout")]
		Payout,

		/// <summary>
		/// Количество акций.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str331Key)]
		SharesOutstanding,

		/// <summary>
		/// Shares Float.
		/// </summary>
		[EnumDisplayName("Shares Float")]
		SharesFloat,

		/// <summary>
		/// Float Short.
		/// </summary>
		[EnumDisplayName("Float Short")]
		FloatShort,

		/// <summary>
		/// Short.
		/// </summary>
		[EnumDisplayName("Short")]
		ShortRatio,

		/// <summary>
		/// Рентабельность активов.
		/// </summary>
		[EnumDisplayName("ROA")]
		ReturnOnAssets,

		/// <summary>
		/// Рентабельность капитала.
		/// </summary>
		[EnumDisplayName("ROE")]
		ReturnOnEquity,

		/// <summary>
		/// Возврат инвестиций.
		/// </summary>
		[EnumDisplayName("ROI")]
		ReturnOnInvestment,

		/// <summary>
		/// Ликвидность (текущая).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str332Key)]
		CurrentRatio,

		/// <summary>
		/// Ликвидность (мгновенная).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str333Key)]
		QuickRatio,

		/// <summary>
		/// Капитал (долгосрочный долг).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str334Key)]
		LongTermDebtEquity,

		/// <summary>
		/// Капитал (долг).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str335Key)]
		TotalDebtEquity,

		/// <summary>
		/// Маржа активов (гросс).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str336Key)]
		GrossMargin,

		/// <summary>
		/// Маржа активов.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str337Key)]
		OperatingMargin,

		/// <summary>
		/// Маржа прибыли.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str338Key)]
		ProfitMargin,

		/// <summary>
		/// Бета.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str339Key)]
		Beta,

		/// <summary>
		/// ATR.
		/// </summary>
		[EnumDisplayName("ATR")]
		AverageTrueRange,

		/// <summary>
		/// Волатильность (неделя).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str340Key)]
		HistoricalVolatilityWeek,

		/// <summary>
		/// Волатильность (месяц).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str341Key)]
		HistoricalVolatilityMonth,

		/// <summary>
		/// Системная информация.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str342Key)]
		IsSystem
	}

	/// <summary>
	/// Сообщение, содержащее первый уровень маркет-данных.
	/// </summary>
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
		/// Создать копию объекта.
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