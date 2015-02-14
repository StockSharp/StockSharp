namespace StockSharp.Quik
{
	using System;

	/// <summary>
	/// Результат проверки настроек таблицы терминала Quik.
	/// </summary>
	public class DdeSettingsResult
	{
		internal DdeSettingsResult(DdeTable table, Exception error, bool isCritical)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			if (error == null)
				throw new ArgumentNullException("error");

			Table = table;
			Error = error;
			IsCritical = isCritical;
		}

		/// <summary>
		/// Таблица, для которой найдена ошибка в настройках.
		/// </summary>
		public DdeTable Table { get; private set; }

		/// <summary>
		/// Описание ошибки.
		/// </summary>
		public Exception Error { get; private set; }

		/// <summary>
		/// Критическая ли ошибка (будет ли с ней нормально работать экспорт DDE).
		/// </summary>
		public bool IsCritical { get; private set; }
	}
}