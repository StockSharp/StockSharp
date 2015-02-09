namespace StockSharp.Alor.Metadata
{
	/// <summary>
	/// Колонки системной таблицы TRDACC.
	/// </summary>
	public static class AlorPortfolioColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Portfolio, "ID", typeof(int), false);

		/// <summary>
		/// Номер торгового счета.
		/// </summary>
		public static readonly AlorColumn Account = new AlorColumn(AlorTableTypes.Portfolio, "Account", typeof(string));

		/// <summary>
		/// Наименование торгового счета.
		/// </summary>
		public static readonly AlorColumn Name = new AlorColumn(AlorTableTypes.Portfolio, "Name", typeof(string), false);

		/// <summary>
		/// Тип торгового счета.
		/// </summary>
		public static readonly AlorColumn Type = new AlorColumn(AlorTableTypes.Portfolio, "Type", typeof(string), false);

		/// <summary>
		/// Код банковского счета.
		/// </summary>
		public static readonly AlorColumn BankAccount = new AlorColumn(AlorTableTypes.Portfolio, "BankAccount", typeof(string), false);

		/// <summary>
		/// Код депозитарного счета.
		/// </summary>
		public static readonly AlorColumn DepoAccount = new AlorColumn(AlorTableTypes.Portfolio, "DepoAccount", typeof(string), false);

		/// <summary>
		/// Сокращенное наименование банка.
		/// </summary>
		public static readonly AlorColumn BankName = new AlorColumn(AlorTableTypes.Portfolio, "BankCode", typeof(string), false);

		/// <summary>
		/// Сокращенное наименование депозитария.
		/// </summary>
		public static readonly AlorColumn DepoName = new AlorColumn(AlorTableTypes.Portfolio, "DepoCode", typeof(string), false);
	}
}