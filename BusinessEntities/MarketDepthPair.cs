namespace StockSharp.BusinessEntities
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Пара котировок.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	public class MarketDepthPair
	{
		private readonly bool _isFull;

		/// <summary>
		/// Создать <see cref="MarketDepthPair"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="bid">Бид.</param>
		/// <param name="ask">Оффер.</param>
		public MarketDepthPair(Security security, Quote bid, Quote ask)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (bid != null && bid.OrderDirection != Sides.Buy)
				throw new ArgumentException(LocalizedStrings.Str492);

			if (ask != null && ask.OrderDirection != Sides.Sell)
				throw new ArgumentException(LocalizedStrings.Str493);

			Security = security;
			Bid = bid;
			Ask = ask;

			_isFull = bid != null && ask != null;
		}

		/// <summary>
		/// Инструмент.
		/// </summary>
		public Security Security { get; private set; }

		/// <summary>
		/// Бид.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.BidKey)]
		[DescriptionLoc(LocalizedStrings.Str494Key)]
		public Quote Bid { get; private set; }

		/// <summary>
		/// Оффер.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.AskKey)]
		[DescriptionLoc(LocalizedStrings.Str495Key)]
		public Quote Ask { get; private set; }

		/// <summary>
		/// Размер спреда по цене. Равно <see langword="null"/>, если отсутствует одна из котировок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str496Key)]
		[DescriptionLoc(LocalizedStrings.Str497Key)]
		public decimal? SpreadPrice
		{
			get { return _isFull ? Ask.Security.ShrinkPrice(Ask.Price - Bid.Price) : (decimal?)null; }
		}

		/// <summary>
		/// Размер спреда по объему. Если значение отрицательное, значит лучший оффер имеет больший объем, чем лучший бид.
		/// Равно <see langword="null"/>, если отсутствует одна из котировок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str498Key)]
		[DescriptionLoc(LocalizedStrings.Str499Key)]
		public decimal? SpreadVolume
		{
			get { return _isFull ? (Ask.Volume - Bid.Volume).Abs() : (decimal?)null; }
		}

		/// <summary>
		/// Середина спреда.
		/// Равно <see langword="null"/>, если отсутствует одна из котировок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str500Key)]
		[DescriptionLoc(LocalizedStrings.Str501Key)]
		public decimal? MiddlePrice
		{
			get { return _isFull ? (Bid.Price + SpreadPrice / 2) : null; }
		}

		/// <summary>
		/// Пара котировок содержит <see cref="Bid"/> и <see cref="Ask"/>.
		/// </summary>
		public bool IsFull
		{
			get { return _isFull; }
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{{{0}}} {{{1}}}".Put(Bid, Ask);
		}
	}
}