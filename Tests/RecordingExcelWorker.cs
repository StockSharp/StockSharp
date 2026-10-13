namespace StockSharp.Tests;

using Ecng.Excel;

/// <summary>
/// A <see cref="DateTime"/> value written into a workbook cell, as the writer passed it.
/// </summary>
/// <param name="Sheet">Sheet name the cell belongs to.</param>
/// <param name="Col">Column index (0-based).</param>
/// <param name="Row">Row index (0-based).</param>
/// <param name="Value">The written value.</param>
record ExcelDateCell(string Sheet, int Col, int Row, DateTime Value);

/// <summary>
/// Decorates <see cref="IExcelWorkerProvider"/> and records every <see cref="DateTime"/> cell written
/// through it. A workbook keeps dates as OADate numbers, which drop <see cref="DateTime.Kind"/>, so the
/// clock a writer used can only be observed at the moment of the write.
/// </summary>
/// <param name="inner">The provider doing the real work.</param>
sealed class RecordingExcelWorkerProvider(IExcelWorkerProvider inner) : IExcelWorkerProvider
{
	private readonly IExcelWorkerProvider _inner = inner ?? throw new ArgumentNullException(nameof(inner));

	/// <summary>
	/// All date cells written through this provider, in write order.
	/// </summary>
	public List<ExcelDateCell> DateCells { get; } = [];

	/// <summary>
	/// Finds a written date by cell address, accepting any of the passed sheet names.
	/// </summary>
	/// <param name="col">Column index (0-based).</param>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="sheets">Accepted sheet names.</param>
	/// <returns>The written value, or <see langword="null"/> if the cell was never written.</returns>
	public DateTime? TryGetDate(int col, int row, params string[] sheets)
	{
		foreach (var cell in DateCells)
		{
			if (cell.Col == col && cell.Row == row && sheets.Contains(cell.Sheet))
				return cell.Value;
		}

		return null;
	}

	IExcelWorker IExcelWorkerProvider.CreateNew(Stream stream, bool readOnly)
		=> new RecordingExcelWorker(_inner.CreateNew(stream, readOnly), DateCells);

	IExcelWorker IExcelWorkerProvider.OpenExist(Stream stream)
		=> new RecordingExcelWorker(_inner.OpenExist(stream), DateCells);
}

/// <summary>
/// Decorates <see cref="IExcelWorker"/>, forwarding everything and recording date cells with the sheet they land on.
/// </summary>
/// <param name="inner">The worker doing the real work.</param>
/// <param name="dateCells">The list to record into.</param>
sealed class RecordingExcelWorker(IExcelWorker inner, List<ExcelDateCell> dateCells) : IExcelWorker
{
	private readonly IExcelWorker _inner = inner ?? throw new ArgumentNullException(nameof(inner));
	private readonly List<ExcelDateCell> _dateCells = dateCells ?? throw new ArgumentNullException(nameof(dateCells));

	private string _sheet;

	IExcelWorker IExcelWorker.SetCell<T>(int col, int row, T value)
	{
		if (value is DateTime dt)
			_dateCells.Add(new(_sheet, col, row, dt));

		_inner.SetCell(col, row, value);
		return this;
	}

	T IExcelWorker.GetCell<T>(int col, int row) => _inner.GetCell<T>(col, row);

	IExcelWorker IExcelWorker.AddSheet()
	{
		_inner.AddSheet();
		_sheet = null;
		return this;
	}

	IExcelWorker IExcelWorker.RenameSheet(string name)
	{
		_inner.RenameSheet(name);
		_sheet = name;
		return this;
	}

	IExcelWorker IExcelWorker.SwitchSheet(string name)
	{
		_inner.SwitchSheet(name);
		_sheet = name;
		return this;
	}

	IExcelWorker IExcelWorker.DeleteSheet(string name)
	{
		_inner.DeleteSheet(name);
		return this;
	}

	bool IExcelWorker.ContainsSheet(string name) => _inner.ContainsSheet(name);
	IEnumerable<string> IExcelWorker.GetSheetNames() => _inner.GetSheetNames();
	int IExcelWorker.GetColumnsCount() => _inner.GetColumnsCount();
	int IExcelWorker.GetRowsCount() => _inner.GetRowsCount();

