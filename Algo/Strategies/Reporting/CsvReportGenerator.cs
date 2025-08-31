namespace StockSharp.Algo.Strategies.Reporting;

using System.Globalization;

using StockSharp.Algo.Strategies;

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

		WriteValues(LocalizedStrings.Strategy, LocalizedStrings.WorkingTime, LocalizedStrings.Position, LocalizedStrings.PnL, LocalizedStrings.Commission, LocalizedStrings.Slippage, LocalizedStrings.Latency);
		WriteValues(strategy.Name, strategy.TotalWorkingTime, strategy.Position, strategy.PnL, strategy.Commission, strategy.Slippage, strategy.Latency);

		var parameters = strategy.GetParameters();
		WriteValues(LocalizedStrings.Parameters);
		WriteValues([.. parameters.Select(p => (object)p.GetName())]);
		WriteValues([.. parameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : p.Value)]);

		var statParameters = strategy.StatisticManager.Parameters;
		WriteValues(LocalizedStrings.Statistics);
		WriteValues([.. statParameters.Select(p => (object)p.Name)]);
		WriteValues([.. statParameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : (p.Value is DateTimeOffset dto ? dto.Format() : p.Value))]);

		if (IncludeOrders)
		{
			WriteValues(LocalizedStrings.Orders);
			WriteValues(LocalizedStrings.Identifier, LocalizedStrings.Transaction, LocalizedStrings.Direction, LocalizedStrings.Time, LocalizedStrings.Price,
				LocalizedStrings.Status, LocalizedStrings.State, LocalizedStrings.Balance,
				LocalizedStrings.Volume, LocalizedStrings.Type, LocalizedStrings.LatencyReg, LocalizedStrings.LatencyCancel, LocalizedStrings.EditionLatency);

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
					trade.Order.Side.GetDisplayName(), trade.Order.Id, trade.PnL, trade.Slippage);
			}
		}

		return default;
	}
}
