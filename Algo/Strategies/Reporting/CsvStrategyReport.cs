#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Reporting.Algo
File: CsvStrategyReport.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

	/// <summary>
	/// The generator of report on equity in the csv format.
	/// </summary>
	public class CsvStrategyReport : StrategyReport
	{
		private readonly string _separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvStrategyReport"/>.
		/// </summary>
		/// <param name="strategy">The strategy, requiring the report generation.</param>
		/// <param name="fileName">The name of the file for the report generation in the csv format.</param>
		public CsvStrategyReport(Strategy strategy, string fileName)
			: this(new[] { strategy }, fileName)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvStrategyReport"/>.
		/// </summary>
		/// <param name="strategies">Strategies, requiring the report generation.</param>
		/// <param name="fileName">The name of the file for the report generation in the csv format.</param>
		public CsvStrategyReport(IEnumerable<Strategy> strategies, string fileName)
			: base(strategies, fileName)
		{
		}

		/// <inheritdoc />
		public override void Generate()
		{
			using (var writer = new StreamWriter(FileName))
			{
				foreach (var strategy in Strategies)
				{
					WriteValues(writer, LocalizedStrings.Strategy, LocalizedStrings.Security, LocalizedStrings.Portfolio, LocalizedStrings.Str1321, LocalizedStrings.Str862, LocalizedStrings.PnL, LocalizedStrings.Str159, LocalizedStrings.Str163, LocalizedStrings.Str161);
					WriteValues(writer,
						strategy.Name, strategy.Security != null ? strategy.Security.Id : string.Empty, strategy.Portfolio != null ? strategy.Portfolio.Name : string.Empty,
						strategy.TotalWorkingTime, strategy.Position, strategy.PnL, strategy.Commission, strategy.Slippage, strategy.Latency);

					var parameters = strategy.Parameters.CachedValues;
					WriteValues(writer, LocalizedStrings.Str1322);
					WriteValues(writer, parameters.Select(p => (object)p.Name).ToArray());
					WriteValues(writer, parameters.Select(p => p.Value is TimeSpan ts ? Format(ts) : p.Value).ToArray());

					var statParameters = strategy.StatisticManager.Parameters.SyncGet(c => c.ToArray());
					WriteValues(writer, LocalizedStrings.Str436);
					WriteValues(writer, statParameters.Select(p => (object)p.Name).ToArray());
					WriteValues(writer, statParameters.Select(p => p.Value is TimeSpan ts ? Format(ts) : p.Value).ToArray());

					WriteValues(writer, LocalizedStrings.Orders);
					WriteValues(writer, LocalizedStrings.Str1190, LocalizedStrings.Transaction, LocalizedStrings.Str128, LocalizedStrings.Time, LocalizedStrings.Price,
						LocalizedStrings.Str1324, LocalizedStrings.State, LocalizedStrings.Str1325,
						LocalizedStrings.Volume, LocalizedStrings.Type, LocalizedStrings.Str1326, LocalizedStrings.Str1327);

					foreach (var order in strategy.Orders)
					{
						WriteValues(writer, order.Id, order.TransactionId, Format(order.Direction), order.Time, order.Price,
							Format(order.State), order.IsMatched() ? LocalizedStrings.Str1328 : (order.IsCanceled() ? LocalizedStrings.Str1329 : string.Empty), order.Balance,
								order.Volume, Format(order.Type), Format(order.LatencyRegistration), Format(order.LatencyCancellation), Format(order.LatencyEdition));
					}

					WriteValues(writer, LocalizedStrings.Str985);
					WriteValues(writer, LocalizedStrings.Str1192, LocalizedStrings.Transaction, LocalizedStrings.Time, LocalizedStrings.Price, LocalizedStrings.Volume,
						LocalizedStrings.Str128, LocalizedStrings.Str1190, LocalizedStrings.Str1330, LocalizedStrings.Str163);

					foreach (var trade in strategy.MyTrades)
					{
						WriteValues(writer, trade.Trade.Id, trade.Order.TransactionId, Format(trade.Trade.Time), trade.Trade.Price, trade.Trade.Volume,
							Format(trade.Order.Direction), trade.Order.Id, strategy.PnLManager.ProcessMessage(trade.ToMessage())?.PnL, trade.Slippage);
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

	}
}