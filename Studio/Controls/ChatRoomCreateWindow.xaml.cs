using Ecng.Common;

namespace StockSharp.Studio.Controls
{
	using System.Linq;
	using System.Windows.Input;

	using StockSharp.Community;

	internal partial class ChatRoomCreateWindow
	{
		public static RoutedCommand OkCommand = new RoutedCommand();
		public static RoutedCommand CancelCommand = new RoutedCommand();

		public ChatRoom ChatRoom { get; set; }

		public ChatRoomCreateWindow(ChatClient client)
		{
			DataContext = ChatRoom = new ChatRoom();

			InitializeComponent();

			ParentRoom.ItemsSource = new[] { new ChatRoom {Name = "", Id = 0} }.Concat(client.GrantedRooms).ToList();
		}

		private void ExecutedOkCommand(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CanExecuteOkCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !ChatRoom.Name.IsEmpty() && !ChatRoom.Description.IsEmpty();
		}

		private void ExecutedCancelCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}

		private void CanExecuteCancelCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
	}
}
