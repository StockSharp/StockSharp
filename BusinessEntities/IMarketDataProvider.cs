namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс поставщика маркет-данных по инструменту.
	/// </summary>
	public interface IMarketDataProvider
	{
		/// <summary>
		/// Событие изменения инструмента.
		/// </summary>
		event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTime> ValuesChanged;

		/// <summary>
		/// Получить стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Стакан котировок.</returns>
		MarketDepth GetMarketDepth(Security security);

		/// <summary>
		/// Получить значение маркет-данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="field">Поле маркет-данных.</param>
		/// <returns>Значение поля. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		object GetSecurityValue(Security security, Level1Fields field);

		/// <summary>
		/// Получить набор доступных полей <see cref="Level1Fields"/>, для которых есть маркет-данные для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Набор доступных полей.</returns>
		IEnumerable<Level1Fields> GetLevel1Fields(Security security);
	}
}