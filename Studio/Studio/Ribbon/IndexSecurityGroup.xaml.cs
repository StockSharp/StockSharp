namespace StockSharp.Studio.Ribbon
{
	using ActiproSoftware.Windows.Controls.Ribbon.Controls;

	public partial class IndexSecurityGroup
	{
		public IndexSecurityGroup()
		{
			InitializeComponent();
		}

		private void SourceElements_OnClick(object sender, ExecuteRoutedEventArgs e)
		{
			((Button)sender).IsChecked = !((Button)sender).IsChecked;
		}
	}
}
