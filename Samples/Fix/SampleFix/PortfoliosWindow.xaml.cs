namespace SampleFix
{
	using System.Windows;

	using StockSharp.BusinessEntities;

	public partial class PortfoliosWindow
	{
		public PortfoliosWindow()
		{
			InitializeComponent();
		}

		private void Lookup_OnClick(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.Trader.LookupPortfolios(new Portfolio { Name = NameLike.Text });

		}
	}
}