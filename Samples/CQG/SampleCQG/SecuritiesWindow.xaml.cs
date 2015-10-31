namespace SampleCQG
{
	using System.Windows;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader)
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		void NewStopOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderConditionalWindow
			{
				Order = new Order
				{
					Security = SecurityPicker.SelectedSecurity,
					Type = OrderTypes.Conditional,
				},
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
				Adapter = MainWindow.Instance.Trader.Adapter
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			NewStopOrder.IsEnabled = NewOrder.IsEnabled = security != null;
		}
	}
}