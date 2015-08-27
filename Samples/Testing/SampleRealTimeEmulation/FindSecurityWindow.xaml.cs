namespace SampleRealTimeEmulation
{
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

		public Security Criteria
		{
			get
			{
				return new Security
				{
					Type = SecType.SelectedType,
					Code = SecCode.Text,
				};
			}
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