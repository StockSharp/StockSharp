namespace SampleSterling
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

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				Connector = MainWindow.Instance.Trader,
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void NewStopOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderConditionalWindow
			{
				Order = new Order
				{
					Security = SecurityPicker.SelectedSecurity,
					Type = OrderTypes.Conditional,
				},
				Connector = MainWindow.Instance.Trader,
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			NewOrder.IsEnabled = NewStopOrder.IsEnabled = security != null;
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			new FindSecurityWindow().ShowModal(this);
		}
	}
}