using SciTrader.ViewModels;
using DevExpress.Xpf.Charts;
using System.Windows.Controls;

namespace SciTrader.Views {
    public partial class MarketDepthView : UserControl {

        public ChartControl Chart { get { return null; } }

        public MarketDepthView() {
            InitializeComponent();
            DataContext = new MarketDepthViewModel();
        }
    }
}
