#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIQFeed.SampleIQFeedPublic
File: NewsWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleIQFeed
{
	using System.Windows;

	using DevExpress.Xpf.Editors;

	public partial class NewsWindow
	{
		public NewsWindow()
		{
			InitializeComponent();
		}

		private void HistoryNews_OnClick(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.Trader.RequestNews(DatePicker.DateTime);
		}

		private void DatePicker_OnValueChanged(object sender, EditValueChangedEventArgs e)
		{
			HistoryNews.IsEnabled = e.NewValue != null;
		}
	}
}