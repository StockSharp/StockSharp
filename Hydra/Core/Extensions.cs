namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Вспомогательный класс.
	/// </summary>
	public static class Extensions
	{
		private static readonly CachedSynchronizedSet<IHydraTask> _tasks = new CachedSynchronizedSet<IHydraTask>();

		/// <summary>
		/// Все созданные задачи.
		/// </summary>
		public static CachedSynchronizedSet<IHydraTask> Tasks
		{
			get { return _tasks; }
		}

		/// <summary>
		/// Идентификатор инструмента "Все инструменты".
		/// </summary>
		public const string AllSecurityId = "ALL@ALL";

		/// <summary>
		/// Получить инструмент "Все инструменты" для задачи.
		/// </summary>
		/// <param name="task">Задача.</param>
		/// <returns>Инструмент "Все инструменты".</returns>
		public static HydraTaskSecurity GetAllSecurity(this IHydraTask task)
		{
			if (task == null)
				throw new ArgumentNullException("task");
			
			return task.Settings.Securities.FirstOrDefault(s => s.Security.IsAllSecurity());
		}

		/// <summary>
		/// Проверить, является ли инструмент "Все инструменты".
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns><see langword="true"/>, если инструмент "Все инструменты", иначе, <see langword="false"/>.</returns>
		public static bool IsAllSecurity(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return security.Id.CompareIgnoreCase(AllSecurityId);
		}

		/// <summary>
		/// Преобразовать <see cref="Security"/> в <see cref="HydraTaskSecurity"/>.
		/// </summary>
		/// <param name="task">Задача.</param>
		/// <param name="securities">Исходные инструменты.</param>
		/// <returns>Сконвертированные инструменты.</returns>
		public static IEnumerable<HydraTaskSecurity> ToHydraSecurities(this IHydraTask task, IEnumerable<Security> securities)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			if (securities == null)
				throw new ArgumentNullException("securities");

			var allSec = task.GetAllSecurity();

			var secMap = task.Settings.Securities.ToDictionary(s => s.Security, s => s);

			return securities.Where(s => s != allSec.Security).Select(s => secMap.TryGetValue(s) ?? new HydraTaskSecurity
			{
				Security = s,
				Settings = task.Settings,
				MarketDataTypes = allSec == null ? ArrayHelper.Empty<Type>() : allSec.MarketDataTypes,
			});
		}

		/// <summary>
		/// Получить отображаемое имя для задачи.
		/// </summary>
		/// <param name="task">Задача.</param>
		/// <returns>Отображаемое имя.</returns>
		public static string GetDisplayName(this IHydraTask task)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			return task.GetType().GetDisplayName();
		}

		/// <summary>
		/// Сгенерировать имя эспортируемого файла.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <param name="from">Дата начала.</param>
		/// <param name="to">Дата окончания.</param>
		/// <param name="type">Тип экспорта.</param>
		/// <returns>Имя эспортируемого файла.</returns>
		public static string GetFileName(this Security security, Type dataType, object arg, DateTime? from, DateTime? to, ExportTypes type)
		{
			if (security == null && dataType != typeof(News) && dataType != typeof(Security))
				throw new ArgumentNullException("security");

			if (dataType == null)
				throw new ArgumentNullException("dataType");

			string fileName;

			if (dataType == typeof(Trade))
				fileName = "trades";
			else if (dataType == typeof(MarketDepth) || dataType == typeof(QuoteChangeMessage))
				fileName = "depths";
			else if (dataType == typeof(Level1ChangeMessage))
				fileName = "level1";
			else if (dataType == typeof(OrderLogItem))
				fileName = "orderLog";
			else if (dataType.IsSubclassOf(typeof(Candle)))
				fileName = "candles_{0}_{1}".Put(dataType.Name, arg).Replace(':', '_');
			else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				fileName = "candles_{0}_{1}".Put(typeof(TimeFrameCandle).Name, arg).Replace(':', '_');
			else if (dataType == typeof(News))
				fileName = "news";
			else if (dataType == typeof(Security) || dataType == typeof(SecurityMessage))
				fileName = "securities";
			else if (dataType == typeof(ExecutionMessage))
			{
				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Tick:
						fileName = "trades";
						break;
					case ExecutionTypes.OrderLog:
						fileName = "orderLog";
						break;
					case ExecutionTypes.Order:
						fileName = "executions";
						break;
					default:
						throw new ArgumentOutOfRangeException("arg");
				}
			}
			else
				throw new ArgumentOutOfRangeException("dataType");

			if (security != null)
				fileName += security.Id.SecurityIdToFolderName();

			if (from != null && to != null)
				fileName += "_{0:yyyy_MM_dd}_{1:yyyy_MM_dd}".Put(from, to);

			switch (type)
			{
				case ExportTypes.Excel:
					fileName += ".xlsx";
					break;
				case ExportTypes.Xml:
					fileName += ".xml";
					break;
				case ExportTypes.Txt:
					fileName += ".csv";
					break;
				case ExportTypes.Sql:
					break;
				case ExportTypes.Bin:
					fileName += ".bin";
					break;
				default:
					throw new ArgumentOutOfRangeException("type");
			}

			return fileName;
		}

		/// <summary>
		/// Проверить, является ли дата торгуемой.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="date">Передаваемая дата, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если торгуемая дата, иначе, неторгуемая.</returns>
		public static bool IsTradeDate(this HydraTaskSecurity security, DateTime date)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return security.Security.Board.WorkingTime.IsTradeDate(date, true);
		}
	}
}