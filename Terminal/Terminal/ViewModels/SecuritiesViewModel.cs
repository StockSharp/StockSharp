namespace Terminal
{
    using System.Collections.Generic;
    using System.Windows.Controls;
    using StockSharp.BusinessEntities;
    using Ecng.Xaml;

    public class SecuritiesViewModel : ToolItemViewModel
    {
        private Root _root;
        public SecuritiesViewModel()
        {
            _root = Root.GetInstance();
            _root.Connector.LookupSecuritiesResult += securities =>
            {
                NotifyPropertyChanged("Securities");
            };

            NewChartCommand = new DelegateCommand(CreateNewChart, CanCreateNewChart);
            NewMarketDepthCommand = new DelegateCommand(CreateNewMarketDepth, CanCreateNewMarketDepth);
        }

        public IEnumerable<Security> Securities
        {
            get { return _root.Connector.Securities; }
        }

        public DelegateCommand NewChartCommand { private set; get; }

        private void CreateNewChart(object obj)
        {
            var security = obj as Security;

            if (security == null) return;

            ToolItemViewModel viewModel = new ChartViewModel() { Security = security };
            viewModel.DefaultDock = Dock.Top;
            viewModel.Title = security.Id;
            _root.ToolItems.Add(viewModel);

        }

        private bool CanCreateNewChart(object obj)
        {
            return true;
        }

        public DelegateCommand NewMarketDepthCommand { private set; get; }

        private void CreateNewMarketDepth(object obj)
        {
            var security = obj as Security;

            if (security == null) return;

            ToolItemViewModel viewModel = new MarketDepthViewModel() { Security = security };
            viewModel.DefaultDock = Dock.Top;
            viewModel.Title = security.Id;
            _root.ToolItems.Add(viewModel);

        }

        private bool CanCreateNewMarketDepth(object obj)
        {
            return true;
        }

    }
}
