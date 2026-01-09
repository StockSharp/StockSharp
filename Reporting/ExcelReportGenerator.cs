namespace StockSharp.Reporting;

using Ecng.Excel;

/// <summary>
/// The report generator for the strategy in the Excel format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExcelReportGenerator"/>.
/// </remarks>
/// <param name="provider"><see cref="IExcelWorkerProvider"/>.</param>
/// <param name="templateStream">The template stream to be copied into report and filled.</param>
public class ExcelReportGenerator(IExcelWorkerProvider provider, Stream templateStream = null) : BaseReportGenerator
{
	private readonly IExcelWorkerProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

	/// <summary>
	/// The template stream to be copied into report and filled.
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

	// Template sheet names
	private const string _dashboardSheet = "Dashboard";
	private const string _paramsSheet = "Params";
	private const string _equitySheet = "Equity";
	private const string _tradesSheet = "Trades";
	private const string _ordersSheet = "Orders";
	private const string _statsSheet = "Stats";

	private const string _templateResourceName = "StockSharp.Reporting.Resources.StrategyReportTemplate.xlsx";

	/// <summary>
	/// Gets the embedded template stream from resources.
	/// </summary>
	/// <returns>Stream containing the template, or null if not found.</returns>
	public static Stream GetTemplateStream()
	{
		var assembly = typeof(ExcelReportGenerator).Assembly;
		return assembly.GetManifestResourceStream(_templateResourceName);
	}

