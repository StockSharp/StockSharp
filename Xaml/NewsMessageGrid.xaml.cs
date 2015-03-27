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
	/// Таблица, отображающая (<see cref="NewsMessage"/>.
	/// </summary>
	public partial class NewsMessageGrid
	{
		/// <summary>
		/// Команда на запрос текста новостей.
		/// </summary>
		public static RoutedCommand RequestStoryCommand = new RoutedCommand();

		/// <summary>
		/// Команда на открытие ссылки новости.
		/// </summary>
		public static RoutedCommand OpenUrlCommand = new RoutedCommand();

		private readonly ThreadSafeObservableCollection<NewsMessage> _messages;

		/// <summary>
		/// Создать <see cref="NewsMessageGrid"/>.
		/// </summary>
		public NewsMessageGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<NewsMessage>();
			ItemsSource = itemsSource;

			_messages = new ThreadSafeObservableCollection<NewsMessage>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// Максимальное число строк для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _messages.MaxCount; }
			set { _messages.MaxCount = value; }
		}

		/// <summary>
		/// Список сообщений, добавленных в таблицу.
		/// </summary>
		public IListEx<NewsMessage> Messages
		{
			get { return _messages; }
		}

		/// <summary>
		/// Выбранное сообщение.
		/// </summary>
		public NewsMessage SelectedMessage
		{
			get { return SelectedMessages.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранные сообщения.
		/// </summary>
		public IEnumerable<NewsMessage> SelectedMessages
		{
			get { return SelectedItems.Cast<NewsMessage>(); }
		}

		private void ExecutedRequestStoryCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedMessages.ForEach(m => ConfigManager.GetService<IConnector>().RequestNewsStory(m.ToNews()));
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