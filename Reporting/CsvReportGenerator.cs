namespace StockSharp.Reporting;

using System.Globalization;

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
	protected override async ValueTask OnGenerate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		using var writer = new StreamWriter(stream, Encoding, leaveOpen: true);

		async Task WriteValuesAsync(params object[] values)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			for (var i = 0; i < values.Length; i++)
			{
				var value = values[i];

				if (value is DateTime dt)
					value = dt.Format();
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
		await WriteValuesAsync(source.Name, source.TotalWorkingTime, source.Position, source.PnL, source.Commission, source.Slippage, source.Latency);

		var parameters = source.Parameters.ToArray();
		await WriteValuesAsync(LocalizedStrings.Parameters);
		await WriteValuesAsync([.. parameters.Select(p => (object)p.Name)]);
		await WriteValuesAsync([.. parameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : p.Value)]);

		var statParameters = source.StatisticParameters.ToArray();
		await WriteValuesAsync(LocalizedStrings.Statistics);
		await WriteValuesAsync([.. statParameters.Select(p => (object)p.Name)]);
		await WriteValuesAsync([.. statParameters.Select(p => p.Value is TimeSpan ts ? ts.Format() : (p.Value is DateTime dt ? dt.Format() : p.Value))]);

		if (IncludeOrders)
		{
			await WriteValuesAsync(LocalizedStrings.Orders);
			await WriteValuesAsync(LocalizedStrings.Identifier, LocalizedStrings.Transaction, LocalizedStrings.SecurityKey, LocalizedStrings.Direction, LocalizedStrings.Time, LocalizedStrings.Price,
				LocalizedStrings.Status, LocalizedStrings.Balance, LocalizedStrings.Volume, LocalizedStrings.Type);

			foreach (var order in source.Orders)
			{
				await WriteValuesAsync(order.Id, order.TransactionId, order.SecurityId.ToStringId(), order.Side.GetDisplayName(), order.Time, order.Price,
					order.State.GetDisplayName(), order.Balance, order.Volume, order.Type.GetDisplayName());
			}
		}

		if (IncludeTrades)
		{
			await WriteValuesAsync(LocalizedStrings.Trades);
			await WriteValuesAsync(LocalizedStrings.Identifier, LocalizedStrings.Transaction, LocalizedStrings.SecurityKey, LocalizedStrings.Time, LocalizedStrings.Price, LocalizedStrings.Volume,
				LocalizedStrings.Direction, LocalizedStrings.OrderId, LocalizedStrings.PnL, LocalizedStrings.Slippage);

			foreach (var trade in source.OwnTrades)
			{
				await WriteValuesAsync(trade.TradeId, trade.OrderTransactionId, trade.SecurityId.ToStringId(), trade.Time.Format(), trade.TradePrice, trade.Volume,
					trade.Side.GetDisplayName(), trade.OrderId, trade.PnL, trade.Slippage);
			}
		}

		if (IncludePositions)
		{
			await WriteValuesAsync(LocalizedStrings.Positions);
			await WriteValuesAsync(LocalizedStrings.Security, LocalizedStrings.Portfolio, LocalizedStrings.EntryTime, LocalizedStrings.OpenPrice, LocalizedStrings.ExitTime, LocalizedStrings.ClosingPrice, LocalizedStrings.MaxVolume);

			foreach (var pos in source.Positions)
			{
				await WriteValuesAsync(pos.SecurityId.ToStringId(), pos.PortfolioName, pos.OpenTime.Format(), pos.OpenPrice, pos.CloseTime.Format(), pos.ClosePrice, pos.MaxPosition);
			}
		}
	}
}
