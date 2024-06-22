namespace StockSharp.Samples.Strategies.LiveOptionsQuoting;

using StockSharp.Messages;

public partial class QuotesWindow
{
	public QuotesWindow()
	{
		InitializeComponent();
	}

	public void Update(IOrderBookMessage depth)
	{
		DepthCtrl.UpdateDepth(depth);
	}
}