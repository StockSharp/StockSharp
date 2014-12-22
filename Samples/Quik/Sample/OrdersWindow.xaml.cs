namespace Sample
{
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;
	using StockSharp.Localization;

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

		//private void ExecConditionOrderClick(object sender, RoutedEventArgs e)
		//{
		//	var order = OrderGrid.SelectedOrders.FirstOrDefault();

		//	if (order == null)
		//		return;

		//	var newOrder = new NewStopOrderWindow
		//	{
		//		Title = "Новая условная заявка на исполнение заявки '{0}'".Put(order.Id),
		//		ConditionOrder = order,
		//	};
		//	newOrder.ShowModal(this);
		//}

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