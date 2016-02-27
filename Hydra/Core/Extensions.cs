#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Reflection;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.ITCH;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Plaza;
	using StockSharp.Xaml;

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
		/// <summary>
		/// Все созданные задачи.
		/// </summary>
		public static CachedSynchronizedSet<IHydraTask> Tasks { get; } = new CachedSynchronizedSet<IHydraTask>();

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
				throw new ArgumentNullException(nameof(task));
			
			return task.Settings.Securities.FirstOrDefault(s => s.Security.IsAllSecurity());
		}

		/// <summary>
		/// Получить инструмент "Все инструменты".
		/// </summary>
		/// <returns>Инструмент "Все инструменты".</returns>
		public static Security GetAllSecurity()
		{
			return ConfigManager.GetService<IEntityRegistry>().Securities.GetAllSecurity();
		}

		/// <summary>
		/// Получить инструмент "Все инструменты".
		/// </summary>
		/// <param name="securities">Инструменты.</param>
		/// <returns>Инструмент "Все инструменты".</returns>
		public static Security GetAllSecurity(this IStorageSecurityList securities)
		{
			return securities.ReadById(AllSecurityId);
		}

		/// <summary>
		/// Получить инструмент "Все инструменты".
		/// </summary>
		/// <param name="picker">Визуальный компонент для поиска и выбора инструмента.</param>
		public static void ExcludeAllSecurity(this SecurityPicker picker)
		{
			if (picker == null)
				throw new ArgumentNullException(nameof(picker));

			picker.ExcludeSecurities.Add(GetAllSecurity());
		}

		///// <summary>
		///// Включено ли у задачи закачка лога собственной торговли.
		///// </summary>
		///// <param name="task">Задача.</param>
		///// <returns>Включено ли у задачи закачка лога собственной торговли.</returns>
		//public static bool IsExecLogEnabled(this IHydraTask task)
		//{
		//	if (task == null)
		//		throw new ArgumentNullException(nameof(task));

		//	return task.Settings.Securities.Any(s => s.DataTypesSet.Contains(typeof(ExecutionMessage)));
		//}

		/// <summary>
		/// Проверить, является ли инструмент "Все инструменты".
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns><see langword="true"/>, если инструмент "Все инструменты", иначе, <see langword="false"/>.</returns>
		public static bool IsAllSecurity(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

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
				throw new ArgumentNullException(nameof(task));

			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			var allSec = task.GetAllSecurity();

			var secMap = task.Settings.Securities.ToDictionary(s => s.Security, s => s);

			return securities.Where(s => s != allSec.Security).Select(s => secMap.TryGetValue(s) ?? new HydraTaskSecurity
			{
				Security = s,
				Settings = task.Settings,
				DataTypes = allSec == null ? ArrayHelper.Empty<DataType>() : allSec.DataTypes,
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
				throw new ArgumentNullException(nameof(task));

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
				throw new ArgumentNullException(nameof(task));

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
				throw new ArgumentNullException(nameof(dataType));

			if (security == null && dataType != typeof(NewsMessage) && dataType != typeof(SecurityMessage))
				throw new ArgumentNullException(nameof(security));

			string fileName;

			if (dataType == typeof(QuoteChangeMessage))
				fileName = "depths";
			else if (dataType == typeof(Level1ChangeMessage))
				fileName = "level1";
			else if (dataType.IsCandleMessage())
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
					case ExecutionTypes.Transaction:
						fileName = "transactions";
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(arg));
				}
			}
			else
				throw new ArgumentOutOfRangeException(nameof(dataType));

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
				case ExportTypes.StockSharp:
					fileName += ".bin";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type));
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
				throw new ArgumentNullException(nameof(security));

			return security.Security.Board.IsTradeDate(date.ApplyTimeZone(security.Security.Board.TimeZone), true);
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
				throw new ArgumentNullException(nameof(dataType));

			string templateName;

			if (dataType == typeof(SecurityMessage))
				templateName = "txt_export_securities";
			else if (dataType == typeof(NewsMessage))
				templateName = "txt_export_news";
			else if (dataType.IsCandleMessage())
				templateName = "txt_export_candles";
			else if (dataType == typeof(Level1ChangeMessage))
				templateName = "txt_export_level1";
			else if (dataType == typeof(QuoteChangeMessage))
				templateName = "txt_export_depths";
			else if (dataType == typeof(ExecutionMessage))
			{
				if (arg == null)
					throw new ArgumentNullException(nameof(arg));

				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Tick:
						templateName = "txt_export_ticks";
						break;
					case ExecutionTypes.Transaction:
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
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str721);

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
				throw new ArgumentNullException(nameof(task));

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
			var url = taskType.GetIconUrl();

			if (url != null)
				return url;

			var connectorType = taskType.GetGenericType(typeof(ConnectorHydraTask<>));
			return connectorType?.GenericTypeArguments[0].GetIconUrl();
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
					throw new ArgumentOutOfRangeException(nameof(builder), builder, null);
			}
		}

		/// <summary>
		/// Проверить, включена ли закачка данных <see cref="Level1ChangeMessage"/> для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Результат проверки.</returns>
		public static bool IsLevel1Enabled(this HydraTaskSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.DataTypesSet.Contains(DataType.Create(typeof(Level1ChangeMessage), null));
		}

		/// <summary>
		/// Проверить, включена ли закачка данных тиков для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Результат проверки.</returns>
		public static bool IsTicksEnabled(this HydraTaskSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.DataTypesSet.Contains(DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Tick));
		}

		/// <summary>
		/// Получить серии свечек.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Серии свечек.</returns>
		public static IEnumerable<DataType> GetCandleSeries(this HydraTaskSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.DataTypes.Where(t => t.MessageType.IsCandleMessage());
		}
	}
}