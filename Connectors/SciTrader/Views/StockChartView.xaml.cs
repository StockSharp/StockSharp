using DevExpress.Xpf.Charts;
using System.Windows.Controls;

namespace SciTrader.Views {
    public partial class StockChartView : UserControl {        
        
        public ChartControl Chart { get { return chartControl; } }

        public StockChartView() {
            InitializeComponent();
        }
    }
}