	IExcelWorker IExcelWorker.SetStyle(int col, Type type)
	{
		_inner.SetStyle(col, type);
		return this;
	}

	IExcelWorker IExcelWorker.SetStyle(int col, string format)
	{
		_inner.SetStyle(col, format);
		return this;
	}

	IExcelWorker IExcelWorker.SetConditionalFormatting(int col, ComparisonOperator op, string condition, string bgColor, string fgColor)
	{
		_inner.SetConditionalFormatting(col, op, condition, bgColor, fgColor);
		return this;
	}

	IExcelWorker IExcelWorker.SetConditionalFormattingFormula(int startCol, int startRow, int endCol, int endRow, string formula, string bgColor, string fgColor)
	{
		_inner.SetConditionalFormattingFormula(startCol, startRow, endCol, endRow, formula, bgColor, fgColor);
		return this;
	}

	IExcelWorker IExcelWorker.SetConditionalFormattingFormula(int startCol, int startRow, int endCol, int endRow, string formula, ExcelConditionalFormat format)
	{
		_inner.SetConditionalFormattingFormula(startCol, startRow, endCol, endRow, formula, format);
		return this;
	}

	IExcelWorker IExcelWorker.SetColorScale(int col, int startRow, string minColor, string midColor, string maxColor)
	{
		_inner.SetColorScale(col, startRow, minColor, midColor, maxColor);
		return this;
	}

	IExcelWorker IExcelWorker.SetColumnWidth(int col, double width)
	{
		_inner.SetColumnWidth(col, width);
		return this;
	}

	IExcelWorker IExcelWorker.SetRowHeight(int row, double height)
	{
		_inner.SetRowHeight(row, height);
		return this;
	}

	IExcelWorker IExcelWorker.AutoFitColumn(int col)
	{
		_inner.AutoFitColumn(col);
		return this;
	}

	IExcelWorker IExcelWorker.FreezeRows(int count)
	{
		_inner.FreezeRows(count);
		return this;
	}

	IExcelWorker IExcelWorker.FreezeCols(int count)
	{
		_inner.FreezeCols(count);
		return this;
	}

	IExcelWorker IExcelWorker.MergeCells(int startCol, int startRow, int endCol, int endRow)
	{
		_inner.MergeCells(startCol, startRow, endCol, endRow);
		return this;
	}

	IExcelWorker IExcelWorker.SetHyperlink(int col, int row, string url, string text)
	{
		_inner.SetHyperlink(col, row, url, text);
		return this;
	}

	IExcelWorker IExcelWorker.SetCellFormat(int col, int row, string format)
	{
		_inner.SetCellFormat(col, row, format);
		return this;
	}

	IExcelWorker IExcelWorker.SetCellColor(int col, int row, string bgColor, string fgColor)
	{
		_inner.SetCellColor(col, row, bgColor, fgColor);
		return this;
	}

	IExcelWorker IExcelWorker.SetCellColor(int col, int row, string bgColor, ExcelFillPattern pattern, string patternColor, string fgColor)
	{
		_inner.SetCellColor(col, row, bgColor, pattern, patternColor, fgColor);
		return this;
	}

	IExcelWorker IExcelWorker.AddLineChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddLineChart(name, dataRange, xCol, yCol, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddBarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddBarChart(name, dataRange, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddPieChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddPieChart(name, dataRange, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddPieChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height, IEnumerable<string> colors)
	{
		_inner.AddPieChart(name, dataRange, anchorCol, anchorRow, width, height, colors);
		return this;
	}

	IExcelWorker IExcelWorker.AddAreaChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddAreaChart(name, dataRange, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddDoughnutChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddDoughnutChart(name, dataRange, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddScatterChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddScatterChart(name, dataRange, xCol, yCol, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddRadarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddRadarChart(name, dataRange, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddBubbleChart(string name, string dataRange, int xCol, int yCol, int sizeCol, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddBubbleChart(name, dataRange, xCol, yCol, sizeCol, anchorCol, anchorRow, width, height);
		return this;
	}

	IExcelWorker IExcelWorker.AddStockChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
	{
		_inner.AddStockChart(name, dataRange, anchorCol, anchorRow, width, height);
		return this;
	}

	void IDisposable.Dispose() => _inner.Dispose();
}
