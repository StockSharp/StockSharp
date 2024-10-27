using System.Collections.Generic;
using DevExpress.Mvvm.POCO;
using SciTrader.Data;
using SciTrader.DataSources;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Charts;

namespace SciTrader.ViewModels {
    public class MarketDepthViewModel {
        public static MarketDepthViewModel Create() {
            return ViewModelSource.Create(() => new MarketDepthViewModel());
        }

        readonly MarketDepthDataSource dataSource;

        public ObservableCollectionCore<DepthData> BidChartDataSource { get { return dataSource.BidChartDataSource; } }
        public ObservableCollectionCore<DepthData> AskChartDataSource { get { return dataSource.AskChartDataSource; } }

        public MarketDepthViewModel() {
            dataSource = new MarketDepthDataSource();
        }

        public void UpdateData(List<TransactionData> sellOrders, List<TransactionData> buyOrders) {
            dataSource.UpdateData(sellOrders, buyOrders);
        }
        public void AfterLoadLayout(object sender) {
            ((ChartControl)sender).Diagram.Series[0].DataSource = BidChartDataSource;
            ((ChartControl)sender).Diagram.Series[1].DataSource = AskChartDataSource;
        }
    }
}