	/// <inheritdoc />
	public override ValueTask Generate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));

		if (stream == null)
			throw new ArgumentNullException(nameof(stream));

		if (TemplateStream != null)
			GenerateWithTemplate(source, stream, cancellationToken);
		else
			GenerateWithoutTemplate(source, stream, cancellationToken);

		return default;
	}

	private void GenerateWithTemplate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		// Copy template into output stream (do not append).
		if (TemplateStream.CanSeek)
			TemplateStream.Position = 0;

		stream.SetLength(0);
		TemplateStream.CopyTo(stream);
		stream.Position = 0;

		using var worker = _provider.OpenExist(stream);

		FillParams(worker, source, cancellationToken);

		if (IncludeTrades)
			FillTrades(worker, source, cancellationToken);

		if (IncludeOrders)
			FillOrders(worker, source, cancellationToken);

		// Equity/Stats depend on Trades (PnL).
		FillEquity(worker, source, cancellationToken);

		// Dashboard/Stats are template-formula driven.
		worker.SwitchSheet(_dashboardSheet);
	}

	private void GenerateWithoutTemplate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		stream.SetLength(0);

		using var worker = _provider.CreateNew(stream);

		// Create Params sheet (first sheet - AddSheet creates a new one since CreateNew doesn't create initial sheet)
		worker.AddSheet().RenameSheet(_paramsSheet);
		CreateParamsSheet(worker, source, cancellationToken);

		// Create Trades sheet
		if (IncludeTrades)
		{
			worker.AddSheet().RenameSheet(_tradesSheet);
			CreateTradesSheet(worker, source, cancellationToken);
		}

		// Create Orders sheet
		if (IncludeOrders)
		{
			worker.AddSheet().RenameSheet(_ordersSheet);
			CreateOrdersSheet(worker, source, cancellationToken);
		}

		// Create Equity sheet
		worker.AddSheet().RenameSheet(_equitySheet);
		CreateEquitySheet(worker, source, cancellationToken);

		// Switch back to Params as the default sheet
		worker.SwitchSheet(_paramsSheet);
	}

	private void CreateParamsSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Header styling (same as template)
		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";

		// Create layout matching template structure
		// Row 0: Title row
		worker
			.SetCell(0, 0, "Strategy Report")
			.SetCellColor(0, 0, headerBg, headerFg)
			.MergeCells(0, 0, 4, 0)
			.SetColumnWidth(0, 20)
			.SetColumnWidth(1, 25)
			.SetColumnWidth(2, 5)
			.SetColumnWidth(3, 20)
			.SetColumnWidth(4, 25);

		// Strategy info section (matching template: B2, B3, B4, B5, B6)
		worker
			.SetCell(0, 1, "Strategy Name")
			.SetCell(1, 1, source.Name)
			.SetCell(0, 2, "Report Date")
			.SetCell(1, 2, DateTime.Now)
			.SetCellFormat(1, 2, "yyyy-MM-dd HH:mm:ss");

		// Try to get common params like template expects
		WriteParamIfExistsCreate(worker, source, "Symbol", 0, 3, 1, 3);
		WriteParamIfExistsCreate(worker, source, "TimeFrame", 0, 4, 1, 4);
		WriteParamIfExistsCreate(worker, source, "InitialCapital", 0, 5, 1, 5);

		// Summary section
		worker
			.SetCell(0, 7, "Summary")
			.SetCellColor(0, 7, headerBg, headerFg)
			.MergeCells(0, 7, 1, 7)
			.SetCell(0, 8, "Total PnL")
			.SetCell(1, 8, source.PnL)
			.SetCell(0, 9, "Position")
			.SetCell(1, 9, source.Position)
			.SetCell(0, 10, "Commission")
			.SetCell(1, 10, source.Commission)
			.SetCell(0, 11, "Working Time")
			.SetCell(1, 11, source.TotalWorkingTime.Format());

		// Statistics table header (row 14, matching template ~row 15/16)
		worker
			.SetCell(0, 14, "Statistic")
			.SetCell(1, 14, "Value")
			.SetCellColor(0, 14, headerBg, headerFg)
			.SetCellColor(1, 14, headerBg, headerFg);

		// Parameters table header
		worker
			.SetCell(3, 14, "Parameter")
			.SetCell(4, 14, "Value")
			.SetCellColor(3, 14, headerBg, headerFg)
			.SetCellColor(4, 14, headerBg, headerFg);

		// Fill Statistics (A16:B...)
		var statRow = 15;
		foreach (var (name, value) in source.StatisticParameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, statRow, name)
				.SetCell(1, statRow, NormalizeCellValue(value));

			statRow++;
		}

		// Fill Parameters (D16:E...)
		var paramRow = 15;
		foreach (var (name, value) in source.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (value is WorkingTime)
				continue;

			worker
				.SetCell(3, paramRow, name)
				.SetCell(4, paramRow, NormalizeCellValue(value));

			paramRow++;
		}

		worker.FreezeRows(1);
	}

	private void WriteParamIfExistsCreate(IExcelWorker worker, IReportSource source, string key, int labelCol, int labelRow, int valueCol, int valueRow)
	{
		worker.SetCell(labelCol, labelRow, key);

		foreach (var (name, value) in source.Parameters)
		{
			if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
			{
				worker.SetCell(valueCol, valueRow, NormalizeCellValue(value));
				return;
			}
		}
	}

	private void CreateTradesSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";

		// Header row matching template: EntryTime, ExitTime, Side, Qty, EntryPrice, ExitPrice, PnL, PnL%, TotalPnL, Position
		var headers = new[] { "Entry Time", "Exit Time", "Side", "Qty", "Entry Price", "Exit Price", "PnL", "PnL%", "Total PnL", "Position" };
		for (var i = 0; i < headers.Length; i++)
		{
			worker
				.SetCell(i, 0, headers[i])
				.SetCellColor(i, 0, headerBg, headerFg);
		}

		worker
			.SetColumnWidth(0, 18)  // Entry Time
			.SetColumnWidth(1, 18)  // Exit Time
			.SetColumnWidth(2, 8)   // Side
			.SetColumnWidth(3, 10)  // Qty
			.SetColumnWidth(4, 12)  // Entry Price
			.SetColumnWidth(5, 12)  // Exit Price
			.SetColumnWidth(6, 12)  // PnL
			.SetColumnWidth(7, 10)  // PnL%
			.SetColumnWidth(8, 12)  // Total PnL
			.SetColumnWidth(9, 10)  // Position
			.SetStyle(0, typeof(DateTime))
			.SetStyle(1, typeof(DateTime))
			.SetStyle(4, typeof(decimal))
			.SetStyle(5, typeof(decimal))
			.SetStyle(6, typeof(decimal))
			.SetStyle(7, "0.00%")
			.SetStyle(8, typeof(decimal))
			.FreezeRows(1)
			.SetConditionalFormatting(6, ComparisonOperator.Less, "0", "FFC7CE", "9C0006")
			.SetConditionalFormatting(6, ComparisonOperator.Greater, "0", "C6EFCE", "006100");

		var row = 1;
		decimal totalPnL = 0m;
		decimal position = 0m;

		foreach (var trade in source.MyTrades.ToArray())
		{
			cancellationToken.ThrowIfCancellationRequested();

			var pnl = trade.PnL ?? 0m;
			totalPnL += pnl;
			position += trade.Position ?? 0m;

			var qty = trade.Volume;
			var entryPrice = trade.OrderPrice;
			var exitPrice = trade.TradePrice;

			decimal pnlPct = 0m;
			var denom = Math.Abs(entryPrice) * qty;
			if (denom != 0)
				pnlPct = pnl / denom;

			worker
				.SetCell(0, row, trade.Time)                       // Entry Time (fallback to trade time)
				.SetCell(1, row, trade.Time)                       // Exit Time (fallback to trade time)
				.SetCell(2, row, trade.Side.GetDisplayName())      // Side
				.SetCell(3, row, qty)                              // Qty
				.SetCell(4, row, entryPrice)                       // Entry Price
				.SetCell(5, row, exitPrice)                        // Exit Price
				.SetCell(6, row, pnl.Round(Decimals))              // PnL
				.SetCell(7, row, pnlPct)                           // PnL%
				.SetCell(8, row, totalPnL.Round(Decimals))         // Total PnL
				.SetCell(9, row, position);                        // Position

			row++;
		}
	}

	private static void CreateOrdersSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";

		// Header row matching template
		var headers = new[] { "Order ID", "Transaction ID", "Side", "Time", "Price", "State", "Balance", "Volume", "Type" };
		for (var i = 0; i < headers.Length; i++)
		{
			worker
				.SetCell(i, 0, headers[i])
				.SetCellColor(i, 0, headerBg, headerFg);
		}

		worker
			.SetColumnWidth(0, 14)  // Order ID
			.SetColumnWidth(1, 14)  // Transaction ID
			.SetColumnWidth(2, 8)   // Side
			.SetColumnWidth(3, 18)  // Time
			.SetColumnWidth(4, 12)  // Price
			.SetColumnWidth(5, 10)  // State
			.SetColumnWidth(6, 10)  // Balance
			.SetColumnWidth(7, 10)  // Volume
			.SetColumnWidth(8, 10)  // Type
			.SetStyle(3, typeof(DateTime))
			.SetStyle(4, typeof(decimal))
			.SetStyle(6, typeof(decimal))
			.SetStyle(7, typeof(decimal))
			.FreezeRows(1);

		var row = 1;

		foreach (var order in source.Orders.ToArray())
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, row, order.Id)
				.SetCell(1, row, order.TransactionId)
				.SetCell(2, row, order.Side.GetDisplayName())
				.SetCell(3, row, order.Time)
				.SetCell(4, row, order.Price)
				.SetCell(5, row, order.State.GetDisplayName())
				.SetCell(6, row, order.Balance)
				.SetCell(7, row, order.Volume)
				.SetCell(8, row, order.Type.GetDisplayName());

			row++;
		}
	}

	private void CreateEquitySheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";

		// Header row matching template
		var headers = new[] { "Date", "Equity", "Drawdown" };
		for (var i = 0; i < headers.Length; i++)
		{
			worker
				.SetCell(i, 0, headers[i])
				.SetCellColor(i, 0, headerBg, headerFg);
		}

		worker
			.SetColumnWidth(0, 12)  // Date
			.SetColumnWidth(1, 15)  // Equity
			.SetColumnWidth(2, 12)  // Drawdown
			.SetStyle(0, "yyyy-MM-dd")
			.SetStyle(1, typeof(decimal))
			.SetStyle(2, "0.00%")
			.FreezeRows(1)
			.SetConditionalFormatting(2, ComparisonOperator.Less, "0", "FFC7CE", "9C0006");

		var trades = source.MyTrades.ToArray();
		if (trades.Length == 0)
			return;

		var initial = GetDecimalParam(source, "InitialCapital");

		// Build a daily equity curve from cumulative trade PnL at day close.
		var byDay = new SortedDictionary<DateTime, decimal>();
		decimal cumPnL = 0m;

		foreach (var tr in trades.OrderBy(t => t.Time))
		{
			cancellationToken.ThrowIfCancellationRequested();

			cumPnL += tr.PnL ?? 0m;
			byDay[tr.Time.Date] = cumPnL;
		}

		var row = 1;
		var peak = initial;

		foreach (var (d, dayPnL) in byDay)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var equity = initial + dayPnL;
			if (equity > peak)
				peak = equity;

			var dd = peak == 0 ? 0 : (equity / peak) - 1;

			worker
				.SetCell(0, row, d)
				.SetCell(1, row, equity.Round(Decimals))
				.SetCell(2, row, dd);

			row++;
		}
	}

	private void FillParams(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		worker.SwitchSheet(_paramsSheet);

		// Template cells (0-based indices):
		// B2 -> (col=1,row=1) Strategy Name
		// B3 -> (col=1,row=2) Report Date/Time
		worker
			.SetCell(1, 1, source.Name)
			.SetCell(1, 2, DateTime.Now);

		// Optional: try to populate commonly used params if present in source.Parameters
		WriteParamIfExists(worker, source, "Symbol", 1, 3);         // B4
		WriteParamIfExists(worker, source, "TimeFrame", 1, 4);      // B5
		WriteParamIfExists(worker, source, "InitialCapital", 1, 5); // B6

		// Fill StatisticParameters table: A16:B...
		var row = 15; // 0-based row => Excel row 16
		foreach (var (name, value) in source.StatisticParameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, row, name)
				.SetCell(1, row, NormalizeCellValue(value));

			row++;
		}

		// Fill Parameters table: D16:E...
		row = 15;
		foreach (var (name, value) in source.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (value is WorkingTime)
				continue;

			worker
				.SetCell(3, row, name)
				.SetCell(4, row, NormalizeCellValue(value));

			row++;
		}
	}

	private void FillTrades(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		worker.SwitchSheet(_tradesSheet);

		// Trades template columns:
		// A EntryTime, B ExitTime, C Side, D Qty, E EntryPrice, F ExitPrice,
		// G PnL, H PnL%, I TotalPnL, J Position
		//
		// We only have MyTrades (single fill). For Entry/Exit we use trade.Time as fallback.
		var row = 1; // data starts from Excel row 2
		decimal totalPnL = 0m;
		decimal position = 0m;

		foreach (var trade in source.MyTrades.ToArray())
		{
			cancellationToken.ThrowIfCancellationRequested();

			var pnl = trade.PnL ?? 0m;
			totalPnL += pnl;

			// Keep the same "position accumulation" semantics as your original code.
			position += trade.Position ?? 0m;

			var qty = trade.Volume;
			var entryPrice = trade.OrderPrice;
			var exitPrice = trade.TradePrice;

			decimal pnlPct = 0m;
			var denom = Math.Abs(entryPrice) * qty;
			if (denom != 0)
				pnlPct = pnl / denom;

			worker
				.SetCell(0, row, trade.Time)                       // EntryTime (fallback)
				.SetCell(1, row, trade.Time)                       // ExitTime (fallback)
				.SetCell(2, row, trade.Side.GetDisplayName())      // Side
				.SetCell(3, row, qty)                              // Qty
				.SetCell(4, row, entryPrice)                       // EntryPrice
				.SetCell(5, row, exitPrice)                        // ExitPrice
				.SetCell(6, row, pnl.Round(Decimals))              // PnL
				.SetCell(7, row, pnlPct)                           // PnL%
				.SetCell(8, row, totalPnL.Round(Decimals))         // TotalPnL
				.SetCell(9, row, position);                        // Position

			row++;
		}
	}

	private static void FillOrders(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		worker.SwitchSheet(_ordersSheet);

		// Orders template columns:
		// A OrderId, B TransactionId, C Side, D Time, E Price, F State, G Balance, H Volume, I Type
		var row = 1; // data starts from Excel row 2

		foreach (var order in source.Orders.ToArray())
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, row, order.Id)
				.SetCell(1, row, order.TransactionId)
				.SetCell(2, row, order.Side.GetDisplayName())
				.SetCell(3, row, order.Time) // IMPORTANT: native DateTime/DateTimeOffset, not string
				.SetCell(4, row, order.Price)
				.SetCell(5, row, order.State.GetDisplayName())
				.SetCell(6, row, order.Balance)
				.SetCell(7, row, order.Volume)
				.SetCell(8, row, order.Type.GetDisplayName());

			row++;
		}
	}

	private void FillEquity(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var trades = source.MyTrades.ToArray();
		if (trades.Length == 0)
			return;

		// InitialCapital can be provided as a strategy parameter. If missing, equity becomes cumulative PnL.
		var initial = GetDecimalParam(source, "InitialCapital");

		// Build a daily equity curve from cumulative trade PnL at day close.
		var byDay = new SortedDictionary<DateTime, decimal>();
		decimal cumPnL = 0m;

		foreach (var tr in trades.OrderBy(t => t.Time))
		{
			cancellationToken.ThrowIfCancellationRequested();

			cumPnL += tr.PnL ?? 0m;
			byDay[tr.Time.Date] = cumPnL;
		}

		worker.SwitchSheet(_equitySheet);

		// Equity template columns:
		// A Date, B Equity, C Drawdown
		var row = 1; // data starts from Excel row 2
		var peak = initial;

		foreach (var (d, dayPnL) in byDay)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var equity = initial + dayPnL;
			if (equity > peak)
				peak = equity;

			var dd = peak == 0 ? 0 : (equity / peak) - 1;

			worker
				.SetCell(0, row, d)
				.SetCell(1, row, equity.Round(Decimals))
				.SetCell(2, row, dd);

			row++;
		}

		// Stats sheet is formula-driven; just ensure it exists and can be recalculated.
		worker.SwitchSheet(_statsSheet);
	}

	private void WriteParamIfExists(IExcelWorker worker, IReportSource source, string key, int col, int row)
	{
		foreach (var (name, value) in source.Parameters)
		{
			if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
				continue;

			worker.SetCell(col, row, NormalizeCellValue(value));
			return;
		}
	}

	private static decimal GetDecimalParam(IReportSource source, string key)
	{
		foreach (var (name, value) in source.Parameters)
		{
			if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
				continue;

			if (value is decimal d) return d;
			if (value is double db) return (decimal)db;
			if (value is float f) return (decimal)f;
			if (value is int i) return i;
			if (value is long l) return l;
		}

		return 0m;
	}

	private object NormalizeCellValue(object value)
	{
		if (value is null)
			return null;

		if (value is DateTimeOffset dto)
			return dto;

		if (value is DateTime dt)
			return dt;

		if (value is TimeSpan ts)
			return ts.Format();

		if (value is decimal dec)
			return dec.Round(Decimals);

		return value;
	}
}
