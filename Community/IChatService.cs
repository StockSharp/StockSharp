namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс, описывающий сервис чата.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/chatservice.svc", CallbackContract = typeof(IChatServiceCallback))]
	public interface IChatService
	{
		/// <summary>
		/// Получить информацию о пользователе по его идентификатору.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="userId">Идентификатор пользователя.</param>
		/// <returns>Информация о пользователе.</returns>
		[OperationContract]
		User GetUser(Guid sessionId, long userId);

		/// <summary>
		/// Получить информацию о пользователях по их идентификаторам.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="userIds">Идентификаторы пользователей.</param>
		/// <returns>Информация о пользователях.</returns>
		[OperationContract]
		IEnumerable<User> GetUsers(Guid sessionId, long[] userIds);

		/// <summary>
		/// Получить информацию о пользователе по его идентификатору.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="joinId">Идентификатор заявки.</param>
		/// <returns>Информация о заявке.</returns>
		[OperationContract]
		ChatJoin GetJoin(Guid sessionId, long joinId);

		/// <summary>
		/// Получить информацию о комнатах.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Информация о комнатах.</returns>
		[OperationContract]
		IEnumerable<ChatRoom> GetRooms(Guid sessionId);

		/// <summary>
		/// Получить идентификаторы комнат, к которым предоставлен доступ.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Идентификаторы комнат.</returns>
		[OperationContract]
		IEnumerable<long> GetJoinedRooms(Guid sessionId);

		/// <summary>
		/// Получить идентификаторы пользователей в комнате.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		/// <returns>Идентификаторы пользователей.</returns>
		[OperationContract]
		IEnumerable<long> GetAuthors(Guid sessionId, long roomId);

		/// <summary>
		/// Подписаться на новые сообщения.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		[OperationContract]
		void Subscribe(Guid sessionId, long roomId);

		/// <summary>
		/// Создать комнату.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="room">Комната.</param>
		/// <returns>Идентификатор комнаты.</returns>
		[OperationContract]
		long CreateRoom(Guid sessionId, ChatRoom room);

		/// <summary>
		/// Удалить комнату.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		[OperationContract]
		void DeleteRoom(Guid sessionId, long roomId);

		/// <summary>
		/// Обновить детали комнаты.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="room">Комната.</param>
		[OperationContract]
		void UpdateRoom(Guid sessionId, ChatRoom room);

		/// <summary>
		/// Отправить сообщение в комнату.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		/// <param name="body">Тело сообщения.</param>
		/// <returns>Идентификатор сообщения.</returns>
		[OperationContract]
		long SendMessage(Guid sessionId, long roomId, string body);

		/// <summary>
		/// Обновить сообщение в комнате.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="messageId">Идентификатор сообщения.</param>
		/// <param name="body">Новое тело сообщения.</param>
		/// <returns><see langword="true"/>, если сообщение было обновлено, false, если сообщение устарело и его нельзя менять.</returns>
		[OperationContract]
		bool UpdateMessage(Guid sessionId, long messageId, string body);

		/// <summary>
		/// Удалить сообщение.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="messageId">Идентификатор сообщения.</param>
		/// <returns><see langword="true"/>, если сообщение было обновлено, false, если сообщение устарело и его нельзя удалять.</returns>
		[OperationContract]
		bool DeleteMessage(Guid sessionId, long messageId);

		/// <summary>
		/// Отправить заявку на присоединение.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="join">Заявка.</param>
		/// <returns>Идентификатор заявки.</returns>
		[OperationContract]
		long Join(Guid sessionId, ChatJoin join);

		/// <summary>
		/// Покинуть комнату.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		[OperationContract]
		void Leave(Guid sessionId, long roomId);

		/// <summary>
		/// Удовлетворить заявку.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="joinId">Идентификатор заявки.</param>
		[OperationContract]
		void Accept(Guid sessionId, long joinId);

		/// <summary>
		/// Отклонить заявку.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="originalJoinId">Идентификатор заявки на присоединение.</param>
		/// <param name="join">Ответ на заявку.</param>
		/// <returns>Идентификатор заявки.</returns>
		[OperationContract]
		long Reject(Guid sessionId, long originalJoinId, ChatJoin join);

		/// <summary>
		/// Удалить пользователя из комнаты.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		/// <param name="userId">Идентификатор пользователя.</param>
		[OperationContract]
		void DeleteAuthor(Guid sessionId, long roomId, long userId);

		/// <summary>
		/// Получить идентификаторы заявок на присоединение от других пользователей, которые ожидают удовлетворения или отмены.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Идентификаторы заявок.</returns>
		[OperationContract]
		IEnumerable<long> GetAvatingJoins(Guid sessionId);

		/// <summary>
		/// Получить идентификаторы собственных заявок на присоединение, которые ожидают удовлетворения или отмены.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Идентификаторы заявок.</returns>
		[OperationContract]
		IEnumerable<long> GetMyJoins(Guid sessionId);
	}
}