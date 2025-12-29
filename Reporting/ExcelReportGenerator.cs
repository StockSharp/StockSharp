namespace StockSharp.Reporting;

using Ecng.Interop;

/// <summary>
/// The report generator for the strategy in the Excel format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExcelReportGenerator"/>.
/// </remarks>
/// <param name="provider"><see cref="IExcelWorkerProvider"/>.</param>
/// <param name="templateStream">The template stream to be copied into report and filled up with Strategy, Orders and Trades sheets.</param>
public class ExcelReportGenerator(IExcelWorkerProvider provider, Stream templateStream = null) : BaseReportGenerator
{
	private readonly IExcelWorkerProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

	/// <summary>
	/// The template stream to be copied into report and filled up with Strategy, Orders and Trades sheets.
	/// </summary>
	public Stream TemplateStream { get; } = templateStream;

	/// <summary>
	/// The number of decimal places. By default, it equals to 2.
	/// </summary>
	public int Decimals { get; set; } = 2;

	/// <inheritdoc />
	public override string Name => "EXCEL";

	/// <inheritdoc />
	public override string Extension => "xlsx";

	/// <inheritdoc />
	public override ValueTask Generate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		if (stream == null)
			throw new ArgumentNullException(nameof(stream));

		var hasTemplate = TemplateStream is not null;

		if (hasTemplate)
		{
			TemplateStream.CopyTo(stream);
			stream.Position = 0;
		}

		using var worker = hasTemplate ? _provider.OpenExist(stream) : _provider.CreateNew(stream);

		if (!hasTemplate)
		{
			worker.AddSheet();
			worker.RenameSheet(source.Name);
		}
		else
		{
			if (!worker.ContainsSheet(source.Name))
			{
				worker.AddSheet().RenameSheet(source.Name);
			}
		}

		worker
			.SetCell(0, 0, LocalizedStrings.Info)

			.SetCell(0, 1, LocalizedStrings.WorkingTime)
			.SetCell(1, 1, source.TotalWorkingTime.Format())

			.SetCell(0, 2, LocalizedStrings.Position + ":")
			.SetCell(1, 2, source.Position)

			.SetCell(0, 3, LocalizedStrings.PnL + ":")
			.SetCell(1, 3, source.PnL)

			.SetCell(0, 4, LocalizedStrings.Commission + ":")
			.SetCell(1, 4, source.Commission)

			.SetCell(0, 5, LocalizedStrings.Slippage + ":")
			.SetCell(1, 5, source.Slippage)

			.SetCell(0, 6, LocalizedStrings.Latency + ":")
			.SetCell(1, 6, source.Latency.Format());

		var rowIndex = 7;

		foreach (var (name, value) in source.StatisticParameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var cellValue = value;

			if (cellValue is TimeSpan ts)
				cellValue = ts.Format();
			else if (cellValue is DateTime dto)
				cellValue = dto.Format();
			else if (cellValue is decimal dec)
				cellValue = dec.Round(Decimals);

			worker
				.SetCell(0, rowIndex, name)
				.SetCell(1, rowIndex, cellValue);

			rowIndex++;
		}

		rowIndex += 2;
		worker.SetCell(0, rowIndex, LocalizedStrings.Parameters);
		rowIndex++;

		foreach (var (name, value) in source.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var cellValue = value;

			if (cellValue is WorkingTime)
				continue;

			if (cellValue is TimeSpan ts)
				cellValue = ts.Format();
			else if (cellValue is decimal dec)
				cellValue = dec.Round(Decimals);

			worker
				.SetCell(0, rowIndex, name)
				.SetCell(1, rowIndex, cellValue);

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
				.SetCell(columnShift + 9, 1, LocalizedStrings.PnL).SetStyle(columnShift + 9, typeof(decimal))
				.SetCell(columnShift + 10, 1, LocalizedStrings.TotalPnL).SetStyle(columnShift + 10, typeof(decimal))
				.SetCell(columnShift + 11, 1, LocalizedStrings.Position).SetStyle(columnShift + 11, typeof(decimal));

			var totalPnL = 0m;
			var position = 0m;

			rowIndex = 2;
			foreach (var trade in source.MyTrades.ToArray())
			{
				cancellationToken.ThrowIfCancellationRequested();

				var pnl = trade.PnL ?? 0;

				totalPnL += pnl;
				position += trade.Position ?? 0;

				worker
					.SetCell(columnShift + 0, rowIndex, trade.TradeId)
					.SetCell(columnShift + 1, rowIndex, trade.OrderTransactionId)
					.SetCell(columnShift + 2, rowIndex, trade.Time.Format())
					.SetCell(columnShift + 3, rowIndex, trade.TradePrice)
					.SetCell(columnShift + 4, rowIndex, trade.OrderPrice)
					.SetCell(columnShift + 5, rowIndex, trade.Volume)
					.SetCell(columnShift + 6, rowIndex, trade.Side.GetDisplayName())
					.SetCell(columnShift + 7, rowIndex, trade.OrderId)
					.SetCell(columnShift + 8, rowIndex, trade.Slippage)
					.SetCell(columnShift + 9, rowIndex, pnl)
					.SetCell(columnShift + 10, rowIndex, totalPnL.Round(Decimals))
					.SetCell(columnShift + 11, rowIndex, position);

				rowIndex++;
			}
		}

		if (IncludeOrders)
		{
			columnShift += 13;

			worker
				.SetCell(columnShift + 0, 0, LocalizedStrings.Orders)

				.SetCell(columnShift + 0, 1, LocalizedStrings.Identifier).SetStyle(columnShift + 0, typeof(long))
				.SetCell(columnShift + 1, 1, LocalizedStrings.Transaction).SetStyle(columnShift + 1, typeof(long))
				.SetCell(columnShift + 2, 1, LocalizedStrings.Direction)
				.SetCell(columnShift + 3, 1, LocalizedStrings.Time).SetStyle(columnShift + 3, "HH:mm:ss.fff")
				.SetCell(columnShift + 4, 1, LocalizedStrings.Price).SetStyle(columnShift + 4, typeof(decimal))
				.SetCell(columnShift + 5, 1, LocalizedStrings.Status)
				.SetCell(columnShift + 6, 1, LocalizedStrings.Balance).SetStyle(columnShift + 6, typeof(decimal))
				.SetCell(columnShift + 7, 1, LocalizedStrings.Volume).SetStyle(columnShift + 7, typeof(decimal))
				.SetCell(columnShift + 8, 1, LocalizedStrings.Type);

			rowIndex = 2;
			foreach (var order in source.Orders.ToArray())
			{
				cancellationToken.ThrowIfCancellationRequested();

				worker
					.SetCell(columnShift + 0, rowIndex, order.Id)
					.SetCell(columnShift + 1, rowIndex, order.TransactionId)
					.SetCell(columnShift + 2, rowIndex, order.Side.GetDisplayName())
					.SetCell(columnShift + 3, rowIndex, order.Time.Format())
					.SetCell(columnShift + 4, rowIndex, order.Price)
					.SetCell(columnShift + 5, rowIndex, order.State.GetDisplayName())
					.SetCell(columnShift + 6, rowIndex, order.Balance)
					.SetCell(columnShift + 7, rowIndex, order.Volume)
					.SetCell(columnShift + 8, rowIndex, order.Type.GetDisplayName());

				rowIndex++;
			}
		}

		return default;
	}
}
