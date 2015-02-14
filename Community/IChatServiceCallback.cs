namespace StockSharp.Community
{
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс, описывающий обратную связь сервиса <see cref="IChatService"/>.
	/// </summary>
	[ServiceContract]
	public interface IChatServiceCallback
	{
		/// <summary>
		/// Новое сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		[OperationContract(IsOneWay = true)]
		void MessageCreated(ChatMessage message);

		/// <summary>
		/// Сообщение обновлено.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		[OperationContract(IsOneWay = true)]
		void MessageUpdated(ChatMessage message);

		/// <summary>
		/// Сообщение удалено.
		/// </summary>
		/// <param name="messageId">Идентификатор сообщения.</param>
		[OperationContract(IsOneWay = true)]
		void MessageDeleted(long messageId);

		///// <summary>
		///// Новые сообщения.
		///// </summary>
		///// <param name="messages">Сообщения.</param>
		//[OperationContract(IsOneWay = true)]
		//void NewMessages(IEnumerable<ChatMessage> messages);

		/// <summary>
		/// Комната создана.
		/// </summary>
		/// <param name="room">Комната.</param>
		[OperationContract(IsOneWay = true)]
		void RoomCreated(ChatRoom room);

		/// <summary>
		/// Комната удалена.
		/// </summary>
		/// <param name="roomId">Идентификатор комнаты.</param>
		[OperationContract(IsOneWay = true)]
		void RoomDeleted(long roomId);

		/// <summary>
		/// Комната обновлена.
		/// </summary>
		/// <param name="room">Комната.</param>
		[OperationContract(IsOneWay = true)]
		void RoomUpdated(ChatRoom room);

		/// <summary>
		/// Пользователь вошел в чат.
		/// </summary>
		/// <param name="userId">Идентификатор пользователя.</param>
		[OperationContract(IsOneWay = true)]
		void LoggedIn(long userId);

		/// <summary>
		/// Пользователь вышел из чата.
		/// </summary>
		/// <param name="userId">Идентификатор пользователя.</param>
		[OperationContract(IsOneWay = true)]
		void LoggedOut(long userId);

		/// <summary>
		/// Новая заявка на участие. Отправляется владельцу комнаты.
		/// </summary>
		/// <param name="joinId">Идентификатор заявки.</param>
		[OperationContract(IsOneWay = true)]
		void JoinSended(long joinId);

		/// <summary>
		/// Заявка удовлетворена. Отправляется тому пользователю, кто послал сообщение через <see cref="IChatService.Join"/>.
		/// </summary>
		/// <param name="joinId">Идентификатор заявки.</param>
		[OperationContract(IsOneWay = true)]
		void JoinAccepted(long joinId);

		/// <summary>
		/// Заявка отклонена. Отправляется тому пользователю, кто послал сообщение через <see cref="IChatService.Join"/>.
		/// </summary>
		/// <param name="originalJoinId">Идентификатор заявки на присоединение.</param>
		/// <param name="join">Ответ на заявку.</param>
		[OperationContract(IsOneWay = true)]
		void JoinRejected(long originalJoinId, ChatJoin join);

		/// <summary>
		/// Добавлен новый пользователь в комнату.
		/// </summary>
		/// <param name="userId">Идентификатор пользователя.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		/// <param name="addedBy">Идентификатор пользователя, добавивший нового автора.
		/// Значение равно null, если пользователь добавился самостоятельно в публичную комнату.</param>
		[OperationContract(IsOneWay = true)]
		void AuthorAdded(long userId, long roomId, long? addedBy);

		/// <summary>
		/// Пользователь удален из комнаты.
		/// </summary>
		/// <param name="userId">Идентификатор пользователя.</param>
		/// <param name="roomId">Идентификатор комнаты.</param>
		/// <param name="deletedBy">Идентификатор пользователя, удалившего автора.
		/// Значение равно null, если пользователь самостоятельно вышел из комнаты.</param>
		[OperationContract(IsOneWay = true)]
		void AuthorDeleted(long userId, long roomId, long? deletedBy);
	}
}