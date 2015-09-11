namespace StockSharp.Xaml
{
	using System.Windows.Controls;

	using Ecng.Common;

	/// <summary>
	/// The visual panel with the news.
	/// </summary>
	public partial class NewsMessagePanel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NewsMessagePanel"/>.
		/// </summary>
		public NewsMessagePanel()
		{
			InitializeComponent();
		}

		private void NewsMessageGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var news = NewsGrid.SelectedMessage;

			var html = "<HTML/>";

			if (news != null && !news.Story.IsEmpty())
				html = "<meta http-equiv=Content-Type content='text/html;charset=UTF-8'>" + news.Story;

			NewsBrowser.NavigateToString(html);
		}
	}
}
