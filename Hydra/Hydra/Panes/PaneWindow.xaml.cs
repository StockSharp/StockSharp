namespace StockSharp.Hydra.Panes
{
	public partial class PaneWindow
	{
		public PaneWindow()
		{
			InitializeComponent();
		}

		public IPane Pane
		{
			get { return (IPane)DataContext; }
			set { DataContext = value; }
		}
	}
}