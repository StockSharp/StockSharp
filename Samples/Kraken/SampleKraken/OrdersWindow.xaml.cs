namespace SampleKraken
{
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class OrdersWindow
	{
		public OrdersWindow()
		{
			InitializeComponent();
		}

		private static IConnector Connector => MainWindow.Instance.Trader;

		private void OrderGrid_OnOrderCanceling(Order order)
		{
			Connector.CancelOrder(order);
		}

		private void OrderGrid_OnOrderReRegistering(Order order)
		{
			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
				SecurityProvider = Connector,
				MarketDataProvider = Connector,
				Portfolios = new PortfolioDataSource(Connector),
				Order = order.ReRegisterClone(newVolume: order.Balance),
			};

			if (window.ShowModal(this))
			{
				Connector.ReRegisterOrder(order, window.Order);
			}
		}

		private void CancelAll_OnClick(object sender, RoutedEventArgs e)
		{
			Connector.CancelOrders();
		}
	}
}