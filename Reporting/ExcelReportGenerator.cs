namespace StockSharp.Reporting;

using Ecng.Excel;

/// <summary>
/// The report generator for the strategy in the Excel format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExcelReportGenerator"/>.
/// </remarks>
/// <param name="provider"><see cref="IExcelWorkerProvider"/>.</param>
/// <param name="template">The template bytes to be copied into report and filled. Thread-safe for parallel use.</param>
public class ExcelReportGenerator(IExcelWorkerProvider provider, ReadOnlyMemory<byte> template = default) : BaseReportGenerator
{
	private readonly IExcelWorkerProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

	/// <summary>
	/// The template bytes to be copied into report and filled.
	/// </summary>
	public ReadOnlyMemory<byte> Template { get; } = template;

	/// <summary>
	/// The number of decimal places for formatting. By default, it equals to 2.
	/// </summary>
	public int Decimals { get; set; } = 2;

	/// <inheritdoc />
	public override string Name => "EXCEL";

	/// <inheritdoc />
	public override string Extension => "xlsx";

	// Template sheet names (localized)
	private readonly string _dashboardSheet = LocalizedStrings.Dashboard;
	private readonly string _paramsSheet = LocalizedStrings.Params;
	private readonly string _equitySheet = LocalizedStrings.Equity;
	private readonly string _tradesSheet = LocalizedStrings.Trades;
	private readonly string _ordersSheet = LocalizedStrings.Orders;
	private readonly string _statsSheet = LocalizedStrings.Statistics;

	// English fallback names for template (template uses English sheet names)
	private const string _dashboardSheetEn = "Dashboard";
	private const string _paramsSheetEn = "Params";
	private const string _equitySheetEn = "Equity";
	private const string _tradesSheetEn = "Trades";
	private const string _ordersSheetEn = "Orders";
	private const string _statsSheetEn = "Stats";

	private const string _templateResourceName = "StockSharp.Reporting.Resources.StrategyReportTemplate.xlsx";

	/// <summary>
	/// Gets the embedded template from resources as byte array.
	/// </summary>
	/// <returns>Template bytes, or empty if not found.</returns>
	public static byte[] GetTemplate()
	{
		var assembly = typeof(ExcelReportGenerator).Assembly;
		using var stream = assembly.GetManifestResourceStream(_templateResourceName);
		if (stream == null)
			return [];

		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		return ms.ToArray();
	}

	/// <inheritdoc />
	protected override ValueTask OnGenerate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(stream);

		if (!Template.IsEmpty)
			GenerateWithTemplate(source, stream, cancellationToken);
		else
			GenerateWithoutTemplate(source, stream, cancellationToken);

