namespace SampleIB
{
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

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

		private void OrderGrid_OnOrderCanceling(IEnumerable<Order> orders)
		{
			orders.ForEach(MainWindow.Instance.Trader.CancelOrder);
		}

		private void OrderGrid_OnOrderReRegistering(Order order)
		{
			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
				Connector = MainWindow.Instance.Trader,
				Order = order.ReRegisterClone(newVolume: order.Balance),
			};

			if (window.ShowModal(this))
			{
				MainWindow.Instance.Trader.ReRegisterOrder(order, window.Order);
			}
		}
	}
}