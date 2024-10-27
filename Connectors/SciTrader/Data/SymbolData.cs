using DevExpress.Mvvm.POCO;
using System;

namespace SciTrader.Data {
    public class SymbolData {
        public static SymbolData Create(string symbol, double basePrice, string group, double volume24) {
            return ViewModelSource.Create(() => new SymbolData(symbol, basePrice, group, volume24));
        }

        public int WatchCount { get; private set; }
        public string Symbol { get; private set; }
        public double BasePrice { get; private set; }
        public string Group { get; private set; }
        public virtual double CurrentPrice { get; set; }
        public virtual double Change24 { get; set; }
        public virtual double Volume24 { get; set; }

        public SymbolData(string symbol, double basePrice, string group, double volume24) {
            Symbol = symbol;
            BasePrice = basePrice;
            Group = group;
            Volume24 = volume24;
        }

        public void IncreaseWatchCount() {
            WatchCount++;
        }
        public void DecreaseWatchCount() {
            WatchCount--;
            if (WatchCount < 0)
                throw new InvalidOperationException();
        }
        public override string ToString() {
            return Symbol;
        }
    }
}
