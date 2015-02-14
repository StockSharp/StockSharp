namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Функциональность.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetPlatformAttribute : Attribute
	{
		/// <summary>
		/// Целевая аудитория.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Платформа.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// Создать <see cref="TargetPlatformAttribute"/>.
		/// </summary>
		/// <param name="preferLanguage">Целевая аудитория.</param>
		/// <param name="platform">Платформа.</param>
		public TargetPlatformAttribute(Languages preferLanguage = Languages.English, Platforms platform = Platforms.AnyCPU)
		{
			PreferLanguage = preferLanguage;
			Platform = platform;
		}
	}

	/// <summary>
	/// Интерфейс, описывающий контейнер для сессии.
	/// </summary>
	public interface IMessageSessionHolder : IPersistable, ILogReceiver, IMessageChannel
	{
		/// <summary>
		/// Генератор идентификаторов транзакций.
		/// </summary>
		IdGenerator TransactionIdGenerator { get; }

		/// <summary>
		/// <see langword="true"/>, если сессия используется для получения маркет-данных, иначе, <see langword="false"/>.
		/// </summary>
		bool IsMarketDataEnabled { get; set; }

		/// <summary>
		/// <see langword="true"/>, если сессия используется для отправки транзакций, иначе, <see langword="false"/>.
		/// </summary>
		bool IsTransactionEnabled { get; set; }

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Объединять обработчики входящих сообщений для адаптеров.
		/// </summary>
		bool JoinInProcessors { get; }

		/// <summary>
		/// Объединять обработчики исходящих сообщений для адаптеров.
		/// </summary>
		bool JoinOutProcessors { get; }

		/// <summary>
		/// Описание классов инструментов, в зависимости от которых будут проставляться параметры в <see cref="SecurityMessage.SecurityType"/> и <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; }

		/// <summary>
		/// Настройки механизма отслеживания соединений.
		/// </summary>
		MessageAdapterReConnectionSettings ReConnectionSettings { get; }

		/// <summary>
		/// Интервал оповещения сервера о том, что подключение еще живое. По-умолчанию равно 1 минуте.
		/// </summary>
		TimeSpan HeartbeatInterval { get; set; }

		/// <summary>
		/// Интервал генерации сообщения <see cref="TimeMessage"/>. По-умолчанию равно 10 миллисекундам.
		/// </summary>
		TimeSpan MarketTimeChangedInterval { get; set; }

		/// <summary>
		/// Являются ли подключения адаптеров независимыми друг от друга.
		/// </summary>
		bool IsAdaptersIndependent { get; }

		/// <summary>
		/// Создавать объединенный инструмент для инструментов с разных торговых площадок.
		/// </summary>
		bool CreateAssociatedSecurity { get; set; }

		/// <summary>
		/// Обновлять стакан для инструмента при появлении сообщения <see cref="Level1ChangeMessage"/>.
		/// </summary>
		bool CreateDepthFromLevel1 { get; set; }

		/// <summary>
		/// Код площадки для объединенного инструмента.
		/// </summary>
		string AssociatedBoardCode { get; set; }

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		OrderCondition CreateOrderCondition();

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		IMessageAdapter CreateTransactionAdapter();

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		IMessageAdapter CreateMarketDataAdapter();
	}
}