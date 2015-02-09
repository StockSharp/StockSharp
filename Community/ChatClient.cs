namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.ServiceModel;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Logging;

	using StockSharp.Localization;

	/// <summary>
	/// Клиент для доступа к <see cref="IChatService"/>.
	/// </summary>
	[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
	public class ChatClient : BaseCommunityClient<IChatService>, IChatServiceCallback
	{
		private readonly CachedSynchronizedDictionary<long, ChatRoom> _rooms = new CachedSynchronizedDictionary<long, ChatRoom>();
		private readonly CachedSynchronizedSet<long> _accessedRooms = new CachedSynchronizedSet<long>();
		private readonly CachedSynchronizedDictionary<long, User> _users = new CachedSynchronizedDictionary<long, User>();
		private readonly CachedSynchronizedDictionary<long, ChatJoin> _avaitingJoins = new CachedSynchronizedDictionary<long, ChatJoin>();
		private readonly CachedSynchronizedDictionary<long, ChatJoin> _myJoins = new CachedSynchronizedDictionary<long, ChatJoin>();

		private readonly EventDispatcher _eventDispatcher;

		/// <summary>
		/// Создать <see cref="ChatClient"/>.
		/// </summary>
		public ChatClient()
			: this("net.tcp://stocksharp.com:8100".To<Uri>())
		{
		}

		/// <summary>
		/// Создать <see cref="ChatClient"/>.
		/// </summary>
		/// <param name="address">Адрес сервера.</param>
		public ChatClient(Uri address)
			: base(address, "chat", true)
		{
			_eventDispatcher = new EventDispatcher(ex => ex.LogError());
		}

		/// <summary>
		/// Создать WCF канал.
		/// </summary>
		/// <returns>WCF канал.</returns>
		protected override ChannelFactory<IChatService> CreateChannel()
		{
			return new DuplexChannelFactory<IChatService>(this, new NetTcpBinding(SecurityMode.None), new EndpointAddress(Address));
		}

		/// <summary>
		/// Подключиться.
		/// </summary>
		public override void Connect()
		{
			base.Connect();

			var rooms = Invoke(f => f.GetRooms(SessionId));

			foreach (var room in rooms)
				_rooms.Add(room.Id, room);

			var ids = Invoke(f => f.GetJoinedRooms(SessionId));
			_accessedRooms.AddRange(ids);

			var joins = Invoke(f => f.GetAvatingJoins(SessionId));

			foreach (var id in joins)
				_avaitingJoins.Add(id, GetJoin(id));

			joins = Invoke(f => f.GetMyJoins(SessionId));

			foreach (var id in joins)
				_myJoins.Add(id, GetJoin(id));

			foreach (var roomId in _accessedRooms)
				Invoke(f => f.Subscribe(SessionId, roomId));
		}

		/// <summary>
		/// Максимальный размер сообщений, который можно отправить в чат.
		/// </summary>
		public const int MaxBodySize = 2048;

		/// <summary>
		/// Все комнаты.
		/// </summary>
		public IEnumerable<ChatRoom> AllRooms
		{
			get
			{
				ForceInit();
				return _rooms.CachedValues;
			}
		}

		/// <summary>
		/// Комнаты, к которым предоставлен доступ.
		/// </summary>
		public IEnumerable<ChatRoom> GrantedRooms
		{
			get
			{
				ForceInit();
				return _accessedRooms.Cache.Select(id => _rooms[id]);
			}
		}

		/// <summary>
		/// Заявки на присоединение от других пользователей, которые ожидают удовлетворения или отмены.
		/// </summary>
		public IEnumerable<ChatJoin> AvaitingJoins
		{
			get
			{
				ForceInit();
				return _avaitingJoins.CachedValues;
			}
		}

		/// <summary>
		/// Собственные заявки на присоединение, которые ожидают удовлетворения или отмены.
		/// </summary>
		public IEnumerable<ChatJoin> MyJoins
		{
			get
			{
				ForceInit();
				return _myJoins.CachedValues;
			}
		}

		private void ForceInit()
		{
			if (!IsConnected)
				Connect();
		}

		/// <summary>
		/// Новое сообщение.
		/// </summary>
		public event Action<ChatMessage> MessageCreated;

		/// <summary>
		/// Сообщение обновлено.
		/// </summary>
		public event Action<ChatMessage> MessageUpdated;

		/// <summary>
		/// Сообщение удалено.
		/// </summary>
		public event Action<ChatMessage> MessageDeleted;

		/// <summary>
		/// Комната создана.
		/// </summary>
		public event Action<ChatRoom> RoomCreated;

		/// <summary>
		/// Комната обновлена.
		/// </summary>
		public event Action<ChatRoom> RoomUpdated;

		/// <summary>
		/// Комната удалена.
		/// </summary>
		public event Action<ChatRoom> RoomDeleted;

		/// <summary>
		/// Пользователь вошел в чат.
		/// </summary>
		public event Action<User> LoggedIn;

		/// <summary>
		/// Пользователь вышел из чата.
		/// </summary>
		public event Action<User> LoggedOut;

		/// <summary>
		/// Новая заявка на участие.
		/// </summary>
		public event Action<ChatJoin> JoinSended;

		/// <summary>
		/// Заявка удовлетворена.
		/// </summary>
		public event Action<ChatJoin> JoinAccepted;

		/// <summary>
		/// Заявка отклонена.
		/// </summary>
		public event Action<ChatJoin, ChatJoin> JoinRejected;

		/// <summary>
		/// Добавлен новый пользователь в комнату.
		/// </summary>
		public event Action<User, ChatRoom, User> AuthorAdded;

		/// <summary>
		/// Пользователь удален из комнаты.
		/// </summary>
		public event Action<User, ChatRoom, User> AuthorDeleted;

		/// <summary>
		/// Получить информацию о пользователях в комнате.
		/// </summary>
		/// <param name="room">Комната.</param>
		/// <returns>Пользователи.</returns>
		public IEnumerable<User> GetAuthors(ChatRoom room)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			var ids = Invoke(f => f.GetAuthors(SessionId, room.Id));
			var newIds = ids.Where(id => !_users.ContainsKey(id)).ToArray();

			if (!newIds.IsEmpty())
				Invoke(f => f.GetUsers(SessionId, newIds)).ForEach(u => _users.TryAdd(u.Id, u));

			return ids.Select(GetUser);
		}

		/// <summary>
		/// Отправить сообщение в комнату.
		/// </summary>
		/// <param name="room">Комната.</param>
		/// <param name="body">Сообщение.</param>
		public void SendMessage(ChatRoom room, string body)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			SendMessage(new ChatMessage
			{
				Body = body,
				RoomId = room.Id
			});
		}

		/// <summary>
		/// Отправить сообщение в комнату.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void SendMessage(ChatMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var body = message.Body;

			if (body.IsEmpty())
				throw new ArgumentException(LocalizedStrings.MessageNoText, "message");

			if (body.Length > MaxBodySize)
				throw new ArgumentOutOfRangeException("message", MaxBodySize, LocalizedStrings.MessageTextMax);

			message.Id = Invoke(f => f.SendMessage(SessionId, message.RoomId, body));
		}

		/// <summary>
		/// Обновить сообщение в комнате.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="body">Новое тело сообщения.</param>
		/// <returns><see langword="true"/>, если сообщение было обновлено, false, если сообщение устарело и его нельзя менять.</returns>
		public bool UpdateMessage(ChatMessage message, string body)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var retVal = Invoke(f => f.UpdateMessage(SessionId, message.Id, body));

			if (retVal)
				message.Body = body;

			return retVal;
		}

		/// <summary>
		/// Отправить заявку на присоединение.
		/// </summary>
		/// <param name="join">Заявка.</param>
		public void Join(ChatJoin join)
		{
			if (join == null)
				throw new ArgumentNullException("join");

			join.Id = Invoke(f => f.Join(SessionId, join));

			if (join.Id != 0)
				_myJoins.Add(join.Id, join);

			//для открытых комнат событие JoinAccepted не вызывается
			//поэтому сразу добавляем ее в доступные комнаты
			if (_rooms[join.RoomId].IsEveryOne)
				_accessedRooms.Add(join.RoomId);
		}

		/// <summary>
		/// Покинуть комнату.
		/// </summary>
		/// <param name="room">Комната.</param>
		public void Leave(ChatRoom room)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			Invoke(f => f.Leave(SessionId, room.Id));
			_accessedRooms.Remove(room.Id);
		}

		/// <summary>
		/// Удовлетворить заявку.
		/// </summary>
		/// <param name="join">Заявка.</param>
		public void Accept(ChatJoin join)
		{
			if (join == null)
				throw new ArgumentNullException("join");

			Invoke(f => f.Accept(SessionId, join.Id));
		}

		/// <summary>
		/// Отклонить заявку.
		/// </summary>
		/// <param name="request">Заявка на присоединение.</param>
		/// <param name="response">Ответ на заявку.</param>
		public void Reject(ChatJoin request, ChatJoin response)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			if (response == null)
				throw new ArgumentNullException("response");

			response.Id = Invoke(f => f.Reject(SessionId, request.Id, response));
		}

		/// <summary>
		/// Создать комнату.
		/// </summary>
		/// <param name="room">Комната.</param>
		public void CreateRoom(ChatRoom room)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			room.Id = Invoke(f => f.CreateRoom(SessionId, room));
			//_rooms.Add(room.Id, room);
		}

		/// <summary>
		/// Обновить детали комнаты.
		/// </summary>
		/// <param name="room">Комната.</param>
		public void UpdateRoom(ChatRoom room)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			Invoke(f => f.UpdateRoom(SessionId, room));
		}

		/// <summary>
		/// Удалить комнату.
		/// </summary>
		/// <param name="room">Комната.</param>
		public void DeleteRoom(ChatRoom room)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			Invoke(f => f.DeleteRoom(SessionId, room.Id));
		}

		/// <summary>
		/// Получить информацию о пользователе.
		/// </summary>
		/// <param name="userId">Идентификатор пользователя.</param>
		/// <returns>Информация о пользователе.</returns>
		public User GetUser(long userId)
		{
			return _users.SafeAdd(userId, key => Invoke(f => f.GetUser(SessionId, userId)));
		}

		private ChatRoom GetRoom(long roomId)
		{
			return _rooms[roomId];
		}

		private ChatJoin GetJoin(long joinId)
		{
			return Invoke(f => f.GetJoin(SessionId, joinId));
		}

		void IChatServiceCallback.MessageCreated(ChatMessage message)
		{
			_eventDispatcher.Add(() => MessageCreated.SafeInvoke(message));
		}

		void IChatServiceCallback.MessageUpdated(ChatMessage message)
		{
			_eventDispatcher.Add(() => MessageUpdated.SafeInvoke(message));
		}

		void IChatServiceCallback.MessageDeleted(long messageId)
		{
			//MessageDeleted.SafeInvoke(GetMessage(messageId));
		}

		void IChatServiceCallback.RoomCreated(ChatRoom room)
		{
			_rooms.Add(room.Id, room);
			_eventDispatcher.Add(() => RoomCreated.SafeInvoke(room));
		}

		void IChatServiceCallback.RoomDeleted(long roomId)
		{
			_rooms.Remove(roomId);
			_eventDispatcher.Add(() => RoomDeleted.SafeInvoke(GetRoom(roomId)));
		}

		void IChatServiceCallback.RoomUpdated(ChatRoom room)
		{
			_eventDispatcher.Add(() => RoomUpdated.SafeInvoke(room));
		}

		void IChatServiceCallback.LoggedIn(long userId)
		{
			_eventDispatcher.Add(() => LoggedIn.SafeInvoke(GetUser(userId)));
		}

		void IChatServiceCallback.LoggedOut(long userId)
		{
			_eventDispatcher.Add(() => LoggedOut.SafeInvoke(GetUser(userId)));
		}

		void IChatServiceCallback.JoinSended(long joinId)
		{
			_eventDispatcher.Add(() =>
			{
				var join = GetJoin(joinId);
				_avaitingJoins.Add(joinId, join);
				JoinSended.SafeInvoke(join);
			});
		}

		void IChatServiceCallback.JoinAccepted(long joinId)
		{
			_accessedRooms.Add(_myJoins[joinId].RoomId);
			_eventDispatcher.Add(() => JoinAccepted.SafeInvoke(_myJoins[joinId]));
		}

		void IChatServiceCallback.JoinRejected(long originalJoinId, ChatJoin join)
		{
			_eventDispatcher.Add(() => JoinRejected.SafeInvoke(_myJoins[originalJoinId], join));
		}

		void IChatServiceCallback.AuthorAdded(long userId, long roomId, long? addedBy)
		{
			_eventDispatcher.Add(() => AuthorAdded.SafeInvoke(GetUser(userId), GetRoom(roomId), addedBy == null ? null : GetUser((long)addedBy)));
		}

		void IChatServiceCallback.AuthorDeleted(long userId, long roomId, long? deletedBy)
		{
			_eventDispatcher.Add(() => AuthorDeleted.SafeInvoke(GetUser(userId), GetRoom(roomId), deletedBy == null ? null : GetUser((long)deletedBy)));
		}
	}
}