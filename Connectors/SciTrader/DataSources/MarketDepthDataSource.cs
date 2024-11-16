using System.Collections.Generic;
using System.Linq;
using SciTrader.Data;
using DevExpress.Xpf.Core;

namespace SciTrader.DataSources {
    public class MarketDepthDataSource {
        readonly ObservableCollectionCore<DepthData> bidChartDataSource = new ObservableCollectionCore<DepthData>();
        readonly ObservableCollectionCore<DepthData> askChartDataSource = new ObservableCollectionCore<DepthData>();

        public ObservableCollectionCore<DepthData> BidChartDataSource { get { return bidChartDataSource; } }
        public ObservableCollectionCore<DepthData> AskChartDataSource { get { return askChartDataSource; } }

        public void UpdateData(List<TransactionData> sellOrders, List<TransactionData> buyOrders) {
            askChartDataSource.BeginUpdate();
            askChartDataSource.Clear();
            TransactionData lastSell = sellOrders[0];
            IEnumerable<TransactionData> reverseSells = sellOrders.AsEnumerable().Reverse();
            TransactionData firstSell = reverseSells.First();
            double currentSellPrice = firstSell.Price;
            double summarySellVolume = 0;
            foreach (TransactionData transaction in reverseSells) {
                if (transaction.Price != currentSellPrice) {                    
                    askChartDataSource.Add(new DepthData(currentSellPrice, summarySellVolume));
                    summarySellVolume += transaction.Volume;
                    currentSellPrice = transaction.Price;
                }
                else
                    summarySellVolume += transaction.Volume;
                if(transaction == lastSell)
                    askChartDataSource.Add(new DepthData(currentSellPrice, summarySellVolume));
            }                
            askChartDataSource.EndUpdate();
            bidChartDataSource.BeginUpdate();
            bidChartDataSource.Clear();            
            TransactionData firstBuy = buyOrders[0];
            TransactionData lastBuy = buyOrders.Last();
            double currentBuyPrice = firstBuy.Price;
            double summaryBuyVolume = 0;
            foreach (TransactionData transaction in buyOrders) {
                if (transaction.Price != currentBuyPrice) {
                    bidChartDataSource.Add(new DepthData(currentBuyPrice, summaryBuyVolume));
                    summaryBuyVolume += transaction.Volume;
                    currentBuyPrice = transaction.Price;
                }
                else
                    summaryBuyVolume += transaction.Volume;
                if (transaction == lastBuy)
                    bidChartDataSource.Add(new DepthData(currentBuyPrice, summaryBuyVolume));
            }
            bidChartDataSource.EndUpdate();
        }
        public void ClearData() {
            askChartDataSource.Clear();
            bidChartDataSource.Clear();
        }
    }
}
