namespace Sample
{
	using System.Windows;
	using System.Windows.Controls;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	public partial class StopOrderWindow
	{
		public StopOrderWindow()
		{
			InitializeComponent();
		}

		private Order SelectedOrder
		{
			get { return OrderGrid.SelectedOrder; }
		}

		private void CancelOrderClick(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.Trader.CancelOrder(SelectedOrder);
		}

		private void OrdersDetailsSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var order = SelectedOrder;
			CancelOrder.IsEnabled = order != null && order.State == OrderStates.Active;
		}
	}
}
