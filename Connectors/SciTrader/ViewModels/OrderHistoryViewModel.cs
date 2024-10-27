using DevExpress.Mvvm.POCO;
using System.Collections.Generic;
using SciTrader.Data;

namespace SciTrader.ViewModels {
    public class OrderHistoryViewModel {
        public static OrderHistoryViewModel Create(List<OrderHistoryData> orders) {
            return ViewModelSource.Create(() => new OrderHistoryViewModel(orders));
        }

        readonly List<OrderHistoryData> ordersDataSource;

        public List<OrderHistoryData> OrdersDataSource { get { return ordersDataSource; } }

        public OrderHistoryViewModel(List<OrderHistoryData> orders) {
            ordersDataSource = orders;
        }
    }
}
