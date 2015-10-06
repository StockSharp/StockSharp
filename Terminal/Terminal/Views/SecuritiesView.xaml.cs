namespace Terminal
{
    using System.Windows.Controls;

    public partial class SecuritiesView : UserControl
    {
        public SecuritiesView()
        {
            InitializeComponent();
            var root = Root.GetInstance();
            SecurityGrid.MarketDataProvider = root.Connector;
        }

    }
}
