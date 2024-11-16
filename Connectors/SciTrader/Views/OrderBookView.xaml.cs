using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using SciTrader.ViewModels;

namespace SciTrader.Views {    
    public partial class OrderBookView : UserControl {
        OrderBookViewModel ViewModel { get { return DataContext as OrderBookViewModel; } }

        public OrderBookView() {
            InitializeComponent();
        }

        void OnLoaded(object sender, RoutedEventArgs e) {
            if (ViewModel != null)
                ViewModel.OrdersSellDataSource.CollectionChanged += OnSellDataSourceChanged;
            if (ViewModel != null)
                ViewModel.OrdersBuyDataSource.CollectionChanged += OnBuyDataSourceChanged;

        }
        void OnUnloaded(object sender, RoutedEventArgs e) {
            if (ViewModel != null)
                ViewModel.OrdersSellDataSource.CollectionChanged -= OnSellDataSourceChanged;
            if (ViewModel != null)
                ViewModel.OrdersBuyDataSource.CollectionChanged -= OnBuyDataSourceChanged;
        }
        void OnSellDataSourceChanged(object sender, NotifyCollectionChangedEventArgs e) {
            gridSell.View.MoveLastRow();
        }
        void OnBuyDataSourceChanged(object sender, NotifyCollectionChangedEventArgs e) {
            gridBuy.View.MoveFirstRow();
        }
    }
}
