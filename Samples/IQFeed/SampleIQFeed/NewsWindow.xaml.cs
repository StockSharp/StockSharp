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

		private void NistoryNews_OnClick(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.Trader.RequestNews((DateTime)DatePicker.Value);
		}
	}
}