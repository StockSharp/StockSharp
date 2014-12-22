namespace StockSharp.Studio.Controls
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Runtime.InteropServices;

	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Community;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3205Key)]
	[DescriptionLoc(LocalizedStrings.Str3206Key)]
	[Icon("Images/chat_24x24.png")]
	[Guid("82C59D8C-D3F8-4294-97E8-B1F4BFD27EB0")]
	public partial class ChatPanel
	{
		public static RoutedCommand CreateRoomCommand = new RoutedCommand();
		public static RoutedCommand DeleteRoomCommand = new RoutedCommand();
		public static RoutedCommand JoinRoomCommand = new RoutedCommand();
		public static RoutedCommand LeaveRoomCommand = new RoutedCommand();
		public static RoutedCommand SendMessageCommand = new RoutedCommand();
		public static RoutedCommand AcceptJoinCommand = new RoutedCommand();
		public static RoutedCommand RejectJoinCommand = new RoutedCommand();

		private readonly ThreadSafeObservableCollection<RoomItem> _chatRooms;

		public event Action<bool> UnreadMessages;

		private RoomItem SelectedRoom
		{
			get { return ChatRoomsTree == null ? null : ChatRoomsTree.SelectedItem as RoomItem; }
		}

		public ChatPanel()
		{
			InitializeComponent();

			var context = new ObservableCollectionEx<RoomItem>();
			ChatRoomsTree.DataContext = context;

			_chatRooms = new ThreadSafeObservableCollection<RoomItem>(context);

			WhenLoaded(ChatControl_OnLoaded);
		}

		private static ChatClient Client
		{
			get { return ConfigManager.GetService<ChatClient>(); }
		}

		private void ChatControl_OnLoaded()
		{
			var client = Client;

			client.AuthorAdded += OnAuthorAdded;
			client.AuthorDeleted += OnAuthorDeleted;
			client.JoinAccepted += OnJoinAccepted;
			client.JoinRejected += OnJoinRejected;
			client.JoinSended += OnJoinSended;
			client.LoggedIn += OnLoggedIn;
			client.LoggedOut += OnLoggedOut;
			client.MessageCreated += OnMessageCreated;
			client.MessageUpdated += OnMessageUpdated;
			client.MessageDeleted += OnMessageDeleted;
			client.RoomCreated += OnRoomCreated;
			client.RoomDeleted += OnRoomDeleted;
			client.RoomUpdated += OnRoomUpdated;

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.Register<LoggedInCommand>(this, false, cmd =>
			{
				IsEnabled = true;

				var rooms = _chatRooms.ToDictionary(i => i.Room, i => i);

				foreach (var room in client.AllRooms)
				{
					var item = rooms.TryGetValue(room);

					if (item != null)
					{
						rooms.Remove(room);
						continue;
					}
					
					item = new RoomItem(room);
					item.Users.AddRange(client.GetAuthors(room));
					_chatRooms.Add(item);
				}

				_chatRooms.RemoveRange(rooms.Values);
				UpdateTitle();
			});
			cmdSvc.Register<LoggedOutCommand>(this, false, cmd =>
			{
				IsEnabled = false;
				UpdateTitle();
			});

			IsEnabled = ConfigManager.GetService<AuthenticationClient>().IsLoggedIn;
			UpdateTitle();

			if (IsEnabled)
				return;

			var res = new MessageBoxBuilder()
					.Owner(this)
					.Text(LocalizedStrings.Str3207)
					.Warning()
					.YesNo()
					.Show();

			if (res == MessageBoxResult.Yes)
				cmdSvc.Process(this, new LogInCommand());
		}

		#region Chat

		private void OnRoomUpdated(ChatRoom room)
		{
		}

		private void OnRoomDeleted(ChatRoom room)
		{
			_chatRooms.RemoveWhere(r => r.Room == room);
		}

		private void OnRoomCreated(ChatRoom room)
		{
			var item = new RoomItem(room);

			item.Users.AddRange(Client.GetAuthors(room));

			_chatRooms.Add(item);
		}

		private void OnMessageCreated(ChatMessage message)
		{
			var item = _chatRooms.FirstOrDefault(r => r.Room.Id == message.RoomId);

			if (item == null)
				return;

			item.Messages.Add(message);

			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				if (item != SelectedRoom)
				{
					item.HasNewItems = true;
					UnreadMessages.SafeInvoke(true);
				}
				else
				{
					var scroll = Messages.FindVisualChild<ScrollViewer>();
					if (scroll != null)
						scroll.ScrollToEnd();
				}
			});
		}

		private void OnMessageUpdated(ChatMessage message)
		{
			
		}

		private void OnMessageDeleted(ChatMessage message)
		{
			
		}

		private void OnLoggedOut(User user)
		{
		}

		private void OnLoggedIn(User user)
		{
		}

		private void OnJoinSended(ChatJoin join)
		{
			var item = _chatRooms.FirstOrDefault(r => r.Room.Id == join.RoomId);

			if (item == null)
				return;

			item.Messages.Add(join);
		}

		private void OnJoinRejected(ChatJoin request, ChatJoin response)
		{
			var item = _chatRooms.FirstOrDefault(r => r.Room.Id == response.RoomId);

			if (item == null)
				return;

			item.Messages.Add(response);
		}

		private void OnJoinAccepted(ChatJoin join)
		{
			var item = _chatRooms.FirstOrDefault(r => r.Room.Id == join.RoomId);

			if (item == null)
				return;

			item.Messages.Add(join);
		}

		private void OnAuthorDeleted(User author, ChatRoom room, User by)
		{
			var chatRoom = _chatRooms.First(r => r.Room == room);

			chatRoom
				.Users
				.Remove(author);

			chatRoom.Messages.Add(new ChatMessage
			{
				Body = LocalizedStrings.Str3208Params.Put(author.Name),
				CreationDate = TimeHelper.Now,
			});
		}

		private void OnAuthorAdded(User author, ChatRoom room, User by)
		{
			var chatRoom = _chatRooms.First(r => r.Room == room);

			chatRoom
				.Users
				.Add(author);

			chatRoom.Messages.Add(new ChatMessage
			{
				Body = LocalizedStrings.Str3209Params.Put(author.Name),
				CreationDate = TimeHelper.Now,
			});
		}

		#endregion

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.UnRegister<LoggedInCommand>(this);
			cmdSvc.UnRegister<LoggedOutCommand>(this);

			var client = Client;

			client.AuthorAdded -= OnAuthorAdded;
			client.AuthorDeleted -= OnAuthorDeleted;
			client.JoinAccepted -= OnJoinAccepted;
			client.JoinRejected -= OnJoinRejected;
			client.JoinSended -= OnJoinSended;
			client.LoggedIn -= OnLoggedIn;
			client.LoggedOut -= OnLoggedOut;
			client.MessageCreated -= OnMessageCreated;
			client.MessageUpdated -= OnMessageUpdated;
			client.MessageDeleted -= OnMessageDeleted;
			client.RoomCreated -= OnRoomCreated;
			client.RoomDeleted -= OnRoomDeleted;
			client.RoomUpdated -= OnRoomUpdated;
		}

		private void UpdateTitle()
		{
			Title = LocalizedStrings.Str3210Params.Put(IsEnabled ? LocalizedStrings.Str3211 : LocalizedStrings.Str3212);
		}

		private void ChatRoomsTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Messages.ItemsSource = SelectedRoom.Messages;
			Users.ItemsSource = SelectedRoom.Users;

			Message.Hint = Client.GrantedRooms.Contains(SelectedRoom.Room)
				               ? LocalizedStrings.Str3213
				               : LocalizedStrings.Str3214;

			Messages.ScrollIntoView(SelectedRoom.LastVisibleMessage);
		}

		private void Messages_OnScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (SelectedRoom == null)
				return;

			ListBoxItem lastItem = null;
			var stackPanel = Messages.FindVisualChild<VirtualizingStackPanel>();

			for (var i = 0; i < stackPanel.Children.Count; i++)
			{
				if (i >= stackPanel.VerticalOffset && i < stackPanel.VerticalOffset + stackPanel.ViewportHeight)
				{
					lastItem = stackPanel.Children[i] as ListBoxItem;
				}
			}

			if (lastItem != null)
			{
				SelectedRoom.LastVisibleMessage = lastItem.DataContext as ChatMessage;

				if (SelectedRoom.LastVisibleMessage == SelectedRoom.Messages.LastOrDefault())
				{
					SelectedRoom.HasNewItems = false;
				}

				UnreadMessages.SafeInvoke(_chatRooms.Any(r => r.HasNewItems));
			}
		}

		private void Message_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && CanSendMessage())
				SendMessage();
		}

		private bool CanSendMessage()
		{
			return SelectedRoom != null && Client.GrantedRooms.Contains(SelectedRoom.Room);
		}

		private void SendMessage()
		{
			Client.SendMessage(SelectedRoom.Room, Message.Text);
			Message.Text = string.Empty;
		}

		#region Commands

		private void ExecutedJoinRoomCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Client.Join(new ChatJoin
			{
				AuthorId = Client.UserId,
				Body = LocalizedStrings.Str3215Params.Put(ConfigManager.GetService<AuthenticationClient>().Credentials.Login),
				CreationDate = TimeHelper.Now,
				RoomId = SelectedRoom.Room.Id
			});
		}

		private void CanExecuteJoinRoomCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedRoom != null && !Client.GrantedRooms.Contains(SelectedRoom.Room);
		}

		private void ExecutedLeaveRoomCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Client.Leave(SelectedRoom.Room);
		}

		private void CanExecuteLeaveRoomCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedRoom != null && Client.GrantedRooms.Contains(SelectedRoom.Room);
		}

		private void ExecutedSendMessageCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SendMessage();
		}

		private void CanExecuteSendMessageCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = CanSendMessage();
		}

		private void ExecutedAcceptJoinCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Client.Accept((ChatJoin)e.Parameter);
		}

		private void CanExecuteAcceptJoinCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedRejectJoinCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Client.Reject((ChatJoin)e.Parameter, new ChatJoin
			{
				AuthorId = Client.UserId,
				Body = LocalizedStrings.Str3216,
			});
		}

		private void CanExecuteRejectJoinCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedCreateRoomCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var wnd = new ChatRoomCreateWindow(Client);

			if (wnd.ShowModal(this))
			{
				Client.CreateRoom(wnd.ChatRoom);
			}
		}

		private void CanExecuteCreateRoomCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedDeleteRoomCommand(object sender, ExecutedRoutedEventArgs e)
		{
		}

		private void CanExecuteDeleteRoomCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
		}

		#endregion
	}

	internal sealed class RoomItem : NotifiableObject
	{
		private bool _hasNewItems;

		public RoomItem(ChatRoom room)
		{
			Room = room;
			Messages = new ThreadSafeObservableCollection<ChatMessage>(new ObservableCollectionEx<ChatMessage>());
			Users = new ThreadSafeObservableCollection<User>(new ObservableCollectionEx<User>());
		}

		public ChatRoom Room { get; private set; }
		public ThreadSafeObservableCollection<ChatMessage> Messages { get; private set; }
		public ThreadSafeObservableCollection<User> Users { get; private set; }

		public ChatMessage LastVisibleMessage { get; set; }

		public bool HasNewItems
		{
			get { return _hasNewItems; }
			set
			{
				_hasNewItems = value;
				NotifyChanged("HasNewItems");
			}
		}
	}

	internal class MessageTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ChatMessageTemplate { get; set; }

		public DataTemplate JoinMessageTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is ChatJoin)
				return JoinMessageTemplate;

			return ChatMessageTemplate;
		}
	}
}
