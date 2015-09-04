namespace SampleIQFeed
{
	using System;
	using System.Windows;

	public partial class NewsWindow
	{
		public NewsWindow()
		{
			InitializeComponent();
		}

		private void HistoryNews_OnClick(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.Trader.RequestNews((DateTime)DatePicker.Value);
		}

		private void DatePicker_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			HistoryNews.IsEnabled = DatePicker.Value != null;
		}
	}
}