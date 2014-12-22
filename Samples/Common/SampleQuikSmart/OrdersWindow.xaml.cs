namespace SampleQuikSmart
{
	using System.Collections.Generic;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	public partial class OrdersWindow
	{
		public OrdersWindow()
		{
			InitializeComponent();
		}

		private void OrderGrid_OnOrderCanceling(IEnumerable<Order> orders)
		{
			orders.ForEach(MainWindow.Instance.Connector.CancelOrder);
		}
	}
}