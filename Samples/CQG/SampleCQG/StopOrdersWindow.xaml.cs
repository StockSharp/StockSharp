namespace SampleCQG
{
	using System.Windows;
	using System.Windows.Controls;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	public partial class StopOrdersWindow
	{
		public StopOrdersWindow()
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
