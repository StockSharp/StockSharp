namespace StockSharp.Algo.Strategies.Reporting;

using Ecng.Interop;

/// <summary>
/// The report generator for the strategy in the Excel format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExcelReportGenerator"/>.
/// </remarks>
/// <param name="provider"><see cref="IExcelWorkerProvider"/>.</param>
/// <param name="template"><see cref="Template"/></param>
public class ExcelReportGenerator(IExcelWorkerProvider provider, string template = null) : BaseReportGenerator
{
	private readonly IExcelWorkerProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

	/// <summary>
	/// The template file, to be copied into report and filled up with Strategy, Orders and Trades sheets.
	/// </summary>
	public string Template { get; } = template;

	/// <summary>
	/// The number of decimal places. By default, it equals to 2.
	/// </summary>
	public int Decimals { get; set; } = 2;

	/// <inheritdoc />
	public override string Name => "EXCEL";

	/// <inheritdoc />
	public override string Extension => "xlsx";

	/// <inheritdoc />
	public override ValueTask Generate(Strategy strategy, string fileName, CancellationToken cancellationToken)
	{
		var hasTemplate = !Template.IsEmpty();

		if (hasTemplate)
			File.Copy(Template, fileName);

		var mode = hasTemplate ? FileMode.Open : FileMode.Create;
		using var stream = new FileStream(fileName, mode, FileAccess.ReadWrite);
		using var worker = hasTemplate ? _provider.OpenExist(stream) : _provider.CreateNew(stream);

		if (Template.IsEmpty())
		{
			worker.AddSheet();
			worker.RenameSheet(strategy.Name);
		}
		else
		{
			if (!worker.ContainsSheet(strategy.Name))
			{
				worker.AddSheet().RenameSheet(strategy.Name);
			}
		}

		worker
			.SetCell(0, 0, LocalizedStrings.Info)

			.SetCell(0, 1, LocalizedStrings.WorkingTime)
			.SetCell(1, 1, strategy.TotalWorkingTime.Format())

			.SetCell(0, 2, LocalizedStrings.Position + ":")
			.SetCell(1, 2, strategy.Position)

			.SetCell(0, 3, LocalizedStrings.PnL + ":")
			.SetCell(1, 3, strategy.PnL)

			.SetCell(0, 4, LocalizedStrings.Commission + ":")
			.SetCell(1, 4, strategy.Commission)

			.SetCell(0, 5, LocalizedStrings.Slippage + ":")
			.SetCell(1, 5, strategy.Slippage)

			.SetCell(0, 6, LocalizedStrings.Latency + ":")
			.SetCell(1, 6, strategy.Latency.Format());

		var rowIndex = 7;

		foreach (var parameter in strategy.StatisticManager.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var value = parameter.Value;

			if (value is TimeSpan ts)
				value = ts.Format();
			else if (value is DateTimeOffset dto)
				value = dto.Format();
			else if (value is decimal dec)
				value = dec.Round(Decimals);

			worker
				.SetCell(0, rowIndex, parameter.Name)
				.SetCell(1, rowIndex, value);

			rowIndex++;
		}

		rowIndex += 2;
		worker.SetCell(0, rowIndex, LocalizedStrings.Parameters);
		rowIndex++;

		foreach (var strategyParam in strategy.GetParameters())
		{
			cancellationToken.ThrowIfCancellationRequested();

			var value = strategyParam.Value;

			if (value is WorkingTime)
				continue;

			if (value is TimeSpan ts)
				value = ts.Format();
			else if (value is decimal dec)
				value = dec.Round(Decimals);

			worker
				.SetCell(0, rowIndex, strategyParam.GetName())
				.SetCell(1, rowIndex, value);

			rowIndex++;
		}

		var columnShift = 3;

		if (IncludeTrades)
		{
			worker
				.SetCell(columnShift + 0, 0, LocalizedStrings.Trades)

				.SetCell(columnShift + 0, 1, LocalizedStrings.Identifier).SetStyle(columnShift + 0, typeof(long))
				.SetCell(columnShift + 1, 1, LocalizedStrings.Transaction).SetStyle(columnShift + 1, typeof(long))
				.SetCell(columnShift + 2, 1, LocalizedStrings.Time).SetStyle(columnShift + 2, "HH:mm:ss.fff")
				.SetCell(columnShift + 3, 1, LocalizedStrings.Price).SetStyle(columnShift + 3, typeof(decimal))
				.SetCell(columnShift + 4, 1, LocalizedStrings.OrderPrice2).SetStyle(columnShift + 4, typeof(decimal))
				.SetCell(columnShift + 5, 1, LocalizedStrings.Volume).SetStyle(columnShift + 5, typeof(decimal))
				.SetCell(columnShift + 6, 1, LocalizedStrings.Direction)
				.SetCell(columnShift + 7, 1, LocalizedStrings.Order).SetStyle(columnShift + 7, typeof(long))
				.SetCell(columnShift + 8, 1, LocalizedStrings.Slippage).SetStyle(columnShift + 8, typeof(decimal))
				.SetCell(columnShift + 9, 1, LocalizedStrings.Comment)
				.SetCell(columnShift + 10, 1, LocalizedStrings.PnL).SetStyle(columnShift + 10, typeof(decimal))
				.SetCell(columnShift + 14, 1, LocalizedStrings.Position).SetStyle(columnShift + 14, typeof(decimal));

			//worker
			//	.SetConditionalFormatting(columnShift + 10, ComparisonOperator.Less, "0", null, Colors.Red)
			//	.SetConditionalFormatting(columnShift + 11, ComparisonOperator.Less, "0", null, Colors.Red)
			//	.SetConditionalFormatting(columnShift + 12, ComparisonOperator.Less, "0", null, Colors.Red)
			//	.SetConditionalFormatting(columnShift + 13, ComparisonOperator.Less, "0", null, Colors.Red);

			var totalPnL = 0m;
			var position = 0m;

			rowIndex = 2;
			foreach (var trade in strategy.MyTrades.ToArray())
			{
				cancellationToken.ThrowIfCancellationRequested();

				var pnl = trade.PnL ?? 0;

				totalPnL += pnl;
				position += trade.Position ?? 0;

				worker
					.SetCell(columnShift + 0, rowIndex, trade.Trade.Id)
					.SetCell(columnShift + 1, rowIndex, trade.Order.TransactionId)
					.SetCell(columnShift + 2, rowIndex, trade.Trade.ServerTime.Format())
					.SetCell(columnShift + 3, rowIndex, trade.Trade.Price)
					.SetCell(columnShift + 4, rowIndex, trade.Order.Price)
					.SetCell(columnShift + 5, rowIndex, trade.Trade.Volume)
					.SetCell(columnShift + 6, rowIndex, trade.Order.Side.GetDisplayName())
					.SetCell(columnShift + 7, rowIndex, trade.Order.Id)
					.SetCell(columnShift + 8, rowIndex, trade.Slippage)
					.SetCell(columnShift + 9, rowIndex, trade.Order.Comment)
					.SetCell(columnShift + 10, rowIndex, pnl)
					.SetCell(columnShift + 12, rowIndex, totalPnL.Round(Decimals))
					.SetCell(columnShift + 14, rowIndex, position);

				rowIndex++;
			}
		}

		if (IncludeOrders)
		{
			columnShift += 17;

			worker
				.SetCell(columnShift + 0, 0, LocalizedStrings.Orders)

				.SetCell(columnShift + 0, 1, LocalizedStrings.Identifier).SetStyle(columnShift + 0, typeof(long))
				.SetCell(columnShift + 1, 1, LocalizedStrings.Transaction).SetStyle(columnShift + 1, typeof(long))
				.SetCell(columnShift + 2, 1, LocalizedStrings.Direction)
				.SetCell(columnShift + 3, 1, LocalizedStrings.RegTime).SetStyle(columnShift + 3, "HH:mm:ss.fff")
				.SetCell(columnShift + 4, 1, LocalizedStrings.ChangeTime).SetStyle(columnShift + 4, "HH:mm:ss.fff")
				.SetCell(columnShift + 5, 1, LocalizedStrings.Duration)
				.SetCell(columnShift + 6, 1, LocalizedStrings.Price).SetStyle(columnShift + 6, typeof(decimal))
				.SetCell(columnShift + 7, 1, LocalizedStrings.Status)
				.SetCell(columnShift + 8, 1, LocalizedStrings.State)
				.SetCell(columnShift + 9, 1, LocalizedStrings.Balance).SetStyle(columnShift + 9, typeof(decimal))
				.SetCell(columnShift + 10, 1, LocalizedStrings.Volume).SetStyle(columnShift + 10, typeof(decimal))
				.SetCell(columnShift + 11, 1, LocalizedStrings.Type)
				.SetCell(columnShift + 12, 1, LocalizedStrings.LatencyReg)
				.SetCell(columnShift + 13, 1, LocalizedStrings.LatencyCancel)
				.SetCell(columnShift + 14, 1, LocalizedStrings.EditionLatency)
				.SetCell(columnShift + 15, 1, LocalizedStrings.Comment);

			//worker
			//	.SetConditionalFormatting(columnShift + 8, ComparisonOperator.Equal, "\"{0}\"".Put(LocalizedStrings.Cancelled), null, Colors.Green)
			//	.SetConditionalFormatting(columnShift + 8, ComparisonOperator.Equal, "\"{0}\"".Put(LocalizedStrings.Active), null, Colors.Red);

			rowIndex = 2;
			foreach (var order in strategy.Orders.ToArray())
			{
				cancellationToken.ThrowIfCancellationRequested();

				worker
					.SetCell(columnShift + 0, rowIndex, order.Id)
					.SetCell(columnShift + 1, rowIndex, order.TransactionId)
					.SetCell(columnShift + 2, rowIndex, order.Side.GetDisplayName())
					.SetCell(columnShift + 3, rowIndex, order.Time.Format())
					.SetCell(columnShift + 4, rowIndex, order.ServerTime.Format())
					.SetCell(columnShift + 5, rowIndex, (order.ServerTime - order.Time).Format())
					.SetCell(columnShift + 6, rowIndex, order.Price)
					.SetCell(columnShift + 7, rowIndex, order.State.GetDisplayName())
					.SetCell(columnShift + 8, rowIndex, order.IsMatched() ? LocalizedStrings.Done : (order.IsCanceled() ? LocalizedStrings.Cancelled : LocalizedStrings.Active))
					.SetCell(columnShift + 9, rowIndex, order.Balance)
					.SetCell(columnShift + 10, rowIndex, order.Volume)
					.SetCell(columnShift + 11, rowIndex, order.Type.GetDisplayName())
					.SetCell(columnShift + 12, rowIndex, order.LatencyRegistration.Format())
					.SetCell(columnShift + 13, rowIndex, order.LatencyCancellation.Format())
					.SetCell(columnShift + 14, rowIndex, order.LatencyEdition.Format())
					.SetCell(columnShift + 15, rowIndex, order.Comment);

					rowIndex++;
			}
		}

		return default;
	}
}
