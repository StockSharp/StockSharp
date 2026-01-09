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

	/// <inheritdoc />
	public override ValueTask Generate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));

		if (stream == null)
			throw new ArgumentNullException(nameof(stream));

		if (TemplateStream == null)
			throw new InvalidOperationException("TemplateStream is required for template-driven Excel report.");

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

		return default;
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
			.SetCell(1, 2, DateTimeOffset.Now);

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

	private void FillOrders(IExcelWorker worker, IReportSource source, CancellationToken cancellationToken)
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
