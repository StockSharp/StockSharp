#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeSettingsResult.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
				throw new ArgumentNullException(nameof(table));

			if (error == null)
				throw new ArgumentNullException(nameof(error));

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