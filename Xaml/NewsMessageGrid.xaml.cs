namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Interop;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Table showing (<see cref="NewsMessage"/>.
	/// </summary>
	public partial class NewsMessageGrid
	{
		/// <summary>
		/// The command for the news request.
		/// </summary>
		public static RoutedCommand RequestStoryCommand = new RoutedCommand();

		/// <summary>
		/// The command for the news link opening.
		/// </summary>
		public static RoutedCommand OpenUrlCommand = new RoutedCommand();

		private readonly ThreadSafeObservableCollection<NewsMessage> _messages;

		/// <summary>
		/// Initializes a new instance of the <see cref="NewsMessageGrid"/>.
		/// </summary>
		public NewsMessageGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<NewsMessage>();
			ItemsSource = itemsSource;

			_messages = new ThreadSafeObservableCollection<NewsMessage>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// The maximum number of rows to display. The -1 value means an unlimited amount. The default is 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _messages.MaxCount; }
			set { _messages.MaxCount = value; }
		}

		/// <summary>
		/// The list of messages added to the table.
		/// </summary>
		public IListEx<NewsMessage> Messages
		{
			get { return _messages; }
		}

		/// <summary>
		/// The selected message.
		/// </summary>
		public NewsMessage SelectedMessage
		{
			get { return SelectedMessages.FirstOrDefault(); }
		}

		/// <summary>
		/// Selected messages.
		/// </summary>
		public IEnumerable<NewsMessage> SelectedMessages
		{
			get { return SelectedItems.Cast<NewsMessage>(); }
		}

		private void ExecutedRequestStoryCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedMessages.Where(n => n.Story.IsEmpty()).ForEach(m => ConfigManager.GetService<IConnector>().RequestNewsStory(m.ToNews()));
		}

		private void CanExecuteRequestStoryCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = ConfigManager.IsServiceRegistered<IConnector>() && SelectedMessages.Any(n => n.Story.IsEmpty());
		}

		private void CanExecuteOpenUrlCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			var news = SelectedMessages;
			e.CanExecute = news.Count() == 1 && SelectedMessage.Url != null;
		}

		private void ExecutedOpenUrlCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedMessage.Url.OpenLinkInBrowser();
		}
	}
}