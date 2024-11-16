using SciTrader.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Utils;

namespace SciTrader.DataSources {
    public class TradingDataSource {
        const int initialTransactionsCount = 40;
        const int activeOrdersMinLimit = 30;
        const int initialOrdersCount = 40;

        readonly List<TransactionData> sellOrders;
        readonly List<TransactionData> buyOrders;
        readonly List<ClosedTransactionData> closedOrders;
        readonly List<ClosedTransactionData> dayHistory;
        readonly NonCryptographicRandom rnd;
        readonly double basePrice;

        TransactionDirection currentDirection;
        int currentDirectionCount;
        double currentPrice;

        public List<TransactionData> SellOrders { get { return sellOrders; } }
        public List<TransactionData> BuyOrders { get { return buyOrders; } }
        public List<ClosedTransactionData> ClosedOrders { get { return closedOrders; } }
        public double CurrentPrice { get { return currentPrice; } }
        public double PreviousPrice { get; private set; }
        public virtual double PriceDayAgo { get; private set; }
        public double Change24 { get; private set; }
        public virtual double High24 { get; private set; }
        public virtual double Low24 { get; private set; }
        public double Volume24 { get; private set; }
        public TransactionInfo OpenedTransactionInfo { get; private set; }
        public TransactionInfo ClosedTransactionInfo { get; private set; }

        public TradingDataSource(double basePrice) {
            sellOrders = new List<TransactionData>();
            buyOrders = new List<TransactionData>();
            closedOrders = new List<ClosedTransactionData>();
            dayHistory = new List<ClosedTransactionData>();
            DateTime date = DateTime.Now;
            rnd = new NonCryptographicRandom(date.Hour + date.Minute + date.Second);
            currentDirection = TransactionDirection.Buy;
            currentDirectionCount = 1;
            this.basePrice = basePrice;
            currentPrice = basePrice;
            InitTransactions();
            InitClosedOrders();
            CalculateDayHistoryData(MainViewModel.Tick);
        }

        void InitTransactions() {
            sellOrders.Clear();
            buyOrders.Clear();
            closedOrders.Clear();
            for (int i = 0; i < initialTransactionsCount; i++) {
                sellOrders.Add(CreateNewTransaction(TransactionDirection.Sell));
                buyOrders.Add(CreateNewTransaction(TransactionDirection.Buy));
            }
            sellOrders.Sort();
            buyOrders.Sort();
        }
        void InitClosedOrders() {
            for (int i = 0; i < initialOrdersCount; i++) {
                double price = currentPrice + (-2 * rnd.NextDouble() + 1) * MarketDataProvider.PriceDeviation;
                TransactionDirection direction = TransactionDirection.Buy;
                if (price > currentPrice)
                    direction = TransactionDirection.Sell;
                TransactionData transaction = new TransactionData(direction, price, rnd.Next(1, 10));
                closedOrders.Insert(0, new ClosedTransactionData(transaction, DateTime.Now));
            }
        }

