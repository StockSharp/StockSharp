namespace StockSharp.Studio
{
	using StockSharp.Studio.Core;

	public partial class ContentToolWindow : IContentWindow
	{
		public ContentToolWindow()
		{
			InitializeComponent();
		}

	    public string Id { get; set; }

	    public IStudioControl Control
		{
			get { return (IStudioControl)DataContext; }
			set
			{
				DataContext = value;
				Content = value;
			}
		}
	}
}