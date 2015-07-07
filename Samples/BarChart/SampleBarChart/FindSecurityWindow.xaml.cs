namespace SampleBarChart
{
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;

	using StockSharp.Messages;

	public partial class FindSecurityWindow
	{
		public FindSecurityWindow()
		{
			InitializeComponent();

			SecCode.Text = "AAPL";
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.Trader.LookupSecurities(new SecurityLookupMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = SecCode.Text,
					BoardCode = BoardCode.Text,
				},
				TransactionId = MainWindow.Instance.Trader.TransactionIdGenerator.GetNextId(),
			});

			DialogResult = true;
		}

		private void Code_TextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void TryEnableOk()
		{
			Ok.IsEnabled = !SecCode.Text.IsEmpty() || !BoardCode.Text.IsEmpty();
		}
	}
}