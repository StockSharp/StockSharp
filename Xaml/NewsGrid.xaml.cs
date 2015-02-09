namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Interop;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Таблица, отображающая новости (<see cref="News"/>).
	/// </summary>
	public partial class NewsGrid
	{
		/// <summary>
		/// Команда на запрос текста новостей.
		/// </summary>
		public static RoutedCommand RequestStoryCommand = new RoutedCommand();

		/// <summary>
		/// Команда на открытие ссылки новости.
		/// </summary>
		public static RoutedCommand OpenUrlCommand = new RoutedCommand();

		private readonly ThreadSafeObservableCollection<News> _news;
		private readonly IConnector _connector;

		/// <summary>
		/// Создать <see cref="NewsGrid"/>.
		/// </summary>
		public NewsGrid()
		{
			InitializeComponent();

			_connector = ConfigManager.TryGetService<IConnector>();

			var itemsSource = new ObservableCollectionEx<News>();
			ItemsSource = itemsSource;

			_news = new ThreadSafeObservableCollection<News>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// Максимальное число новостей для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _news.MaxCount; }
			set { _news.MaxCount = value; }
		}

		/// <summary>
		/// Список новостей, добавленных в таблицу.
		/// </summary>
		public IListEx<News> News
		{
			get { return _news; }
		}

		/// <summary>
		/// Выбранная новость.
		/// </summary>
		public News FirstSelectedNews
		{
			get { return SelectedNews.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранный новости.
		/// </summary>
		public IEnumerable<News> SelectedNews
		{
			get { return SelectedItems.Cast<News>(); }
		}

		private void ExecutedRequestStoryCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedNews.ForEach(_connector.RequestNewsStory);
		}

		private void CanExecuteRequestStoryCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _connector != null && SelectedNews.Any(n => n.Story.IsEmpty());
		}

		private void CanExecuteOpenUrlCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			var news = SelectedNews;
			e.CanExecute = news.Count() == 1 && FirstSelectedNews.Url != null;
		}

		private void ExecutedOpenUrlCommand(object sender, ExecutedRoutedEventArgs e)
		{
			FirstSelectedNews.Url.OpenLinkInBrowser();
		}
	}
}