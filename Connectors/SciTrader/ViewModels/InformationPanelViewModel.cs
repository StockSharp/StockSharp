using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm.POCO;
using SciTrader.DataSources;

namespace SciTrader.ViewModels {
    public class InformationPanelViewModel {
        public static InformationPanelViewModel Create() {
            return ViewModelSource.Create(() => new InformationPanelViewModel());
        }

        readonly List<ClosedTransactionData> transactionHistory24 = new List<ClosedTransactionData>();

        public virtual double PreviousPrice { get; protected set; }
        public virtual double CurrentPrice { get; protected set; }
        public virtual double PriceDayAgo { get; protected set; }
        public virtual double Change24 { get; protected set; }
        public virtual double Change24Percent { get; protected set; }
        public virtual double High24 { get; protected set; }
        public virtual double Low24 { get; protected set; }
        public virtual double Volume24 { get; protected set; }

        public void UpdateData(double previousPrice, double currentPrice, double priceDayAgo, double change24, double high24, double low24, double volume24) {
            PreviousPrice = previousPrice;
            CurrentPrice = currentPrice;
            PriceDayAgo = priceDayAgo;
            Change24 = change24;
            Change24Percent = Change24 / PriceDayAgo * 100;
            High24 = high24;
            Low24 = low24;
            Volume24 = volume24;
        }
    }
}
