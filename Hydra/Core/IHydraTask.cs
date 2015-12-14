#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: IHydraTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Algo;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Состояния задачи.
	/// </summary>
	public enum TaskStates
	{
		/// <summary>
		/// Остановлен.
		/// </summary>
		Stopped,

		/// <summary>
		/// Останавливается.
		/// </summary>
		Stopping,

		/// <summary>
		/// Запускается.
		/// </summary>
		Starting,

		/// <summary>
		/// Запущен.
		/// </summary>
		Started,
	}

	/// <summary>
	/// Интерфейс, описывающий задачу.
	/// </summary>
	public interface IHydraTask : ILogReceiver
	{
		/// <summary>
		/// Адрес иконки, для визуального обозначения.
		/// </summary>
		Uri Icon { get; }

		/// <summary>
		/// Настройки задачи <see cref="IHydraTask"/>.
		/// </summary>
		HydraTaskSettings Settings { get; }

		/// <summary>
		/// Инициализировать задачу.
		/// </summary>
		/// <param name="settings">Настройки задачи.</param>
		void Init(HydraTaskSettings settings);

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		void SaveSettings();

		/// <summary>
		/// Запустить.
		/// </summary>
		void Start();

		/// <summary>
		/// Остановить.
		/// </summary>
		void Stop();

		/// <summary>
		/// Поддерживаемые типы данных.
		/// </summary>
		IEnumerable<DataType> SupportedDataTypes { get; }

		/// <summary>
		/// Событие о загрузке маркет-данных.
		/// </summary>
		event Action<Security, Type, object, DateTimeOffset, int> DataLoaded;

		/// <summary>
		/// Событие запуска.
		/// </summary>
		event Action<IHydraTask> Started;

		/// <summary>
		/// Событие остановки.
		/// </summary>
		event Action<IHydraTask> Stopped;

		/// <summary>
		/// Текущее состояние задачи.
		/// </summary>
		TaskStates State { get; }
	}
}