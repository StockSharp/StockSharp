namespace StockSharp.Quik
{
	/// <summary>
	/// Колонки таблицы Портфель по деривативам.
	/// </summary>
	public static class DdeDerivativePortfolioColumns
	{
		static DdeDerivativePortfolioColumns()
		{
			FirmId = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Фирма", typeof(string));
			Account = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Торговый счет", typeof(string));
			LimitType = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Тип лимита", typeof(string));
			LiquidCoef = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Коэф. ликвидн.", typeof(decimal));
			BeginLimitPositionsPrice = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Предыд. лимит откр. поз.", typeof(decimal));
			CurrentLimitPositionsPrice = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Лимит откр. поз.", typeof(decimal));
			CurrentLimitPositionsOrdersPrice = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Тек.чист.поз.", typeof(decimal));
			OrdersPrice = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Тек.чист.поз. (под заявки)", typeof(decimal));
			PositionsPrice = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Тек.чист.поз. (под открытые позиции)", typeof(decimal));
			PlannedPositionsPrice = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "План.чист.поз.", typeof(decimal));
			Margin = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Вариац. маржа", typeof(decimal));
			ACI = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Накоплен. доход", typeof(decimal));
			OptionPositionsPremium = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Премия по опционам", typeof(decimal));
			MarketCommission = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Биржевые сборы", typeof(decimal));
			CoveredClientCoef = new DdeTableColumn(DdeTableTypes.DerivativePortfolio, "Коэфф. кл-го ГО", typeof(decimal));
		}

		/// <summary>
		/// Идентификатор фирмы-дилера в торговой системе.
		/// </summary>
		public static DdeTableColumn FirmId { get; private set; }

		/// <summary>
		/// Внутренний составной параметр сервера QUIK, содержащий обозначение торговой площадки, например – SPBFUT00, и код клиента на бирже, например - 001.
		/// </summary>
		public static DdeTableColumn Account { get; private set; }

		/// <summary>
		/// Тип лимита.
		/// </summary>
		public static DdeTableColumn LimitType { get; private set; }

		/// <summary>
		/// Коэффициент, определяющий какая часть средств блокируется из залогового, а какая из рублёвого лимита.
		/// </summary>
		public static DdeTableColumn LiquidCoef { get; private set; }

		/// <summary>
		/// Лимит открытых позиций по всем инструментам предыдущей торговой сессии в денежном выражении.
		/// </summary>
		public static DdeTableColumn BeginLimitPositionsPrice { get; private set; }

		/// <summary>
		/// Текущий лимит открытых позиций по всем инструментам в денежном выражении.
		/// </summary>
		public static DdeTableColumn CurrentLimitPositionsPrice { get; private set; }

		/// <summary>
		/// Совокупное денежное обеспечение, резервируемое под открытые позиции и торговые операции текущей сессии.
		/// </summary>
		public static DdeTableColumn CurrentLimitPositionsOrdersPrice { get; private set; }

		/// <summary>
		/// Величина гарантийного обеспечения, зарезервированного под клиентские заявки, в рублях.
		/// </summary>
		public static DdeTableColumn OrdersPrice { get; private set; }

		/// <summary>
		/// Величина гарантийного обеспечения, зарезервированного под открытые клиентские позиции, в рублях.
		/// </summary>
		public static DdeTableColumn PositionsPrice { get; private set; }

		/// <summary>
		/// Планируемые чистые позиции по всем инструментам в денежном выражении Соответствует параметру «Свободные средства» рынка FORTS.
		/// </summary>
		public static DdeTableColumn PlannedPositionsPrice { get; private set; }

		/// <summary>
		/// Вариационная маржа по позициям клиента, по всем инструментам, рассчитанная между клирингами.
		/// </summary>
		public static DdeTableColumn Margin { get; private set; }

		/// <summary>
		/// Накопленный до промежуточного клиринга доход на клиентском счету.
		/// </summary>
		public static DdeTableColumn ACI { get; private set; }

		/// <summary>
		/// Премия по опционным позициям, рассчитанная по правилам торговой системы.
		/// </summary>
		public static DdeTableColumn OptionPositionsPremium { get; private set; }

		/// <summary>
		/// Сумма, взимаемая биржевым комитетом за проведение биржевых сделок.
		/// </summary>
		public static DdeTableColumn MarketCommission { get; private set; }

		/// <summary>
		/// Коэффициент клиентского гарантийного обеспечения.
		/// </summary>
		public static DdeTableColumn CoveredClientCoef { get; private set; }
	}
}