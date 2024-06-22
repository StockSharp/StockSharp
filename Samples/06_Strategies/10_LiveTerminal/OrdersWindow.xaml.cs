namespace StockSharp.Samples.Strategies.LiveTerminal;

using StockSharp.BusinessEntities;

public partial class OrdersWindow
{
	public OrdersWindow()
	{
		InitializeComponent();
	}

	private static IConnector Connector => MainWindow.Instance.Connector;

	private void OrderGrid_OnOrderCanceling(Order order)
	{
		Connector.CancelOrder(order);
	}
}