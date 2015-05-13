namespace StockSharp.ETrade
{
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Тип условной заявки ETrade.
	/// </summary>
	public enum ETradeStopTypes
	{
		/// <summary>После достижения стоп-цены автоматически выставляется рыночная заявка.</summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
		StopMarket,

		/// <summary>После достижения стоп-цены автоматически выставляется лимитная заявка.</summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1733Key)]
		StopLimit,
	}

	/// <summary>
	/// Условие заявок, специфичных для <see cref="ETrade"/>.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "ETrade")]
	public class ETradeOrderCondition : OrderCondition
	{
		private const string _keyStopType = "StopType";
		private const string _keyStopPrice = "StopPrice";

		/// <summary>
		/// Создать <see cref="ETradeOrderCondition"/>.
		/// </summary>
		public ETradeOrderCondition()
		{
			StopType = ETradeStopTypes.StopLimit;
			StopPrice = 0;
		}

		/// <summary>
		/// Тип стопа.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str1691Key)]
		public ETradeStopTypes StopType
		{
			get { return (ETradeStopTypes)Parameters[_keyStopType]; }
			set { Parameters[_keyStopType] = value; }
		}

		/// <summary>
		/// Стоп-цена.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str1693Key)]
		public decimal StopPrice
		{
			get { return (decimal)Parameters[_keyStopPrice]; }
			set { Parameters[_keyStopPrice] = value; }
		}
	}
}