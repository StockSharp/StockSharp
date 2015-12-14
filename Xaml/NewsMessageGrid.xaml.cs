#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: NewsMessageGrid.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Input;

	using Ecng.Collections;
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
		public IListEx<NewsMessage> Messages => _messages;

		/// <summary>
		/// The selected message.
		/// </summary>
		public NewsMessage SelectedMessage => SelectedMessages.FirstOrDefault();

		/// <summary>
		/// Selected messages.
		/// </summary>
		public IEnumerable<NewsMessage> SelectedMessages => SelectedItems.Cast<NewsMessage>();

		/// <summary>
		/// The provider of information about news.
		/// </summary>
		public INewsProvider NewsProvider { get; set; }

		private void ExecutedRequestStoryCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedMessages.Where(n => n.Story.IsEmpty()).ForEach(m => NewsProvider.RequestNewsStory(m.ToNews()));
		}

		private void CanExecuteRequestStoryCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = NewsProvider != null && SelectedMessages.Any(n => n.Story.IsEmpty());
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