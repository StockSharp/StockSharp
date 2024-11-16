using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Docking.Base;
using System.Windows.Controls;

namespace SciTrader.Views {
    public partial class TabView : UserControl
    {
        public event DockItemActivatedEventHandler DockItemActivated;
        public TabView() {
            InitializeComponent();
        }

        private void DockLayoutManager_DockItemActivated(object sender, DockItemActivatedEventArgs e)
        {
            // Check if the activated item contains a content
            if (e.Item is LayoutPanel panel && panel.Content is UserControl view)
            {
                // Now you can access the UserControl (view) inside the activated DockItem
                // For example, if it's a specific UserControl, you can cast it and interact with it
                if (view is StockSharpChartView chartView)
                {
                    // Now you can access properties or methods of the UserControl
                    chartView.LoadChart();
                }
            }
            // Raise your custom event here, passing the event args along.
            DockItemActivated?.Invoke(sender, e);
        }
    }
}
