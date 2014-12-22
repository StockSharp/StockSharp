namespace SampleIQFeed
{
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	public partial class FindSecurityWindow
	{
		public FindSecurityWindow()
		{
			InitializeComponent();

			SecType.SetDataSource<SecurityTypes>();
			SecType.SetSelectedValue<SecurityTypes>(SecurityTypes.Stock);

			SecCode.Text = "AAPL";
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			var criteria = new Security
			{
				Type = (SecurityTypes)SecType.SelectedValue,
				Code = SecCode.Text,
			};

			MainWindow.Instance.Trader.LookupSecurities(criteria);
			DialogResult = true;
		}

		private void SecCode_TextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void TryEnableOk()
		{
			Ok.IsEnabled = !SecCode.Text.IsEmpty();
		}
	}
}