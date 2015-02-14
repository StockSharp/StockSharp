namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс к сервису отправки уведомлений на телефон или почту.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/notificationservice.svc")]
	public interface INotificationService
	{
		/// <summary>
		/// Получить доступное количество SMS-сообщений.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Доступное количество SMS-сообщений.</returns>
		[OperationContract]
		int GetSmsCount(Guid sessionId);

		/// <summary>
		/// Получить доступное количество email-сообщений.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Доступное количество email-сообщений.</returns>
		[OperationContract]
		int GetEmailCount(Guid sessionId);

		/// <summary>
		/// Послать SMS-сообщение.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="message">Тело сообщения.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte SendSms(Guid sessionId, string message);

		/// <summary>
		/// Послать email-сообщение.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="caption">Заголовок сообщения.</param>
		/// <param name="message">Тело сообщения.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte SendEmail(Guid sessionId, string caption, string message);

		/// <summary>
		/// Получить последние новости.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии. Может быть пустым, если запрос идет анонимно.</param>
		/// <param name="fromId">Идентификатор, с которого необходимо получить новости.</param>
		/// <returns>Последние новости.</returns>
		[OperationContract]
		Tuple<long, string, string, int>[] GetNews(Guid sessionId, long fromId);
	}
}