namespace StockSharp.Studio
{
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core;
	using StockSharp.Xaml.Diagram;

	public partial class ContentDocumentWindow : IContentWindow
	{
		public ContentDocumentWindow()
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

				ApplyContext(Tag);
			}
		}

		private void ApplyContext(object context)
		{
			var isStrategy = context is StrategyContainer;

			if (isStrategy)
				this.SetBindings(TitleProperty, context, "Name", BindingMode.OneWay);

			HeaderTemplate = context is Security
				? (DataTemplate)FindResource("IndexSecurityHeaderTemplate")
				: (isStrategy
					? (DataTemplate)FindResource("StrategyHeaderTemplate")
					: (context is CompositionDiagramElement
						? (DataTemplate)FindResource("CompositionHeaderTemplate")
						: (DataTemplate)FindResource("DefaultHeaderTemplate")));
		}
	}
}