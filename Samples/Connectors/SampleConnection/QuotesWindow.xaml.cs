namespace SampleConnection
{
	using System.Linq;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Xaml;
	using StockSharp.Messages;

	public partial class QuotesWindow
	{
		public QuotesWindow()
		{
			InitializeComponent();
		}

		public Security Security { get; set; }

		private static Connector Connector => MainWindow.Instance.MainPanel.Connector;

		private void DepthCtrl_MovingOrder(Order order, decimal newPrice)
		{
			Connector.ReRegisterOrderEx(order, order.ReRegisterClone(newPrice));
		}

		private void DepthCtrl_CancelingOrder(Order order)
		{
			Connector.CancelOrder(order);
		}

		private void DepthCtrl_RegisteringOrder(Sides side, decimal price)
		{
			var connector = Connector;

			var wnd = new OrderWindow
			{
				Order = new Order
				{
					Direction = side,
					Price = price,
					Security = Security,
					Portfolio = connector.Portfolios.FirstOrDefault(),
				},
			}.Init(connector);

			if (wnd.ShowModal(this))
				connector.RegisterOrder(wnd.Order);
		}

		public void ProcessOrder(Order order)
		{
			DepthCtrl.ProcessOrder(order, order.Price, order.Balance, order.State);
		}

		public void ProcessOrderFail(OrderFail fail)
		{
			DepthCtrl.ProcessOrderFail(fail, fail.Order.State);
		}
	}
}