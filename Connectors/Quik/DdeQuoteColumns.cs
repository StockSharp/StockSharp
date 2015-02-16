namespace StockSharp.Quik
{
	/// <summary>
	/// Колонки таблицы со стаканом.
	/// </summary>
	public static class DdeQuoteColumns
	{
		static DdeQuoteColumns()
		{
			AskVolume = new DdeTableColumn(DdeTableTypes.Quote, "Продажа", typeof(decimal));
			BidVolume = new DdeTableColumn(DdeTableTypes.Quote, "Покупка", typeof(decimal));
			Price = new DdeTableColumn(DdeTableTypes.Quote, "Цена", typeof(decimal));

			OwnAskVolume = new DdeTableColumn(DdeTableTypes.Quote, "Своя продажа", typeof(decimal));
			OwnBidVolume = new DdeTableColumn(DdeTableTypes.Quote, "Своя покупка", typeof(decimal));
			YieldAsk = new DdeTableColumn(DdeTableTypes.Quote, "Доходность продажи", typeof(decimal));
			YieldBid = new DdeTableColumn(DdeTableTypes.Quote, "Доходность покупки", typeof(decimal));
			BetterAskVolume = new DdeTableColumn(DdeTableTypes.Quote, "Сумма лучшей продажи", typeof(decimal));
			BetterBidVolume = new DdeTableColumn(DdeTableTypes.Quote, "Сумма лучшей покупки", typeof(decimal));
		}

		/// <summary>
		/// Количество бумаг в заявках на продажу по данной цене, лотов.
		/// </summary>
		public static DdeTableColumn AskVolume { get; private set; }

		/// <summary>
		/// Количество бумаг в заявках на покупку по данной цене, лотов.
		/// </summary>
		public static DdeTableColumn BidVolume { get; private set; }

		/// <summary>
		/// Котировка.
		/// </summary>
		public static DdeTableColumn Price { get; private set; }

		/// <summary>
		/// Количество бумаг в собственных заявках на продажу по данной цене, лотов.
		/// </summary>
		public static DdeTableColumn OwnAskVolume { get; private set; }

		/// <summary>
		/// Количество бумаг в собственных заявках на покупку по данной цене, лотов.
		/// </summary>
		public static DdeTableColumn OwnBidVolume { get; private set; }

		/// <summary>
		/// Доходность инструмента по котировке на продажу.
		/// </summary>
		public static DdeTableColumn YieldAsk { get; private set; }

		/// <summary>
		/// Доходность инструмента по котировке на покупку.
		/// </summary>
		public static DdeTableColumn YieldBid { get; private set; }

		/// <summary>
		/// Количество бумаг в заявках на продажу по цене не хуже данной, лотов.
		/// </summary>
		public static DdeTableColumn BetterAskVolume { get; private set; }

		/// <summary>
		/// Количество бумаг в заявках на покупку по цене не хуже данной, лотов.
		/// </summary>
		public static DdeTableColumn BetterBidVolume { get; private set; }
	}
}