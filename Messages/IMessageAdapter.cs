namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Интерфейс, описывающий адаптер, конвертирующий сообщения <see cref="Message"/> в команды торговой системы и обратно.
	/// </summary>
	public interface IMessageAdapter : IMessageChannel, IPersistable, ILogReceiver
	{
		/// <summary>
		/// Генератор идентификаторов транзакций.
		/// </summary>
		IdGenerator TransactionIdGenerator { get; }

		/// <summary>
		/// Поддерживаемые типы сообщений, который может обработать адаптер.
		/// </summary>
		MessageTypes[] SupportedMessages { get; set; }

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Описание классов инструментов, в зависимости от которых будут проставляться параметры в <see cref="SecurityMessage.SecurityType"/> и <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; }

		/// <summary>
		/// Настройки механизма отслеживания соединений <see cref="IMessageAdapter"/> с торговом системой.
		/// </summary>
		ReConnectionSettings ReConnectionSettings { get; }

		/// <summary>
		/// Интервал оповещения сервера о том, что подключение еще живое.
		/// </summary>
		TimeSpan HeartbeatInterval { get; set; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		bool PortfolioLookupRequired { get; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		bool SecurityLookupRequired { get; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		bool OrderStatusRequired { get; }

		/// <summary>
		/// Код площадки для объединенного инструмента.
		/// </summary>
		string AssociatedBoardCode { get; }

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		OrderCondition CreateOrderCondition();

		/// <summary>
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если было успешно установлено подключение.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, <see langword="false"/>, если торговая система разорвала подключение.</returns>
		bool IsConnectionAlive();

		/// <summary>
		/// Создать построитель стакана.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Построитель стакана.</returns>
		IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId);
	}
}