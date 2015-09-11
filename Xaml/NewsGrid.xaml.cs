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
	/// The table showing the news (<see cref="NewsGrid.News"/>).
	/// </summary>
	public partial class NewsGrid
	{
		/// <summary>
		/// The command for the news request.
		/// </summary>
		public static RoutedCommand RequestStoryCommand = new RoutedCommand();

		/// <summary>
		/// The command for the news link opening.
		/// </summary>
		public static RoutedCommand OpenUrlCommand = new RoutedCommand();

		private readonly ThreadSafeObservableCollection<News> _news;

		/// <summary>
		/// Initializes a new instance of the <see cref="NewsGrid"/>.
		/// </summary>
		public NewsGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<News>();
			ItemsSource = itemsSource;

			_news = new ThreadSafeObservableCollection<News>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// The maximum number of news to display. The -1 value means an unlimited amount. The default value is 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _news.MaxCount; }
			set { _news.MaxCount = value; }
		}

		/// <summary>
		/// The list of news added to the table.
		/// </summary>
		public IListEx<News> News
		{
			get { return _news; }
		}

		/// <summary>
		/// Selected news item.
		/// </summary>
		public News FirstSelectedNews
		{
			get { return SelectedNews.FirstOrDefault(); }
		}

		/// <summary>
		/// Selected news items.
		/// </summary>
		public IEnumerable<News> SelectedNews
		{
			get { return SelectedItems.Cast<News>(); }
		}

		private void ExecutedRequestStoryCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedNews.Where(n => n.Story.IsEmpty()).ForEach(ConfigManager.GetService<IConnector>().RequestNewsStory);
		}

		private void CanExecuteRequestStoryCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = ConfigManager.IsServiceRegistered<IConnector>() && SelectedNews.Any(n => n.Story.IsEmpty());
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