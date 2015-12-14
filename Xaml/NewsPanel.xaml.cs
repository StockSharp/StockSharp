#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: NewsPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Windows.Controls;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The visual panel with the news.
	/// </summary>
	public partial class NewsPanel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NewsPanel"/>.
		/// </summary>
		public NewsPanel()
		{
			InitializeComponent();
		}

		/// <summary>
		/// The provider of information about news.
		/// </summary>
		public INewsProvider NewsProvider
		{
			get { return NewsGrid.NewsProvider; }
			set { NewsGrid.NewsProvider = value; }
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