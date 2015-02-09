namespace StockSharp.Algo.Strategies.Reporting
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Strategies;

	using StockSharp.Localization;

	///<summary>
	/// Генератор отчета по эквити стратегии в формате csv.
	///</summary>
	public class CsvStrategyReport : StrategyReport
	{
		private readonly string _separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		/// <summary>
		/// Создать <see cref="CsvStrategyReport"/>.
		/// </summary>
		/// <param name="strategy">Стратегия, для которой необходимо сгенерировать отчет.</param>
		/// <param name="fileName">Название файла, в котором сгенерируется отчет в формате csv.</param>
		public CsvStrategyReport(Strategy strategy, string fileName)
			: this(new[] { strategy }, fileName)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");
		}

		/// <summary>
		/// Создать <see cref="CsvStrategyReport"/>.
		/// </summary>
		/// <param name="strategies">Стратегии, для которых необходимо сгенерировать отчет.</param>
		/// <param name="fileName">Название файла, в котором сгенерируется отчет в формате csv.</param>
		public CsvStrategyReport(IEnumerable<Strategy> strategies, string fileName)
			: base(strategies, fileName)
		{
		}

		/// <summary>
		/// Сгенерировать отчет.
		/// </summary>
		public override void Generate()
		{
			using (var writer = new StreamWriter(FileName))
			{
				foreach (var strategy in Strategies)
				{
					WriteValues(writer, LocalizedStrings.Strategy, LocalizedStrings.Security, LocalizedStrings.Portfolio, LocalizedStrings.Str1321, LocalizedStrings.Str862, "P&L", LocalizedStrings.Str159, LocalizedStrings.Str163, LocalizedStrings.Str161);
					WriteValues(writer,
						strategy.Name, strategy.Security != null ? strategy.Security.Id : string.Empty, strategy.Portfolio != null ? strategy.Portfolio.Name : string.Empty,
						strategy.TotalWorkingTime, strategy.Position, strategy.PnL, strategy.Commission, strategy.Slippage, strategy.Latency);

					var parameters = strategy.Parameters.SyncGet(c => c.ToArray());
					WriteValues(writer, LocalizedStrings.Str1322);
					WriteValues(writer, parameters.Select(p => (object)p.Name).ToArray());
					WriteValues(writer, parameters.Select(p => p.Value is TimeSpan ? Format((TimeSpan)p.Value) : p.Value).ToArray());

					var statParameters = strategy.StatisticManager.Parameters.SyncGet(c => c.ToArray());
					WriteValues(writer, LocalizedStrings.Str436);
					WriteValues(writer, statParameters.Select(p => (object)p.Name).ToArray());
					WriteValues(writer, statParameters.Select(p => p.Value is TimeSpan ? Format((TimeSpan)p.Value) : p.Value).ToArray());

					WriteValues(writer, LocalizedStrings.Orders);
					WriteValues(writer, LocalizedStrings.Str1190, LocalizedStrings.Str230, LocalizedStrings.Str128, LocalizedStrings.Str219, LocalizedStrings.Price,
						LocalizedStrings.Str1323, LocalizedStrings.Str1324, LocalizedStrings.State, LocalizedStrings.Str1325,
						LocalizedStrings.Volume, LocalizedStrings.Type, LocalizedStrings.Str1326, LocalizedStrings.Str1327);

					foreach (var order in strategy.Orders)
					{
						WriteValues(writer, order.Id, order.TransactionId, Format(order.Direction), order.Time, order.Price, order.GetAveragePrice(),
							Format(order.State), order.IsMatched() ? LocalizedStrings.Str1328 : (order.IsCanceled() ? LocalizedStrings.Str1329 : string.Empty), order.Balance,
								order.Volume, Format(order.Type), Format(order.LatencyRegistration), Format(order.LatencyCancellation));
					}

					WriteValues(writer, LocalizedStrings.Str985);
					WriteValues(writer, LocalizedStrings.Str1192, LocalizedStrings.Str230, LocalizedStrings.Str219, LocalizedStrings.Price, LocalizedStrings.Volume,
						LocalizedStrings.Str128, LocalizedStrings.Str1190, LocalizedStrings.Str1330, LocalizedStrings.Str163);

					foreach (var trade in strategy.MyTrades)
					{
						WriteValues(writer, trade.Trade.Id, trade.Order.TransactionId, Format(trade.Trade.Time), trade.Trade.Price, trade.Trade.Volume,
							Format(trade.Order.Direction), trade.Order.Id, strategy.PnLManager.ProcessMyTrade(trade.ToMessage()).PnL, trade.Slippage);
					}
				}
			}
		}

		private void WriteValues(TextWriter writer, params object[] values)
		{
			for (var i = 0; i < values.Length; i++)
			{
				var value = values[i];

				if (value is DateTimeOffset)
					value = Format((DateTimeOffset)value);
				else if (value is TimeSpan)
					value = Format((TimeSpan)value);

				writer.Write(value);

				if (i < (values.Length - 1))
					writer.Write(_separator);
			}

			writer.WriteLine();
		}

		//private void Save()
		//{



		//    using (var sw = new StreamWriter(FileName))
		//    {
		//        sw.WriteLine("; All Trades; Long Trades; Short Trades");

		//        sw.WriteLine("Net Profit; " + _strategyStatistics.NetProfit + "; " + _strategyStatistics.NetLongProfit +
		//                     "; " + _strategyStatistics.NetShortProfit);
		//        sw.WriteLine("Number of Trades; " + _strategyStatistics.NumberOfTrades + "; " +
		//                     _strategyStatistics.NumberOfLongTrades + "; " +
		//                     _strategyStatistics.NumberOfShortTrades);
		//        sw.WriteLine("Average Profit; " + _strategyStatistics.AverageProfit + "; " +
		//                     _strategyStatistics.AverageLongProfit + "; " + _strategyStatistics.AverageShortProfit);
		//        sw.WriteLine("Average Profit %; " + _strategyStatistics.AverageNetProfitPrc + "%; " +
		//                     _strategyStatistics.AverageLongNetProfitPrc + "%; " +
		//                     _strategyStatistics.AverageShortNetProfitPrc + "%");
		//        sw.WriteLine(";;;");
		//        sw.WriteLine("Winning Trades; " + _strategyStatistics.WinningTrades + "; " +
		//                     _strategyStatistics.WinningLongTrades + "; " + _strategyStatistics.LosingLongTrades);
		//        sw.WriteLine("Win Rate; " + _strategyStatistics.WinRate + "%; " + _strategyStatistics.WinLongRate + "%; " +
		//                     _strategyStatistics.WinShortRate + "%");
		//        sw.WriteLine("Gross Profit; " + _strategyStatistics.GrossProfit + "; " + _strategyStatistics.GrossLongProfit +
		//                     "; " + _strategyStatistics.GrossShortProfit);
		//        sw.WriteLine("Average Profit; " + _strategyStatistics.AverageWinProfit + "; " +
		//                     _strategyStatistics.AverageWinLongProfit + "; " +
		//                     _strategyStatistics.AverageWinShortProfit);
		//        sw.WriteLine("Average Profit %; " + _strategyStatistics.AverageWinProfitPrc + "%; " +
		//                     _strategyStatistics.AverageWinLongProfitPrc + "%; " +
		//                     _strategyStatistics.AverageWinShortProfitPrc + "%");
		//        sw.WriteLine(";;;");
		//        sw.WriteLine("Losing Trades; " + _strategyStatistics.LosingTrades + "; " +
		//                     _strategyStatistics.LosingLongTrades + "; " + _strategyStatistics.LosingShortTrades);
		//        sw.WriteLine("Loss Rate; " + _strategyStatistics.LossRate + "%; " + _strategyStatistics.LossLongRate + "%; " +
		//                     _strategyStatistics.LossShortRate + "%");
		//        sw.WriteLine("Gross Loss; " + _strategyStatistics.GrossLoss + "; " + _strategyStatistics.GrossLongLoss +
		//                     "; " + _strategyStatistics.GrossShortLoss);
		//        sw.WriteLine("Average Loss; " + _strategyStatistics.AverageLoss + "; " + _strategyStatistics.AverageLongLoss +
		//                     "; " + _strategyStatistics.AverageShortLoss);
		//        sw.WriteLine("Average Loss %; " + _strategyStatistics.AverageLossPrc + "%; " +
		//                     _strategyStatistics.AverageLongLossPrc + "%; " +
		//                     _strategyStatistics.AverageShortLossPrc + "%");
		//        sw.WriteLine(";;;");
		//        sw.WriteLine("Maximum Drawdown; " + _strategyStatistics.MaxDrawdown + "; " +
		//                     _strategyStatistics.MaxLongDrawdown + "; " + _strategyStatistics.MaxShortDrawdown);
		//        sw.WriteLine("Profit Factor; " + _strategyStatistics.ProfitFactor + "; " +
		//                     _strategyStatistics.ProfitFactorLong + "; " + _strategyStatistics.ProfitFactorShort);
		//        sw.WriteLine("Recovery Factor; " + _strategyStatistics.RecoveryFactor + "; " +
		//                     _strategyStatistics.RecoveryFactorLong + "; " +
		//                     _strategyStatistics.RecoveryFactorShort);
		//        sw.WriteLine("Payoff Ratio; " + _strategyStatistics.PayoffRatio + "; " + _strategyStatistics.PayoffRatioLong +
		//                     "; " + _strategyStatistics.PayoffRatioShort);
		//        sw.WriteLine("Smoothness Factor; " + _strategyStatistics.SmoothnessFactor);

		//        sw.WriteLine(";;;");
		//        sw.WriteLine(";;;");
		//        sw.WriteLine(";;;");

		//        var index = 0;
		//        sw.WriteLine(
		//            "Position; Quantity; Entry Date; Entry Price; Exit Date; Exit Price; Equity; Long; Short; Median;");
		//        foreach (var kvp in _strategyStatistics.TradePairs)
		//        {
		//            sw.WriteLine(
		//                kvp.Value.FirstTrade.OrderDirection + ";" +
		//                kvp.Value.FirstTrade.Volume + ";" +
		//                kvp.Value.FirstTrade.Time + ";" +
		//                kvp.Value.FirstTrade.Price + ";" +
		//                kvp.Value.SecondTrade.Time + ";" +
		//                kvp.Value.SecondTrade.Price + ";" +
		//                _strategyStatistics.Equity[index] + ";" +
		//                _strategyStatistics.LongEquity[index] + ";" +
		//                _strategyStatistics.ShortEquity[index] + ";" +
		//                _strategyStatistics.EquityMedian[index]);
		//            index++;
		//        }
		//    }
		//}
	}
}