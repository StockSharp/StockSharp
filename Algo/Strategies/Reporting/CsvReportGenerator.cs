namespace StockSharp.Algo.Strategies.Reporting;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

		WriteValues(LocalizedStrings.Strategy, LocalizedStrings.Security, LocalizedStrings.Portfolio, LocalizedStrings.WorkingTime, LocalizedStrings.Position, LocalizedStrings.PnL, LocalizedStrings.Commission, LocalizedStrings.Slippage, LocalizedStrings.Latency);
		WriteValues(
			strategy.Name, strategy.Security?.Id, strategy.Portfolio?.Name,
			strategy.TotalWorkingTime, strategy.Position, strategy.PnL, strategy.Commission, strategy.Slippage, strategy.Latency);

		var parameters = strategy.Parameters.CachedValues;
		WriteValues(LocalizedStrings.Parameters);
		WriteValues(parameters.Select(p => (object)p.Name).ToArray());
		WriteValues(parameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : p.Value).ToArray());

		var statParameters = strategy.StatisticManager.Parameters;
		WriteValues(LocalizedStrings.Statistics);
		WriteValues(statParameters.Select(p => (object)p.Name).ToArray());
		WriteValues(statParameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : (p.Value is DateTimeOffset dto ? dto.Format() : p.Value)).ToArray());

		if (IncludeOrders)
		{
			WriteValues(LocalizedStrings.Orders);
			WriteValues(LocalizedStrings.Identifier, LocalizedStrings.Transaction, LocalizedStrings.Direction, LocalizedStrings.Time, LocalizedStrings.Price,
				LocalizedStrings.Status, LocalizedStrings.State, LocalizedStrings.Balance,
				LocalizedStrings.Volume, LocalizedStrings.Type, LocalizedStrings.LatencyReg, LocalizedStrings.LatencyCancel);

			foreach (var order in strategy.Orders)
			{
				WriteValues(order.Id, order.TransactionId, order.Side.GetDisplayName(), order.Time, order.Price,
					order.State.GetDisplayName(), order.IsMatched() ? LocalizedStrings.Done : (order.IsCanceled() ? LocalizedStrings.Cancelled : string.Empty), order.Balance,
						order.Volume, order.Type.GetDisplayName(), order.LatencyRegistration.Format(), order.LatencyCancellation.Format(), order.LatencyEdition.Format());
			}
		}

		if (IncludeTrades)
		{
			WriteValues(LocalizedStrings.Trades);
			WriteValues(LocalizedStrings.Identifier, LocalizedStrings.Transaction, LocalizedStrings.Time, LocalizedStrings.Price, LocalizedStrings.Volume,
				LocalizedStrings.Direction, LocalizedStrings.OrderId, LocalizedStrings.PnL, LocalizedStrings.Slippage);

			foreach (var trade in strategy.MyTrades)
			{
				WriteValues(trade.Trade.Id, trade.Order.TransactionId, trade.Trade.ServerTime.Format(), trade.Trade.Price, trade.Trade.Volume,
					trade.Order.Side.GetDisplayName(), trade.Order.Id, strategy.PnLManager.ProcessMessage(trade.ToMessage())?.PnL, trade.Slippage);
			}
		}

		return default;
	}
}