        TransactionData CreateNewTransaction(TransactionDirection direction, double deviationFactor = 1.0) {
            double price = currentPrice;
            switch (direction) {
                case TransactionDirection.Sell:
                    price = currentPrice * (1 + rnd.NextDouble() * MarketDataProvider.PriceDeviation * deviationFactor);
                    break;
                case TransactionDirection.Buy:
                    price = currentPrice * (1 - rnd.NextDouble() * MarketDataProvider.PriceDeviation * deviationFactor);
                    break;
            }
            return new TransactionData(direction, price, rnd.Next(1, 10));
        }
        TransactionData GenerateTransaction() {
            currentDirectionCount--;
            if (currentDirectionCount == 0) {
                currentDirection = currentDirection == TransactionDirection.Buy ? TransactionDirection.Sell : TransactionDirection.Buy;
                currentDirectionCount = rnd.Next(1, 5);
            }
            return CreateNewTransaction(currentDirection, 0.01);
        }
        void CloseOrder() {
            if (rnd.NextDouble() < 0.5) {
                if (sellOrders.Count > activeOrdersMinLimit)
                    CloseSellOrder();
                else if (buyOrders.Count > activeOrdersMinLimit)
                    CloseBuyOrder();
            }
            else {
                if (buyOrders.Count > activeOrdersMinLimit)
                    CloseBuyOrder();
                else if (sellOrders.Count > activeOrdersMinLimit)
                    CloseSellOrder();
            }
        }
        void CloseSellOrder() {
            TransactionData lowestSell = sellOrders.Last();
            sellOrders.Remove(lowestSell);
            currentPrice = lowestSell.Price;
            closedOrders.Insert(0, new ClosedTransactionData(lowestSell, DateTime.Now));
            ClosedTransactionInfo = new TransactionInfo(lowestSell, sellOrders.Count);
        }
        void CloseBuyOrder() {
            TransactionData highestBuy = buyOrders[0];
            buyOrders.Remove(highestBuy);
            currentPrice = highestBuy.Price;
            closedOrders.Insert(0, new ClosedTransactionData(highestBuy, DateTime.Now));
            ClosedTransactionInfo = new TransactionInfo(highestBuy, 0);
        }
        void CalculateDayHistoryData(int tick) {
            DateTime dayAgoTime = DateTime.Now.AddDays(-1);
            int transactionsCount = Convert.ToInt32(Math.Round(TimeSpan.FromDays(1).TotalMilliseconds / tick));
            for (int i = 0; i < transactionsCount; i++)
                dayHistory.Add(GenerateTransaction().Close(dayAgoTime.AddMilliseconds(i * tick)));
            CalculateHighLowVolume();
        }
        void CalculateHighLowVolume() {
            High24 = dayHistory.Max(x => x.Price);
            Low24 = dayHistory.Min(x => x.Price);
            Volume24 = dayHistory.Sum(x => x.Volume);
        }
        public void UpdateDayHistorySymbolData(ClosedTransactionData lastTransaction) {
            double volumeDayAgo = dayHistory[0].Volume;
            dayHistory.RemoveAt(0);
            dayHistory.Add(lastTransaction);
            UpdateChange24();
            UpdateHighLow24();
            UpdateVolume24(volumeDayAgo);
        }
        void UpdateChange24() {
            PriceDayAgo = dayHistory[0].Price;
            Change24 = CurrentPrice - PriceDayAgo;
        }
        void UpdateHighLow24() {
            if (CurrentPrice > High24)
                High24 = CurrentPrice;
            if (CurrentPrice < Low24)
                Low24 = CurrentPrice;
        }
        void UpdateVolume24(double volumeDayAgo) {
            Volume24 += dayHistory.Last().Volume - volumeDayAgo;
        }

        public void UpdateMarketState() {
            PreviousPrice = CurrentPrice;
            CloseOrder();
            TransactionData transaction = GenerateTransaction();
            switch (transaction.Direction) {
                case TransactionDirection.Sell:
                    sellOrders.Add(transaction);
                    sellOrders.Sort();
                    OpenedTransactionInfo = new TransactionInfo(transaction, sellOrders.IndexOf(transaction));
                    break;
                case TransactionDirection.Buy:
                    buyOrders.Add(transaction);
                    buyOrders.Sort();
                    OpenedTransactionInfo = new TransactionInfo(transaction, buyOrders.IndexOf(transaction));
                    break;
            }
            UpdateDayHistorySymbolData(closedOrders[0]);
        }
    }

    public enum TransactionDirection {
        Sell,
        Buy
    }

    public class TransactionData : IComparable {
        public TransactionDirection Direction { get; private set; }
        public double Price { get; private set; }
        public double Volume { get; private set; }
        public double Total { get { return Price * Volume; } }

        public TransactionData(TransactionDirection direction, double price, double volume) {
            Direction = direction;
            Price = price;
            Volume = volume;
        }

        public int CompareTo(object obj) {
            return ((TransactionData)obj).Price.CompareTo(Price);
        }

        public ClosedTransactionData Close(DateTime time) {
            return new ClosedTransactionData(this, time);
        }
    }

    public class ClosedTransactionData : TransactionData {
        public DateTime Time { get; private set; }

        public ClosedTransactionData(TransactionData closedTransaction, DateTime time) : base(closedTransaction.Direction, closedTransaction.Price, closedTransaction.Volume) {
            Time = time;
        }
    }

    public class TransactionInfo {
        public TransactionData Transaction { get; private set; }
        public int Index { get; private set; }

        public TransactionInfo(TransactionData transaction, int index) {
            Transaction = transaction;
            Index = index;
        }
    }
}
