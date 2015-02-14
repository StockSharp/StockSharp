namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Интерфейс, описывающий провайдер бирж и торговых площадок.
	/// </summary>
	public interface IExchangeInfoProvider
	{
		/// <summary>
		/// Все биржи.
		/// </summary>
		IEnumerable<ExchangeBoard> Boards { get; }

		/// <summary>
		/// Все площадки.
		/// </summary>
		IEnumerable<Exchange> Exchanges { get; }

		/// <summary>
		/// Получить площадку по коду.
		/// </summary>
		/// <param name="code">Код площадки <see cref="ExchangeBoard.Code"/>.</param>
		/// <returns>Торговая площадка. Если площадка с заданным кодом не существует, то будет возвращено <see langword="null"/>.</returns>
		ExchangeBoard GetExchangeBoard(string code);

		/// <summary>
		/// Получить биржу по коду.
		/// </summary>
		/// <param name="code">Код биржи <see cref="Exchange.Name"/>.</param>
		/// <returns>Биржа. Если биржа с заданным кодом не существует, то будет возвращено <see langword="null"/>.</returns>
		Exchange GetExchange(string code);

		/// <summary>
		/// Сохранить площадку.
		/// </summary>
		/// <param name="board">Торговая площадка.</param>
		void Save(ExchangeBoard board);

		/// <summary>
		/// Сохранить биржу.
		/// </summary>
		/// <param name="exchange">Биржа.</param>
		void Save(Exchange exchange);

		/// <summary>
		/// Оповещение о добавлении новой площадки.
		/// </summary>
		event Action<ExchangeBoard> BoardAdded;

		/// <summary>
		/// Оповещение о добавлении новой биржи.
		/// </summary>
		event Action<Exchange> ExchangeAdded;
	}
}