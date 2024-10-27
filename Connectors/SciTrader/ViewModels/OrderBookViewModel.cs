using DevExpress.Mvvm.POCO;
using System.Collections.ObjectModel;
using SciTrader.DataSources;
using System.Collections.Generic;

namespace SciTrader.ViewModels {
    public class OrderBookViewModel {
        public static OrderBookViewModel Create() {
            return ViewModelSource.Create(() => new OrderBookViewModel());
        }

        ObservableCollection<TransactionData> ordersSellDataSource = new ObservableCollection<TransactionData>();
        ObservableCollection<TransactionData> ordersBuyDataSource = new ObservableCollection<TransactionData>();

        public ObservableCollection<TransactionData> OrdersSellDataSource { get { return ordersSellDataSource; } }
        public ObservableCollection<TransactionData> OrdersBuyDataSource { get { return ordersBuyDataSource; } }

        public OrderBookViewModel() {            
        }

        public void Init(List<TransactionData> sellTransactions, List<TransactionData> buyTransactions) {
            foreach (TransactionData transaction in sellTransactions)
                ordersSellDataSource.Add(transaction);
            foreach (TransactionData transaction in buyTransactions)
                ordersBuyDataSource.Add(transaction);
        }

        public void UpdateData(TransactionInfo openedTransactionInfo, TransactionInfo closedTransactionInfo) {
            switch (closedTransactionInfo.Transaction.Direction) {
                case TransactionDirection.Sell:
                    ordersSellDataSource.RemoveAt(closedTransactionInfo.Index);
                    break;
                case TransactionDirection.Buy:
                    ordersBuyDataSource.RemoveAt(closedTransactionInfo.Index);
                    break;
            }
            switch (openedTransactionInfo.Transaction.Direction) {
                case TransactionDirection.Sell:
                    ordersSellDataSource.Insert(openedTransactionInfo.Index, openedTransactionInfo.Transaction);
                    break;
                case TransactionDirection.Buy:
                    ordersBuyDataSource.Insert(openedTransactionInfo.Index, openedTransactionInfo.Transaction);
                    break;
            }
        }
    }
}
