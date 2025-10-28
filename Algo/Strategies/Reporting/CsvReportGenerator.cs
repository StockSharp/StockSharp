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
	public override async ValueTask Generate(Strategy strategy, Stream stream, CancellationToken cancellationToken)
	{
		using var writer = new StreamWriter(stream, Encoding, leaveOpen: true);

		async Task WriteValuesAsync(params object[] values)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			for (var i = 0; i < values.Length; i++)
			{
				var value = values[i];

				if (value is DateTimeOffset dto)
					value = dto.Format();
				else if (value is TimeSpan ts)
					value = ts.Format();

				var str = value?.ToString() ?? string.Empty;

				await writer.WriteAsync(str.AsMemory(), cancellationToken);

				if (i < (values.Length - 1))
					await writer.WriteAsync(_separator.AsMemory(), cancellationToken);
			}

			await writer.WriteLineAsync();
		}

		await WriteValuesAsync(LocalizedStrings.Strategy, LocalizedStrings.WorkingTime, LocalizedStrings.Position, LocalizedStrings.PnL, LocalizedStrings.Commission, LocalizedStrings.Slippage, LocalizedStrings.Latency);
		await WriteValuesAsync(strategy.Name, strategy.TotalWorkingTime, strategy.Position, strategy.PnL, strategy.Commission, strategy.Slippage, strategy.Latency);

		var parameters = strategy.GetParameters();
		await WriteValuesAsync(LocalizedStrings.Parameters);
		await WriteValuesAsync([.. parameters.Select(p => (object)p.GetName())]);
		await WriteValuesAsync([.. parameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : p.Value)]);

		var statParameters = strategy.StatisticManager.Parameters;
		await WriteValuesAsync(LocalizedStrings.Statistics);
		await WriteValuesAsync([.. statParameters.Select(p => (object)p.Name)]);
		await WriteValuesAsync([.. statParameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : (p.Value is DateTimeOffset dto ? dto.Format() : p.Value))]);

		if (IncludeOrders)
		{
			await WriteValuesAsync(LocalizedStrings.Orders);
			await WriteValuesAsync(LocalizedStrings.Identifier, LocalizedStrings.Transaction, LocalizedStrings.Direction, LocalizedStrings.Time, LocalizedStrings.Price,
				LocalizedStrings.Status, LocalizedStrings.State, LocalizedStrings.Balance,
				LocalizedStrings.Volume, LocalizedStrings.Type, LocalizedStrings.LatencyReg, LocalizedStrings.LatencyCancel, LocalizedStrings.EditionLatency);

			foreach (var order in strategy.Orders)
			{
				await WriteValuesAsync(order.Id, order.TransactionId, order.Side.GetDisplayName(), order.Time, order.Price,
					order.State.GetDisplayName(), order.IsMatched() ? LocalizedStrings.Done : (order.IsCanceled() ? LocalizedStrings.Cancelled : LocalizedStrings.Active), order.Balance,
						order.Volume, order.Type.GetDisplayName(), order.LatencyRegistration.Format(), order.LatencyCancellation.Format(), order.LatencyEdition.Format());
			}
		}

		if (IncludeTrades)
		{
			await WriteValuesAsync(LocalizedStrings.Trades);
			await WriteValuesAsync(LocalizedStrings.Identifier, LocalizedStrings.Transaction, LocalizedStrings.Time, LocalizedStrings.Price, LocalizedStrings.Volume,
				LocalizedStrings.Direction, LocalizedStrings.OrderId, LocalizedStrings.PnL, LocalizedStrings.Slippage);

			foreach (var trade in strategy.MyTrades)
			{
				await WriteValuesAsync(trade.Trade.Id, trade.Order.TransactionId, trade.Trade.ServerTime.Format(), trade.Trade.Price, trade.Trade.Volume,
					trade.Order.Side.GetDisplayName(), trade.Order.Id, trade.PnL, trade.Slippage);
			}
		}
	}
}
