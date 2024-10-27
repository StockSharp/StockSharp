using SciTrader.DataSources;
using System;

namespace SciTrader.Data {
    public class TradeHistoryData {
        public string Symbol { get; private set; }
        public DateTime Date { get; private set; }
        public TransactionDirection Type { get; private set; }
        public double Price { get; private set; }
        public double Amount { get; private set; }
        public double Total { get { return Amount * Price; } }

        public TradeHistoryData(string symbol, DateTime date, TransactionDirection type, double price, double amount) {
            Symbol = symbol;
            Date = date;
            Type = type;
            Price = price;
            Amount = amount;
        }
        public TradeHistoryData(OrderHistoryData order) {
            Symbol = order.Symbol;
            Date = order.Date;
            Type = order.Type;
            Price = order.Price;
            Amount = order.Amount;
        }
    }

    public enum OrderStatus {
        Active,
        Closed
    }

    public class OrderHistoryData : TradeHistoryData {
        public OrderStatus Status { get; private set; }

        public OrderHistoryData(string symbol, DateTime date, TransactionDirection type, double price, double amount, OrderStatus status)
            : base(symbol, date, type, price, amount) {
            Status = status;
        }
        public OrderHistoryData(OpenOrderData order) : base(order.Symbol, order.Date, order.Type, order.Price, order.Amount) {
            Status = OrderStatus.Active;
        }
    }

    public class OpenOrderData : TradeHistoryData {
        public double FilledPercent { get; private set; }

        public OpenOrderData(string symbol, DateTime date, TransactionDirection type, double price, double amount, double filledPercent)
            : base(symbol, date, type, price, amount) {
            FilledPercent = filledPercent;
        }
    }
}
