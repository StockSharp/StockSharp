using DevExpress.Mvvm.POCO;
using System.Collections.ObjectModel;
using SciTrader.DataSources;
using System.Collections.Generic;

namespace SciTrader.ViewModels {
    public class TradesViewModel {
        public static TradesViewModel Create() {
            return ViewModelSource.Create(() => new TradesViewModel());
        }

        ObservableCollection<ClosedTransactionData> tradesDataSource = new ObservableCollection<ClosedTransactionData>();        

        public ObservableCollection<ClosedTransactionData> TradesDataSource { get { return tradesDataSource; } }        
        
        public void Init(List<ClosedTransactionData> closedTransactions) {
            foreach (ClosedTransactionData transaction in closedTransactions)
                tradesDataSource.Add(transaction);
        }
        public void UpdateData(ClosedTransactionData transaction) {
            tradesDataSource.Insert(0, transaction);
        }
    }
}