		return default;
	}

	/// <summary>
	/// Switch to sheet by localized name, falling back to English name if not found.
	/// </summary>
	private static void SwitchSheet(IExcelWorker worker, string localizedName, string englishName)
	{
		worker.SwitchSheet(worker.ContainsSheet(localizedName) ? localizedName : englishName);
	}

	private void GenerateWithTemplate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		// Copy template into output stream
		stream.SetLength(0);
		stream.Write(Template.Span);
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
		SwitchSheet(worker, _dashboardSheet, _dashboardSheetEn);
	}

	private void GenerateWithoutTemplate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		stream.SetLength(0);

		using var worker = _provider.CreateNew(stream, false);

		// Create sheets in template order: Dashboard, Params, Equity, Trades, Stats, Orders

		// 1. Dashboard sheet
		worker.AddSheet().RenameSheet(_dashboardSheet);
		CreateDashboardSheet(worker, source, cancellationToken);

		// 2. Params sheet
		worker.AddSheet().RenameSheet(_paramsSheet);
		CreateParamsSheet(worker, source, cancellationToken);

		// 3. Equity sheet
		worker.AddSheet().RenameSheet(_equitySheet);
		CreateEquitySheet(worker, source, cancellationToken);

		// 4. Trades sheet
		if (IncludeTrades)
		{
			worker.AddSheet().RenameSheet(_tradesSheet);
			CreateTradesSheet(worker, source, cancellationToken);
		}

		// 5. Stats sheet
		worker.AddSheet().RenameSheet(_statsSheet);
		CreateStatsSheet(worker, source, cancellationToken);

		// 6. Orders sheet
		if (IncludeOrders)
		{
			worker.AddSheet().RenameSheet(_ordersSheet);
			CreateOrdersSheet(worker, source, cancellationToken);
		}

		// Add charts to Dashboard (after all data sheets are populated)
		AddDashboardCharts(worker, source);

		// Switch to Dashboard as the default sheet (like template)
		worker.SwitchSheet(_dashboardSheet);
	}

	private string GetDecimalFormat()
		=> Decimals <= 0 ? "#,##0" : $"#,##0.{new string('0', Decimals)}";

	private void CreateParamsSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Header styling (same as template)
		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";
		const string labelBg = "D9E2F3";

		// Column widths matching template
		worker
			.SetColumnWidth(0, 26)  // A - labels
			.SetColumnWidth(1, 20)  // B - values
			.SetColumnWidth(2, 3)   // C - spacer
			.SetColumnWidth(3, 26)  // D - labels
			.SetColumnWidth(4, 20); // E - values

		// Row 0 (Excel row 1): Title "Parameters"
		worker
			.SetCell(0, 0, LocalizedStrings.Parameters)
			.SetCellColor(0, 0, headerBg, headerFg)
			.MergeCells(0, 0, 3, 0);

		// Rows 1-10: Strategy parameters (matching template structure)
		// Row 1: Strategy Name
		worker
			.SetCell(0, 1, LocalizedStrings.StrategyName)
			.SetCellColor(0, 1, labelBg, null)
			.SetCell(1, 1, source.Name);

		// Row 2: Run Date
		worker
			.SetCell(0, 2, LocalizedStrings.StartTime)
			.SetCellColor(0, 2, labelBg, null)
			.SetCell(1, 2, DateTime.Now)
			.SetCellFormat(1, 2, "yyyy-MM-dd HH:mm:ss");

		// Row 3: Symbol
		worker.SetCell(0, 3, LocalizedStrings.Security).SetCellColor(0, 3, labelBg, null);
		WriteParamIfExistsCreate(worker, source, LocalizedStrings.Security, 1, 3);

		// Row 4: Timeframe
		worker.SetCell(0, 4, LocalizedStrings.TimeFrame).SetCellColor(0, 4, labelBg, null);
		WriteParamIfExistsCreate(worker, source, LocalizedStrings.TimeFrame, 1, 4);

		// Row 5: Initial Capital
		worker.SetCell(0, 5, "Initial Capital").SetCellColor(0, 5, labelBg, null);
		var initialCapital = GetDecimalParam(source, "InitialCapital");
		worker.SetCell(1, 5, initialCapital > 0 ? initialCapital : 100000m)
			.SetCellFormat(1, 5, GetDecimalFormat());

		// Row 6: Currency
		worker.SetCell(0, 6, LocalizedStrings.Currency).SetCellColor(0, 6, labelBg, null);
		WriteParamIfExistsCreate(worker, source, LocalizedStrings.Currency, 1, 6);
		if (worker.GetCell<string>(1, 6).IsEmpty())
			worker.SetCell(1, 6, "USD");

		// Row 7: Commission (per trade)
		worker.SetCell(0, 7, LocalizedStrings.TradeCommission).SetCellColor(0, 7, labelBg, null)
			.SetCell(1, 7, source.Commission ?? 0m)
			.SetCellFormat(1, 7, GetDecimalFormat());

		// Row 8: Slippage (per trade)
		worker.SetCell(0, 8, LocalizedStrings.SlippageTrade).SetCellColor(0, 8, labelBg, null)
			.SetCell(1, 8, source.Slippage ?? 0m)
			.SetCellFormat(1, 8, GetDecimalFormat());

		// Row 9: Risk-free rate (annual)
		worker.SetCell(0, 9, LocalizedStrings.RiskFreeRate).SetCellColor(0, 9, labelBg, null)
			.SetCell(1, 9, 0m)
			.SetCellFormat(1, 9, "0.00%");

		// Row 10: Sharpe (placeholder for Stats sheet)
		worker.SetCell(0, 10, LocalizedStrings.Sharpe).SetCellColor(0, 10, labelBg, null)
			.SetCell(1, 10, "");

		// Row 13: Section headers
		worker
			.SetCell(0, 13, LocalizedStrings.Statistics)
			.SetCellColor(0, 13, headerBg, headerFg)
			.SetCell(3, 13, LocalizedStrings.Parameters)
			.SetCellColor(3, 13, headerBg, headerFg);

		// Row 14: Column headers for tables
		worker
			.SetCell(0, 14, LocalizedStrings.Name)
			.SetCell(1, 14, LocalizedStrings.Value)
			.SetCellColor(0, 14, labelBg, null)
			.SetCellColor(1, 14, labelBg, null)
			.SetCell(3, 14, LocalizedStrings.Name)
			.SetCell(4, 14, LocalizedStrings.Value)
			.SetCellColor(3, 14, labelBg, null)
			.SetCellColor(4, 14, labelBg, null);

		// Fill Statistics (A16:B...)
		var statRow = 15;
		foreach (var (name, value) in source.StatisticParameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, statRow, name)
				.SetCell(1, statRow, NormalizeCellValue(value));

			ApplyCellFormat(worker, 1, statRow, value);
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

			ApplyCellFormat(worker, 4, paramRow, value);
			paramRow++;
		}

		worker.FreezeRows(1);
	}

	private static void WriteParamIfExistsCreate(IExcelWorker worker, IReportSource source, string key, int col, int row)
	{
		foreach (var (name, value) in source.Parameters)
		{
			if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
			{
				worker.SetCell(col, row, NormalizeCellValue(value));
				return;
			}
		}
	}

	private void CreateDashboardSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";
		const string labelBg = "D9E2F3";

		// Column widths matching template
		worker
			.SetColumnWidth(0, 22)
			.SetColumnWidth(1, 22);

		// Row 0: Title "Strategy Report" merged A1:H1
		worker
			.SetCell(0, 0, LocalizedStrings.Report)
			.SetCellColor(0, 0, headerBg, headerFg)
			.MergeCells(0, 0, 7, 0);

		// Rows 3-9: Summary metrics (matching template structure)
		// Note: Template uses formulas, but without template we fill static values

		// Row 3: Strategy
		worker
			.SetCell(0, 3, LocalizedStrings.Strategy)
			.SetCellColor(0, 3, labelBg, null)
			.SetCell(1, 3, source.Name);

		// Row 4: Run Date
		worker
			.SetCell(0, 4, LocalizedStrings.StartTime)
			.SetCellColor(0, 4, labelBg, null)
			.SetCell(1, 4, DateTime.Now)
			.SetCellFormat(1, 4, "yyyy-MM-dd HH:mm:ss");

		// Row 5: Net PnL
		worker
			.SetCell(0, 5, LocalizedStrings.PnL)
			.SetCellColor(0, 5, labelBg, null)
			.SetCell(1, 5, source.PnL)
			.SetCellFormat(1, 5, GetDecimalFormat());

		// Row 6: Max Drawdown (calculate from trades)
		var maxDrawdown = CalculateMaxDrawdown(source);
		worker
			.SetCell(0, 6, LocalizedStrings.MaxDrawdown)
			.SetCellColor(0, 6, labelBg, null)
			.SetCell(1, 6, maxDrawdown)
			.SetCellFormat(1, 6, "0.00%");

		// Row 7: Sharpe (placeholder)
		worker
			.SetCell(0, 7, LocalizedStrings.Sharpe)
			.SetCellColor(0, 7, labelBg, null)
			.SetCell(1, 7, GetStatParam(source, LocalizedStrings.Sharpe));

		// Row 8: Win Rate
		var winRate = CalculateWinRate(source);
		worker
			.SetCell(0, 8, LocalizedStrings.WinRate)
			.SetCellColor(0, 8, labelBg, null)
			.SetCell(1, 8, winRate)
			.SetCellFormat(1, 8, "0.00%");

		// Row 9: Trades count
		var tradesCount = source.OwnTrades.Count();
		worker
			.SetCell(0, 9, LocalizedStrings.Trades)
			.SetCellColor(0, 9, labelBg, null)
			.SetCell(1, 9, tradesCount);

		// Charts will be added later after Equity data is populated
	}

	private void AddDashboardCharts(IExcelWorker worker, IReportSource source)
	{
		var trades = source.OwnTrades.ToArray();
		if (trades.Length == 0)
			return;

		// Calculate equity data row count
		var byDay = trades
			.OrderBy(t => t.Time)
			.GroupBy(t => t.Time.Date)
			.Count();

		if (byDay == 0)
			return;

		var equityRowCount = byDay + 1; // +1 for header

		worker.SwitchSheet(_dashboardSheet);

		// Chart 1: "Equity Curve" at anchor col=4 (D), row=4 - matches template
		// xCol=1 (A - Date), yCol=2 (B - Equity), wider chart with more space
		var equityDataRange = $"{_equitySheet}!$A$1:$B${equityRowCount}";
		worker.AddLineChart(LocalizedStrings.EquityCurve, equityDataRange, 1, 2, 4, 4, 700, 280);

		// Chart 2: "Drawdown" at anchor col=4 (D), row=22 - more spacing from first chart
		// xCol=1 (A - Date), yCol=3 (C - Drawdown)
		var drawdownDataRange = $"{_equitySheet}!$A$1:$C${equityRowCount}";
		worker.AddLineChart(LocalizedStrings.Drawdown, drawdownDataRange, 1, 3, 4, 22, 700, 280);
	}

	private void CreateStatsSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";
		const string labelBg = "D9E2F3";

		// Column widths and styles matching template
		worker
			.SetColumnWidth(0, 10)  // Year
			.SetColumnWidth(1, 10)  // Month
			.SetColumnWidth(2, 12)  // Return
			.SetColumnWidth(3, 14)  // Cumulative
			.SetStyle(0, "0")       // Year as integer
			.SetStyle(1, "0")       // Month as integer
			.SetStyle(2, "0.00%")   // Return as percent
			.SetStyle(3, "0.00%")   // Cumulative as percent
			// ColorScale for Return (col 2) starting from row 3 (after headers) - matching template
			.SetColorScale(2, 3, "F8696B", "FFEB84", "63BE7B");

		// Row 0: Title
		worker
			.SetCell(0, 0, LocalizedStrings.MonthlyReturns)
			.SetCellColor(0, 0, headerBg, headerFg)
			.MergeCells(0, 0, 5, 0);

		// Row 2: Headers
		worker
			.SetCell(0, 2, LocalizedStrings.Year)
			.SetCellColor(0, 2, labelBg, null)
			.SetCell(1, 2, LocalizedStrings.Month)
			.SetCellColor(1, 2, labelBg, null)
			.SetCell(2, 2, LocalizedStrings.Return)
			.SetCellColor(2, 2, labelBg, null)
			.SetCell(3, 2, LocalizedStrings.Cumulative)
			.SetCellColor(3, 2, labelBg, null);

		// Calculate monthly returns from trades
		var trades = source.OwnTrades.ToArray();
		if (trades.Length == 0)
		{
			worker.FreezeRows(3);
			return;
		}

		var monthlyReturns = trades
			.GroupBy(t => new { t.Time.Year, t.Time.Month })
			.OrderBy(g => g.Key.Year)
			.ThenBy(g => g.Key.Month)
			.Select(g => new
			{
				g.Key.Year,
				g.Key.Month,
				Return = g.Sum(t => t.PnL ?? 0m)
			})
			.ToList();

		var row = 3;
		decimal cumulative = 0m;
		var initialCapital = GetDecimalParam(source, LocalizedStrings.InitialCapital);
		if (initialCapital <= 0) initialCapital = 100000m;

		foreach (var mr in monthlyReturns)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var returnPct = mr.Return / initialCapital;
			cumulative += returnPct;

			worker
				.SetCell(0, row, mr.Year)
				.SetCell(1, row, mr.Month)
				.SetCell(2, row, returnPct)
				.SetCellFormat(2, row, "0.00%")
				.SetCell(3, row, cumulative)
				.SetCellFormat(3, row, "0.00%");

			row++;
		}

		worker.FreezeRows(3);

		// Add Monthly Returns chart at anchor col=6 (F), row=3 (matching template)
		if (row > 3)
		{
			// Use columns A-D, xCol=2 (Month), yCol=3 (Return)
			var returnDataRange = $"{_statsSheet}!$A$3:$D${row}";
			worker.AddLineChart(LocalizedStrings.MonthlyReturns, returnDataRange, 2, 3, 6, 3, 500, 300);
		}
	}

	private static decimal CalculateMaxDrawdown(IReportSource source)
	{
		var trades = source.OwnTrades.ToArray();
		if (trades.Length == 0)
			return 0m;

		decimal cumPnL = 0m;
		decimal peak = 0m;
		decimal maxDrawdown = 0m;

		foreach (var trade in trades.OrderBy(t => t.Time))
		{
			cumPnL += trade.PnL ?? 0m;
			if (cumPnL > peak)
				peak = cumPnL;

			if (peak > 0)
			{
				var drawdown = (cumPnL - peak) / peak;
				if (drawdown < maxDrawdown)
					maxDrawdown = drawdown;
			}
		}

		return maxDrawdown;
	}

	private static decimal CalculateWinRate(IReportSource source)
	{
		var trades = source.OwnTrades.ToArray();
		if (trades.Length == 0)
			return 0m;

		var winningTrades = trades.Count(t => (t.PnL ?? 0m) > 0);
		return (decimal)winningTrades / trades.Length;
	}

	private static object GetStatParam(IReportSource source, string key)
	{
		foreach (var (name, value) in source.StatisticParameters)
		{
			if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
				return value;
		}
		return "";
	}

	private void CreateTradesSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";

		// Header row matching template: EntryTime, ExitTime, Security, Side, Qty, EntryPrice, ExitPrice, PnL, PnL%, TotalPnL, Position
		var headers = new[] { LocalizedStrings.EntryTime, LocalizedStrings.ExitTime, LocalizedStrings.Security, LocalizedStrings.Side, LocalizedStrings.Volume, LocalizedStrings.Enter, LocalizedStrings.Exit, LocalizedStrings.PnL, LocalizedStrings.PnLChange, LocalizedStrings.TotalPnL, LocalizedStrings.Position };
		for (var i = 0; i < headers.Length; i++)
		{
			worker
				.SetCell(i, 0, headers[i])
				.SetCellColor(i, 0, headerBg, headerFg);
		}

		var decimalFormat = GetDecimalFormat();

		worker
			.SetColumnWidth(0, 18)  // Entry Time
			.SetColumnWidth(1, 18)  // Exit Time
			.SetColumnWidth(2, 16)  // Security
			.SetColumnWidth(3, 8)   // Side
			.SetColumnWidth(4, 10)  // Qty
			.SetColumnWidth(5, 12)  // Entry Price
			.SetColumnWidth(6, 12)  // Exit Price
			.SetColumnWidth(7, 12)  // PnL
			.SetColumnWidth(8, 10)  // PnL%
			.SetColumnWidth(9, 12)  // Total PnL
			.SetColumnWidth(10, 10) // Position
			.SetStyle(0, typeof(DateTime))
			.SetStyle(1, typeof(DateTime))
			.SetStyle(4, decimalFormat)
			.SetStyle(5, decimalFormat)
			.SetStyle(6, decimalFormat)
			.SetStyle(7, decimalFormat)
			.SetStyle(8, "0.00%")
			.SetStyle(9, decimalFormat)
			.SetStyle(10, decimalFormat)
			.FreezeRows(1)
			// ColorScale for PnL (col 7) and TotalPnL (col 9) - matching template
			.SetColorScale(7, 1, "F8696B", "FFEB84", "63BE7B")
			.SetColorScale(9, 1, "F8696B", "FFEB84", "63BE7B");

		var row = 1;
		decimal totalPnL = 0m;
		decimal position = 0m;

		foreach (var trade in source.OwnTrades.ToArray())
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
				.SetCell(0, row, trade.Time)           // Entry Time
				.SetCell(1, row, trade.Time)           // Exit Time
				.SetCell(2, row, trade.SecurityId.ToStringId()) // Security
				.SetCell(3, row, trade.Side.GetDisplayName())
				.SetCell(4, row, qty)                  // Qty
				.SetCell(5, row, entryPrice)           // Entry Price
				.SetCell(6, row, exitPrice)            // Exit Price
				.SetCell(7, row, pnl)                  // PnL
				.SetCell(8, row, pnlPct)               // PnL%
				.SetCell(9, row, totalPnL)             // Total PnL
				.SetCell(10, row, position);           // Position

			// Color Side column: Buy = green, Sell = red
			var (sideBg, sideFg) = trade.Side == Sides.Buy
				? ("C6EFCE", "006100")  // Light green bg, dark green text
				: ("FFC7CE", "9C0006"); // Light red bg, dark red text
			worker.SetCellColor(3, row, sideBg, sideFg);

			row++;
		}
	}

	private void CreateOrdersSheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";

		// Header row matching template
		var headers = new[] { LocalizedStrings.OrderId, LocalizedStrings.TransactionId, LocalizedStrings.Security, LocalizedStrings.Side, LocalizedStrings.Time, LocalizedStrings.Price, LocalizedStrings.State, LocalizedStrings.Balance, LocalizedStrings.Volume, LocalizedStrings.Type };
		for (var i = 0; i < headers.Length; i++)
		{
			worker
				.SetCell(i, 0, headers[i])
				.SetCellColor(i, 0, headerBg, headerFg);
		}

		var decimalFormat = GetDecimalFormat();

		worker
			.SetColumnWidth(0, 14)  // Order ID
			.SetColumnWidth(1, 14)  // Transaction ID
			.SetColumnWidth(2, 16)  // Security
			.SetColumnWidth(3, 8)   // Side
			.SetColumnWidth(4, 18)  // Time
			.SetColumnWidth(5, 12)  // Price
			.SetColumnWidth(6, 10)  // State
			.SetColumnWidth(7, 10)  // Balance
			.SetColumnWidth(8, 10)  // Volume
			.SetColumnWidth(9, 10)  // Type
			.SetStyle(0, "0")       // Order ID as integer
			.SetStyle(1, "0")       // Transaction ID as integer
			.SetStyle(4, typeof(DateTime))
			.SetStyle(5, decimalFormat)
			.SetStyle(7, decimalFormat)
			.SetStyle(8, decimalFormat)
			.FreezeRows(1)
			// ColorScale for Balance (col 7) - 0 = green (fully executed), >0 = red (has remaining)
			.SetColorScale(7, 1, "63BE7B", "FFEB84", "F8696B");

		var row = 1;

		foreach (var order in source.Orders.ToArray())
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, row, order.Id)
				.SetCell(1, row, order.TransactionId)
				.SetCell(2, row, order.SecurityId.ToStringId())
				.SetCell(3, row, order.Side.GetDisplayName())
				.SetCell(4, row, order.Time)
				.SetCell(5, row, order.Price)
				.SetCell(6, row, order.State.GetDisplayName())
				.SetCell(7, row, order.Balance)
				.SetCell(8, row, order.Volume)
				.SetCell(9, row, order.Type.GetDisplayName());

			// Color Side column: Buy = green, Sell = red
			var (sideBg, sideFg) = order.Side == Sides.Buy
				? ("C6EFCE", "006100")  // Light green bg, dark green text
				: ("FFC7CE", "9C0006"); // Light red bg, dark red text
			worker.SetCellColor(3, row, sideBg, sideFg);

			// Color State column based on order state
			var (stateBg, stateFg) = order.State switch
			{
				OrderStates.Done => ("C6EFCE", "006100"),    // Green - completed
				OrderStates.Failed => ("FFC7CE", "9C0006"), // Red - failed/error
				OrderStates.Active => ("FFEB84", "806000"), // Yellow - active
				_ => (null, null)       // No color for others
			};
			if (stateBg != null)
				worker.SetCellColor(6, row, stateBg, stateFg);

			row++;
		}
	}

	private void CreateEquitySheet(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		const string headerBg = "4472C4";
		const string headerFg = "FFFFFF";

		// Header row matching template
		var headers = new[] { LocalizedStrings.Date, LocalizedStrings.Equity, LocalizedStrings.Drawdown };
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
			.SetStyle(1, GetDecimalFormat())
			.SetStyle(2, "0.00%")
			.FreezeRows(1)
			// ColorScale for Drawdown (col 2) - matching template
			.SetColorScale(2, 1, "F8696B", "FFEB84", "63BE7B");

		var trades = source.OwnTrades.ToArray();
		if (trades.Length == 0)
			return;

		var initial = GetDecimalParam(source, LocalizedStrings.InitialCapital);

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
				.SetCell(1, row, equity)
				.SetCell(2, row, dd);

			row++;
		}

		// Add Equity line chart
		if (row > 1)
		{
			var dataRange = $"{_equitySheet}!$A$2:$B${row}";
			worker.AddLineChart(LocalizedStrings.EquityCurve, dataRange, 1, 2, 4, 1, 400, 250);
		}
	}

	private void FillParams(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		SwitchSheet(worker, _paramsSheet, _paramsSheetEn);

		// Template cells (0-based indices):
		// B2 -> (col=1,row=1) Strategy Name
		// B3 -> (col=1,row=2) Report Date/Time
		worker
			.SetCell(1, 1, source.Name)
			.SetCell(1, 2, DateTime.Now);

		// Optional: try to populate commonly used params if present in source.Parameters
		WriteParamIfExists(worker, source, LocalizedStrings.Security, 1, 3);         // B4
		WriteParamIfExists(worker, source, LocalizedStrings.TimeFrame, 1, 4);      // B5
		WriteParamIfExists(worker, source, LocalizedStrings.InitialCapital, 1, 5); // B6

		// Fill StatisticParameters table: A16:B...
		var row = 15; // 0-based row => Excel row 16
		foreach (var (name, value) in source.StatisticParameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, row, name)
				.SetCell(1, row, NormalizeCellValue(value));

			ApplyCellFormat(worker, 1, row, value);
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

			ApplyCellFormat(worker, 4, row, value);
			row++;
		}
	}

	private void FillTrades(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		SwitchSheet(worker, _tradesSheet, _tradesSheetEn);

		// Trades template columns:
		// A EntryTime, B ExitTime, C Security, D Side, E Qty, F EntryPrice, G ExitPrice,
		// H PnL, I PnL%, J TotalPnL, K Position
		var row = 1; // data starts from Excel row 2
		decimal totalPnL = 0m;
		decimal position = 0m;

		foreach (var trade in source.OwnTrades.ToArray())
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
				.SetCell(0, row, trade.Time)           // EntryTime
				.SetCell(1, row, trade.Time)           // ExitTime
				.SetCell(2, row, trade.SecurityId.ToStringId()) // Security
				.SetCell(3, row, trade.Side.GetDisplayName())
				.SetCell(4, row, qty)                  // Qty
				.SetCell(5, row, entryPrice)           // EntryPrice
				.SetCell(6, row, exitPrice)            // ExitPrice
				.SetCell(7, row, pnl)                  // PnL
				.SetCell(8, row, pnlPct)               // PnL%
				.SetCell(9, row, totalPnL)             // TotalPnL
				.SetCell(10, row, position);           // Position

			row++;
		}
	}

	private void FillOrders(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		SwitchSheet(worker, _ordersSheet, _ordersSheetEn);

		// Orders template columns:
		// A OrderId, B TransactionId, C Security, D Side, E Time, F Price, G State, H Balance, I Volume, J Type
		var row = 1; // data starts from Excel row 2

		foreach (var order in source.Orders.ToArray())
		{
			cancellationToken.ThrowIfCancellationRequested();

			worker
				.SetCell(0, row, order.Id)
				.SetCell(1, row, order.TransactionId)
				.SetCell(2, row, order.SecurityId.ToStringId())
				.SetCell(3, row, order.Side.GetDisplayName())
				.SetCell(4, row, order.Time)
				.SetCell(5, row, order.Price)
				.SetCell(6, row, order.State.GetDisplayName())
				.SetCell(7, row, order.Balance)
				.SetCell(8, row, order.Volume)
				.SetCell(9, row, order.Type.GetDisplayName());

			// Ensure Order ID and Transaction ID are formatted as numbers, not dates
			worker
				.SetCellFormat(0, row, "0")
				.SetCellFormat(1, row, "0");

			row++;
		}
	}

	private void FillEquity(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var trades = source.OwnTrades.ToArray();
		if (trades.Length == 0)
			return;

		// InitialCapital can be provided as a strategy parameter. If missing, equity becomes cumulative PnL.
		var initial = GetDecimalParam(source, LocalizedStrings.InitialCapital);

		// Build a daily equity curve from cumulative trade PnL at day close.
		var byDay = new SortedDictionary<DateTime, decimal>();
		decimal cumPnL = 0m;

		foreach (var tr in trades.OrderBy(t => t.Time))
		{
			cancellationToken.ThrowIfCancellationRequested();

			cumPnL += tr.PnL ?? 0m;
			byDay[tr.Time.Date] = cumPnL;
		}

		SwitchSheet(worker, _equitySheet, _equitySheetEn);

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
				.SetCell(1, row, equity)
				.SetCell(2, row, dd);

			row++;
		}

		// Add Equity line chart
		if (row > 1)
		{
			var dataRange = $"{_equitySheet}!$A$2:$B${row}";
			worker.AddLineChart(LocalizedStrings.EquityCurve, dataRange, 1, 2, 4, 1, 400, 250);
		}

		// Stats sheet is formula-driven; just ensure it exists and can be recalculated.
		SwitchSheet(worker, _statsSheet, _statsSheetEn);
	}

	private static void WriteParamIfExists(IExcelWorker worker, IReportSource source, string key, int col, int row)
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

	private static object NormalizeCellValue(object value)
	{
		if (value is null)
			return null;

		if (value is DateTimeOffset dto)
			return dto;

		if (value is DateTime dt)
			return dt;

		if (value is TimeSpan ts)
			return ts.Format();

		return value;
	}

	private void ApplyCellFormat(IExcelWorker worker, int col, int row, object value)
	{
		var format = value switch
		{
			decimal => GetDecimalFormat(),
			double => GetDecimalFormat(),
			float => GetDecimalFormat(),
			int => "0",
			long => "0",
			DateTime => "yyyy-MM-dd HH:mm:ss",
			DateTimeOffset => "yyyy-MM-dd HH:mm:ss",
			_ => null
		};

		if (format != null)
			worker.SetCellFormat(col, row, format);
	}
}
