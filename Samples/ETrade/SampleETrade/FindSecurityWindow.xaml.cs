namespace SampleETrade
{
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	public partial class FindSecurityWindow
	{
		public FindSecurityWindow()
		{
			InitializeComponent();

			SecCode.Text = "AAPL";
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			var criteria = new Security
			{
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