namespace SampleMultiConnection
{
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class StopOrderWindow
	{
		public StopOrderWindow()
		{
			InitializeComponent();
		}

		private void OrderGrid_OnOrderCanceling(IEnumerable<Order> orders)
		{
			orders.ForEach(MainWindow.Instance.Connector.CancelOrder);
		}

		private void OrderGrid_OnOrderReRegistering(Order order)
		{
			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
				SecurityProvider = MainWindow.Instance.Connector,
				MarketDataProvider = MainWindow.Instance.Connector,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Connector),
				Order = order.ReRegisterClone(newVolume: order.Balance),
			};

			if (window.ShowModal(this))
			{
				MainWindow.Instance.Connector.ReRegisterOrder(order, window.Order);
			}
		}
	}
}
