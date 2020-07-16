namespace SampleConnection
{
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

		private static Connector Connector => MainWindow.Instance.MainPanel.Connector;

		private void OrderGrid_OnOrderCanceling(Order order)
		{
			Connector.CancelOrder(order);
		}

		private void OrderGrid_OrderReRegistering(Order order)
		{
			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
				Order = order.ReRegisterClone(newVolume: order.Balance),
				SecurityEnabled = false,
				PortfolioEnabled = false,
				OrderTypeEnabled = false,
			}.Init(Connector);

			if (!window.ShowModal(this))
				return;

			Connector.ReRegisterOrderEx(order, window.Order);
		}
	}
}