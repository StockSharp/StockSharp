namespace StockSharp.Algo.Strategies.Reporting
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Strategies;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Базовый генератор отчета для стратегии.
	/// </summary>
	public abstract class StrategyReport
	{
		/// <summary>
		/// Инициализировать <see cref="StrategyReport"/>.
		/// </summary>
		/// <param name="strategies">Стратегии, для которых необходимо сгенерировать отчет.</param>
		/// <param name="fileName">Название файла, в котором сгенерируется отчет.</param>
		protected StrategyReport(IEnumerable<Strategy> strategies, string fileName)
		{
			if (strategies == null)
				throw new ArgumentNullException("strategies");

			if (strategies.IsEmpty())
				throw new ArgumentOutOfRangeException("strategies");

			if (fileName.IsEmpty())
				throw new ArgumentNullException("fileName");

			Strategies = strategies;
			FileName = fileName;
		}

		/// <summary>
		/// Название файла, в котором сгенерируется отчет.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Стратегии, для которых необходимо сгенерировать отчет.
		/// </summary>
		public IEnumerable<Strategy> Strategies { get; private set; }

		/// <summary>
		/// Сгенерировать отчет.
		/// </summary>
		public abstract void Generate();

		/// <summary>
		/// Отформатировать время в строку.
		/// </summary>
		/// <param name="time">Время.</param>
		/// <returns>Отформатированная строка.</returns>
		protected virtual string Format(TimeSpan? time)
		{
			return time == null
				? string.Empty
				: "{0:00}:{1:00}:{2:00}".Put(time.Value.TotalHours, time.Value.Minutes, time.Value.Seconds);
		}

		/// <summary>
		/// Отформатировать время в строку.
		/// </summary>
		/// <param name="time">Время.</param>
		/// <returns>Отформатированная строка.</returns>
		protected virtual string Format(DateTimeOffset time)
		{
			return time.To<string>();
		}

		/// <summary>
		/// Отформатировать направление заявки в строку.
		/// </summary>
		/// <param name="direction">Направление заявки.</param>
		/// <returns>Отформатированная строка.</returns>
		protected virtual string Format(Sides direction)
		{
			return direction == Sides.Buy ? LocalizedStrings.Str403 : LocalizedStrings.Str404;
		}

		/// <summary>
		/// Отформатировать состояние заявки в строку.
		/// </summary>
		/// <param name="state">Состояние заявки.</param>
		/// <returns>Отформатированная строка.</returns>
		protected virtual string Format(OrderStates state)
		{
			switch (state)
			{
				case OrderStates.None:
					return string.Empty;
				case OrderStates.Active:
					return LocalizedStrings.Str238;
				case OrderStates.Done:
					return LocalizedStrings.Str239;
				case OrderStates.Failed:
					return LocalizedStrings.Str152;
				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		/// <summary>
		/// Отформатировать тип заявки в строку.
		/// </summary>
		/// <param name="type">Тип заявки.</param>
		/// <returns>Отформатированная строка.</returns>
		protected virtual string Format(OrderTypes type)
		{
			switch (type)
			{
				case OrderTypes.Limit:
					return LocalizedStrings.Str1353;
				case OrderTypes.Market:
					return LocalizedStrings.Str241;
				case OrderTypes.Repo:
					return LocalizedStrings.Str243;
				case OrderTypes.Rps:
					return LocalizedStrings.Str1354;
				case OrderTypes.ExtRepo:
					return LocalizedStrings.Str244;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}
	}
}