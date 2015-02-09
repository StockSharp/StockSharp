namespace StockSharp.AlfaDirect
{
	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Условие заявок, специфичных для <see cref="AlfaDirect"/>.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "AlfaDirect")]
	public class AlfaOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="AlfaOrderCondition"/>.
		/// </summary>
		public AlfaOrderCondition()
		{
			StopPrice = Slippage = TargetPrice = Level = 0;
		}

		private const string _keyStopPrice = "StopPrice";

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

		private const string _keySlippage = "Slippage";

		/// <summary>
		/// Проскальзывание.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str2265Key)]
		public decimal Slippage
		{
			get { return (decimal)Parameters[_keySlippage]; }
			set { Parameters[_keySlippage] = value; }
		}

		private const string _keyTargetPrice = "TargetPrice";

		/// <summary>
		/// Цена фиксирования прибыли. Используется для заявок Stop+TargetProfit.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2266Key)]
		[DescriptionLoc(LocalizedStrings.Str2267Key)]
		public decimal TargetPrice
		{
			get { return (decimal)Parameters[_keyTargetPrice]; }
			set { Parameters[_keyTargetPrice] = value; }
		}

		private const string _keyLevel = "Level";

		/// <summary>
		/// Максимальное отклонение в противоположную сторону. Используется для заявок TrailingStop.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2268Key)]
		[DescriptionLoc(LocalizedStrings.Str2269Key)]
		public decimal Level
		{
			get { return (decimal)Parameters[_keyLevel]; }
			set { Parameters[_keyLevel] = value; }
		}
	}
}