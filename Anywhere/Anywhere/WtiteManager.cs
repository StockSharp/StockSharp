using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using StockSharp.BusinessEntities;

namespace Anywhere
{
    /// <summary>
    ///  The write functions to text files
    /// </summary>

    public class WtiteManager
    {

        // returns a string of trade
        public string TradeToString(Trade trade)
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
        public string MyTradeToString(MyTrade trade)
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
        public string OrderToString(Order order)
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
        public string PositionToString(Position position)
        {
            return string.Format("{0};{1};{2};{3}",
                                position.Security.Id,
                                position.Portfolio.Name,
                                position.CurrentValue,
                                position.AveragePrice);
        }

        // returns a string of level1
        public string Level1ToString(Security security)
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
        public string QuoteToString(Quote quote)
        {
            return string.Format("{0};{1};{2}{3}",
                                quote.OrderDirection.ToString(),
                                quote.Price,
                                quote.Volume,
                                Environment.NewLine);
        }

        // writing data to a file
        public void SaveToFile(string line, string filePath)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
            {
                file.WriteLine(line);
            }
        }

        // converts marketdepth to MemoryStream and writes to the file
        public void DepthToFile(MarketDepth depth, string filePath)
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

    }
}

