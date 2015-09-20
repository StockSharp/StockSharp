using System;
using System.Text;
using System.IO;
using System.Reflection;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Anywhere
{
    /// <summary>
    ///  The write functions to text files
    /// </summary>
    public static class WtiteHelpers
    {

        static private string _outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OUTPUT"); // Data output folder path

        private static string _tradesFilePath = Path.Combine(_outputFolder, "trades.txt");
        private static string _ordersFilePath = Path.Combine(_outputFolder, "orders.txt");
        private static string _myTradesFilePath = Path.Combine(_outputFolder, "mytrades.txt");
        private static string _level1FilePath = Path.Combine(_outputFolder, "level1.txt");
        private static string _positionsFilePath = Path.Combine(_outputFolder, "positions.txt");

        static WtiteHelpers()
        {
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }
        }

        public static void WriteTrade(this Trade trade)
        {
            SaveToFile(TradeToString(trade), _tradesFilePath);
        }

        public static void WriteMyTrade(this MyTrade trade)
        {
            SaveToFile(MyTradeToString(trade), _myTradesFilePath);
        }

        public static void WriteMarketDepth(this MarketDepth depth)
        {
            DepthToFile(depth, Path.Combine(_outputFolder, string.Format("{0}_depth.txt", depth.Security.Code)));
        }

        public static void WriteLevel1(this Security security)
        {
            SaveToFile(Level1ToString(security), _level1FilePath);
        }

        public static void WriteOrder(this Order order)
        {
            SaveToFile(OrderToString(order), _ordersFilePath);
        }

        public static void WritePosition(this Position position)
        {
            SaveToFile(PositionToString(position), _positionsFilePath);
        }

        public static void WriteTrade(this ExecutionMessage tick)
        {
            SaveToFile(TradeToString(tick), _tradesFilePath);
        }

        public static void WriteMyTrade(this ExecutionMessage trade)
        {
            SaveToFile(MyTradeToString(trade), _myTradesFilePath);
        }

        public static void WriteMarketDepth(this QuoteChangeMessage quotes)
        {
            DepthToFile(QuotesToString(quotes), Path.Combine(_outputFolder, string.Format("{0}_depth.txt", quotes.SecurityId.SecurityCode)));
        }

        public static void WriteLevel1(this Level1ChangeMessage level)
        {
            SaveToFile(Level1ToString(level), _level1FilePath);
        }

        public static void WriteOrder(this ExecutionMessage order)
        {
            SaveToFile(OrderToString(order), _ordersFilePath);
        }

        public static void WritePosition(this PositionChangeMessage position)
        {
            SaveToFile(PositionToString(position), _positionsFilePath);
        }

        // returns a string of trade
        private static string TradeToString(Trade trade)
        {
            //securityId;tradeId;time;price;volume;orderdirection 
            return string.Format("{0};{1};{2};{3};{4};{5}",
                                   trade.Security.Id,
                                   trade.Id.ToString(),
                                   trade.Time.ToString(),
                                   trade.Price.ToString(),
                                   trade.Volume.ToString(),
                                   trade.OrderDirection.ToString());
        }

        // returns a string of mytrade
        private static string MyTradeToString(MyTrade trade)
        {
            //securityId;tradeId;time;volume;price;orderdirection;orderId 
            return string.Format("{0};{1};{2};{3};{4};{5};{6}",
                                   trade.Trade.Security.Id,
                                   trade.Trade.Id,
                                   trade.Trade.Time,
                                   trade.Trade.Volume,
                                   trade.Trade.Price,
                                   trade.Trade.OrderDirection.ToString(),
                                   trade.Order.Id);
        }

        // returns a string of order
        private static string OrderToString(Order order)
        {
            //orderId;transactionId;time;securityId;portfolioName;volume;balance;price;direction;type;localTime 
            return string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11}",
                                   order.Id,
                                   order.TransactionId,
                                   order.Time,
                                   order.Security.Id,
                                   order.Portfolio.Name,
                                   order.Volume,
                                   order.Balance,
                                   order.Price,
                                   order.Direction.ToString(),
                                   order.Type.ToString(),
                                   order.State.ToString(),
                                   order.LocalTime);
        }

        // returns a string of position
        private static string PositionToString(Position position)
        {
            return string.Format("{0};{1};{2};{3}",
                                position.Security.Id,
                                position.Portfolio.Name,
                                position.CurrentValue,
                                position.AveragePrice);
        }

        // returns a string of level1
        private static string Level1ToString(Security security)
        {
            return string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13}",
                                security.Id,
                                security.Board.Code,
                                security.PriceStep,
                                security.VolumeStep,
                                security.Type,
                                security.LastTrade.Price,
                                security.LastTrade.Volume,
                                security.LastTrade.Time.TimeOfDay,
                                security.LastTrade.Time.Date,
                                security.LastTrade.Time.Date,
                                security.BestBid.Price,
                                security.BestBid.Volume,
                                security.BestAsk.Price,
                                security.BestAsk.Volume
                                );
        }

        // returns a string of marketdepth quote
        private static string QuoteToString(Quote quote)
        {
            return string.Format("{0};{1};{2}{3}",
                                quote.OrderDirection.ToString(),
                                quote.Price,
                                quote.Volume,
                                Environment.NewLine);
        }

        private static string TradeToString(ExecutionMessage tick)
        {
            //securityId;tradeId;time;price;volume;orderdirection 
            return string.Format("{0};{1};{2};{3};{4};{5}",
                                   tick.SecurityId.SecurityCode,
                                   tick.TradeId,
                                   tick.ServerTime.ToString(),
                                   tick.TradePrice,
                                   tick.Volume,
                                   tick.Side.ToString());
        }

        private static string MyTradeToString(ExecutionMessage trade)
        {
            return string.Format("{0};{1};{2};{3};{4};{5};{6}",
                                   trade.SecurityId.SecurityCode,
                                   trade.TradeId.ToString(),
                                   trade.ServerTime.ToString(),
                                   trade.TradePrice.ToString(),
                                   trade.Volume.ToString(),
                                   trade.Side.ToString(),
                                   trade.OrderId);
        }

        private static string OrderToString(ExecutionMessage order)
        {
            return string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}",
                         order.OrderId,
                         order.OriginalTransactionId,
                         order.ServerTime,
                         order.SecurityId.SecurityCode,
                         order.PortfolioName,
                         order.Volume,
                         order.Balance,
                         order.Price,
                         order.OriginSide.ToString(),
                         order.OrderType.ToString(),
                         order.OrderState.ToString(),
                         order.IsCancelled.ToString(),
                         order.LocalTime);
        }

        private static string PositionToString(PositionChangeMessage position)
        {
            return string.Format("{0};{1};{2};{3};{4}",
                                position.SecurityId.SecurityCode,
                                position.PortfolioName,
                                position.Changes[PositionChangeTypes.BeginValue],
                                position.Changes[PositionChangeTypes.CurrentValue],
                                position.Changes[PositionChangeTypes.AveragePrice]);
        }

        private static string QuotesToString(QuoteChangeMessage quotes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var quote in quotes.Bids)
            {
                sb.Append(string.Format("Bid:{0}:{1};", quote.Price, quote.Volume));
            }
            foreach (var quote in quotes.Asks)
            {
                sb.Append(string.Format("Ask:{0}:{1};", quote.Price, quote.Volume));
            }

            return string.Format("{0};{1};{2}",
                                quotes.ServerTime,
                                quotes.SecurityId.SecurityCode,
                                sb.ToString()
                                );
        }

        private static string Level1ToString(Level1ChangeMessage level)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in level.Changes)
            {
                sb.Append(string.Format("{0}:{1};", kvp.Key, kvp.Value));
            }

            return string.Format("{0};{1};{2}",
                                level.ServerTime,
                                level.SecurityId.SecurityCode,
                                sb.ToString()
                                );
        }

        // writing data to a file
        private static void SaveToFile(string line, string filePath)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
            {
                file.WriteLine(line);
            }
        }

        // converts marketdepth to MemoryStream and writes to the file
        private static void DepthToFile(MarketDepth depth, string filePath)
        {
            using (MemoryStream mem = new MemoryStream(200))
            {
                for (var i = depth.Asks.GetUpperBound(0); i >= 0; i--)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(QuoteToString(depth.Asks[i]));
                    mem.Write(bytes, 0, bytes.Length);
                }

                for (var i = 0; i <= depth.Bids.GetUpperBound(0); i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(QuoteToString(depth.Bids[i]));
                    mem.Write(bytes, 0, bytes.Length);
                }

                using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    mem.WriteTo(file);
                }
            }

        }

        private static void DepthToFile(string quotes, string filePath)
        {
            using (MemoryStream mem = new MemoryStream(quotes.Length + 2))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(quotes);
                mem.Write(bytes, 0, bytes.Length);
                using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    mem.WriteTo(file);
                }
            }
        }

    }
}