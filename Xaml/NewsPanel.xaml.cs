namespace StockSharp.Xaml
{
	using System.Windows.Controls;

	using Ecng.Common;

	/// <summary>
	/// Визуальная панель с новостями.
	/// </summary>
	public partial class NewsPanel
	{
		/// <summary>
		/// Создать <see cref="NewsPanel"/>.
		/// </summary>
		public NewsPanel()
		{
			InitializeComponent();
		}

		private void NewsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var news = NewsGrid.FirstSelectedNews;

			var html = "<HTML/>";

			if (news != null && !news.Story.IsEmpty())
				html = "<meta http-equiv=Content-Type content='text/html;charset=UTF-8'>" + news.Story;

			NewsBrowser.NavigateToString(html);
		}
	}
}