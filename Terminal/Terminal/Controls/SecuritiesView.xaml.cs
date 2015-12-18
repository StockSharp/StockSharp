#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.TerminalPublic
File: SecuritiesView.xaml.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using Ecng.Configuration;
using Ecng.Xaml;

using StockSharp.BusinessEntities;

namespace StockSharp.Terminal.Controls
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
			//_parent.LayoutManager.CreateNewChart(obj as Security);
		}

		private bool CanCreateNewChart(object obj)
		{
			return true;
		}

		private void CreateNewMarketDepth(object obj)
		{
			//_parent.LayoutManager.CreateNewMarketDepth(obj as Security);
		}

		private bool CanCreateNewMarketDepth(object obj)
		{
			return true;
		}
	}
}