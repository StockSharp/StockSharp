namespace StockSharp.Algo.Strategies.Reporting;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Collections;
using Ecng.ComponentModel;

using StockSharp.Algo.Strategies;
using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// The generator of report on equity in the csv format.
/// </summary>
public class CsvReportGenerator : BaseReportGenerator
{
	private readonly string _separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

	/// <inheritdoc />
	public override string Name => "CSV";

	/// <inheritdoc />
	public override string Extension => "csv";

	/// <inheritdoc />
	public override ValueTask Generate(Strategy strategy, string fileName, CancellationToken cancellationToken)
	{
		using var writer = new StreamWriter(fileName);

		void WriteValues(params object[] values)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			cancellationToken.ThrowIfCancellationRequested();

			for (var i = 0; i < values.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var value = values[i];

				if (value is DateTimeOffset dto)
					value = dto.Format();
				else if (value is TimeSpan ts)
					value = ts.Format();

				writer.Write(value);

				if (i < (values.Length - 1))
					writer.Write(_separator);
			}

			writer.WriteLine();
		}

		WriteValues(LocalizedStrings.Strategy, LocalizedStrings.Security, LocalizedStrings.Portfolio, LocalizedStrings.Str1321, LocalizedStrings.Str862, LocalizedStrings.PnL, LocalizedStrings.Commission, LocalizedStrings.Str163, LocalizedStrings.Str161);
		WriteValues(
			strategy.Name, strategy.Security != null ? strategy.Security.Id : string.Empty, strategy.Portfolio != null ? strategy.Portfolio.Name : string.Empty,
			strategy.TotalWorkingTime, strategy.Position, strategy.PnL, strategy.Commission, strategy.Slippage, strategy.Latency);

		var parameters = strategy.Parameters.CachedValues;
		WriteValues(LocalizedStrings.Str1322);
		WriteValues(parameters.Select(p => (object)p.Name).ToArray());
		WriteValues(parameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : p.Value).ToArray());

		var statParameters = strategy.StatisticManager.Parameters;
		WriteValues(LocalizedStrings.Statistics);
		WriteValues(statParameters.Select(p => (object)p.Name).ToArray());
		WriteValues(statParameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : (p.Value is DateTimeOffset dto ? dto.Format() : p.Value)).ToArray());

		WriteValues(LocalizedStrings.Orders);
		WriteValues(LocalizedStrings.Str1190, LocalizedStrings.Transaction, LocalizedStrings.Str128, LocalizedStrings.Time, LocalizedStrings.Price,
			LocalizedStrings.Str1324, LocalizedStrings.State, LocalizedStrings.Str1325,
			LocalizedStrings.Volume, LocalizedStrings.Type, LocalizedStrings.Str1326, LocalizedStrings.Str1327);

		foreach (var order in strategy.Orders)
		{
			WriteValues(order.Id, order.TransactionId, order.Side.GetDisplayName(), order.Time, order.Price,
				order.State.GetDisplayName(), order.IsMatched() ? LocalizedStrings.Str1328 : (order.IsCanceled() ? LocalizedStrings.Str1329 : string.Empty), order.Balance,
					order.Volume, order.Type.GetDisplayName(), order.LatencyRegistration.Format(), order.LatencyCancellation.Format(), order.LatencyEdition.Format());
		}

		WriteValues(LocalizedStrings.Trades);
		WriteValues(LocalizedStrings.Str1192, LocalizedStrings.Transaction, LocalizedStrings.Time, LocalizedStrings.Price, LocalizedStrings.Volume,
			LocalizedStrings.Str128, LocalizedStrings.Str1190, LocalizedStrings.Str1330, LocalizedStrings.Str163);

		foreach (var trade in strategy.MyTrades)
		{
			WriteValues(trade.Trade.Id, trade.Order.TransactionId, trade.Trade.ServerTime.Format(), trade.Trade.Price, trade.Trade.Volume,
				trade.Order.Side.GetDisplayName(), trade.Order.Id, strategy.PnLManager.ProcessMessage(trade.ToMessage())?.PnL, trade.Slippage);
		}

		return default;
	}
}