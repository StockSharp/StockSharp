namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.ITCH;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Plaza;

	/// <summary>
	/// Построители стаканов из лога заявок.
	/// </summary>
	public enum OrderLogBuilders
	{
		/// <summary>
		/// Плаза 2.
		/// </summary>
		Plaza2,

		/// <summary>
		/// ITCH.
		/// </summary>
		ITCH
	}

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
		/// Включено ли у задачи закачка лога собственной торговли.
		/// </summary>
		/// <param name="task">Задача.</param>
		/// <returns>Включено ли у задачи закачка лога собственной торговли.</returns>
		public static bool IsExecLogEnabled(this IHydraTask task)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			return task.Settings.Securities.Any(s => s.MarketDataTypesSet.Contains(typeof(ExecutionMessage)));
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
		/// Получить описание задачи.
		/// </summary>
		/// <param name="task">Задача.</param>
		/// <returns>Описание задачи.</returns>
		public static string GetDescription(this IHydraTask task)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			return task.GetType().GetDescription();
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
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			if (security == null && dataType != typeof(NewsMessage) && dataType != typeof(SecurityMessage))
				throw new ArgumentNullException("security");

			string fileName;

			if (dataType == typeof(QuoteChangeMessage))
				fileName = "depths";
			else if (dataType == typeof(Level1ChangeMessage))
				fileName = "level1";
			else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				fileName = "candles_{0}_{1}".Put(typeof(TimeFrameCandle).Name, arg).Replace(':', '_');
			else if (dataType == typeof(NewsMessage))
				fileName = "news";
			else if (dataType == typeof(SecurityMessage))
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

			return security.Security.Board.IsTradeDate(date.ApplyTimeZone(security.Security.Board.Exchange.TimeZoneInfo), true);
		}

		/// <summary>
		/// Получить шаблон для текстового экспорта данных.
		/// </summary>
		/// <param name="dataType">Тип данных.</param>
		/// <param name="arg">Параметр данных.</param>
		/// <returns>Шаблон для текстового экспорта данных.</returns>
		public static string GetTxtTemplate(this Type dataType, object arg = null)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			string templateName;

			if (dataType == typeof(SecurityMessage))
				templateName = "txt_export_securities";
			else if (dataType == typeof(NewsMessage))
				templateName = "txt_export_news";
			else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				templateName = "txt_export_candles";
			else if (dataType == typeof(Level1ChangeMessage))
				templateName = "txt_export_level1";
			else if (dataType == typeof(QuoteChangeMessage))
				templateName = "txt_export_depths";
			else if (dataType == typeof(ExecutionMessage))
			{
				if (arg == null)
					throw new ArgumentNullException("arg");

				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Tick:
						templateName = "txt_export_ticks";
						break;
					case ExecutionTypes.Order:
					case ExecutionTypes.Trade:
						templateName = "txt_export_transactions";
						break;
					case ExecutionTypes.OrderLog:
						templateName = "txt_export_orderlog";
						break;
					default:
						throw new InvalidOperationException(LocalizedStrings.Str1122Params.Put(arg));
				}
			}
			else
				throw new ArgumentOutOfRangeException("dataType", dataType, LocalizedStrings.Str721);

			return ConfigurationManager.AppSettings.Get(templateName);
		}

		/// <summary>
		/// Принадлежит ли задача категории.
		/// </summary>
		/// <param name="task">Задача.</param>
		/// <param name="category">Категория.</param>
		/// <returns>Принадлежит ли задача категории.</returns>
		public static bool IsCategoryOf(this IHydraTask task, TaskCategories category)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			return task.GetType().IsCategoryOf(category);
		}

		/// <summary>
		/// Принадлежит ли задача категории.
		/// </summary>
		/// <param name="taskType">Задача.</param>
		/// <param name="category">Категория.</param>
		/// <returns>Принадлежит ли задача категории.</returns>
		public static bool IsCategoryOf(this Type taskType, TaskCategories category)
		{
			var attr = taskType.GetAttribute<TaskCategoryAttribute>();
			return attr != null && attr.Categories.Contains(category);
		}

		/// <summary>
		/// Получить инонку задачи.
		/// </summary>
		/// <param name="taskType">Задача.</param>
		/// <returns>Иконка задачи.</returns>
		public static Uri GetIcon(this Type taskType)
		{
			var attr = taskType.GetAttribute<TaskIconAttribute>();
			return attr == null ? null : attr.Icon.GetResourceUrl(taskType);
		}

		/// <summary>
		/// Создать построитель стакана из лога заявок.
		/// </summary>
		/// <param name="builder">Тип построителя.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Построитель стакана из лога заявок.</returns>
		public static IOrderLogMarketDepthBuilder CreateBuilder(this OrderLogBuilders builder, SecurityId securityId)
		{
			switch (builder)
			{
				case OrderLogBuilders.Plaza2:
					return new PlazaOrderLogMarketDepthBuilder(securityId);
				case OrderLogBuilders.ITCH:
					return new ItchOrderLogMarketDepthBuilder(securityId);
				default:
					throw new ArgumentOutOfRangeException("builder", builder, null);
			}
		}
	}
}