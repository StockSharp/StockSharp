namespace StockSharp.Hydra.Core
{
	using System;

	/// <summary>
	/// Категории задач.
	/// </summary>
	[Flags]
	public enum TaskCategories
	{
		/// <summary>
		/// Россия.
		/// </summary>
		Russia = 1,

		/// <summary>
		/// Америка.
		/// </summary>
		America = Russia << 1,

		/// <summary>
		/// Фондовая биржа.
		/// </summary>
		Stock = America << 1,

		/// <summary>
		/// Форекс.
		/// </summary>
		Forex = Stock << 1,

		/// <summary>
		/// Криптовалюта.
		/// </summary>
		Crypto = Forex << 1,

		/// <summary>
		/// История.
		/// </summary>
		History = Crypto << 1,

		/// <summary>
		/// Реал-тайм.
		/// </summary>
		RealTime = History << 1,

		/// <summary>
		/// Бесплатно.
		/// </summary>
		Free = RealTime << 1,

		/// <summary>
		/// Платно.
		/// </summary>
		Paid = Free << 1,

		/// <summary>
		/// Тики (источник).
		/// </summary>
		Ticks = Paid << 1,

		/// <summary>
		/// Свечи (источник).
		/// </summary>
		Candles = Ticks << 1,

		/// <summary>
		/// Стакан (источник).
		/// </summary>
		MarketDepth = Candles << 1,

		/// <summary>
		/// Level1 (источник).
		/// </summary>
		Level1 = MarketDepth << 1,

		/// <summary>
		/// Лог заявок (источник).
		/// </summary>
		OrderLog = Level1 << 1,

		/// <summary>
		/// Новости (источник).
		/// </summary>
		News = OrderLog << 1,

		/// <summary>
		/// Транзакции (источник).
		/// </summary>
		Transactions = News << 1,

		/// <summary>
		/// Вспомогательная задача.
		/// </summary>
		Tool = Transactions << 1,
	}

	/// <summary>
	/// Артибут, задающий категории задач.
	/// </summary>
	public class TaskCategoryAttribute : Attribute
	{
		/// <summary>
		/// Категории задач.
		/// </summary>
		public TaskCategories Categories { get; private set; }

		/// <summary>
		/// Создать <see cref="TaskCategoryAttribute"/>.
		/// </summary>
		/// <param name="categories">Категории задач.</param>
		public TaskCategoryAttribute(TaskCategories categories)
		{
			Categories = categories;
		}
	}
}