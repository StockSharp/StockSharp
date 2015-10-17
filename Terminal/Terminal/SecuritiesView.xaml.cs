using Ecng.Configuration;
using Ecng.Xaml;

using StockSharp.BusinessEntities;

namespace StockSharp.Terminal
{
	public partial class SecuritiesView
	{
		private readonly MainWindow _parent;

		public SecuritiesView(MainWindow parent)
		{
			InitializeComponent();

			_parent = parent;

			NewChartCommand = new DelegateCommand(CreateNewChart, CanCreateNewChart);
			NewMarketDepthCommand = new DelegateCommand(CreateNewMarketDepth, CanCreateNewMarketDepth);

			SecurityGrid.SecurityProvider = ConfigManager.GetService<ISecurityProvider>();
		}

		public DelegateCommand NewChartCommand { private set; get; }

		public DelegateCommand NewMarketDepthCommand { private set; get; }

		private void CreateNewChart(object obj)
		{
			_parent.LayoutManager.CreateNewChart(obj as Security);
		}

		private bool CanCreateNewChart(object obj)
		{
			return true;
		}

		private void CreateNewMarketDepth(object obj)
		{
			_parent.LayoutManager.CreateNewMarketDepth(obj as Security);
		}

		private bool CanCreateNewMarketDepth(object obj)
		{
			return true;
		}
	}
}