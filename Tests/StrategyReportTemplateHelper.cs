namespace StockSharp.Tests;

using System;
using System.IO;

using Ecng.Interop;

/// <summary>
/// Utility for generating strategy report Excel templates.
/// Can be used to regenerate the template file when structure changes.
/// </summary>
public static class StrategyReportTemplateHelper
{
	/// <summary>
	/// Strategy info section header.
	/// </summary>
	public const string InfoHeader = "Strategy Info";

	/// <summary>
	/// Trades section header.
	/// </summary>
	public const string TradesHeader = "Trades";

	/// <summary>
	/// Orders section header.
	/// </summary>
	public const string OrdersHeader = "Orders";

	/// <summary>
	/// Summary sheet name.
	/// </summary>
	public const string SummarySheetName = "Summary";

	/// <summary>
	/// Column index where trades section starts.
	/// </summary>
	public const int TradesColumnOffset = 3;

	/// <summary>
	/// Column index where orders section starts.
	/// </summary>
	public const int OrdersColumnOffset = 17;

	/// <summary>
	/// Creates a template Excel workbook with predefined structure.
	/// </summary>
	/// <param name="provider">Excel worker provider.</param>
	/// <returns>Stream containing the template workbook.</returns>
	public static MemoryStream CreateTemplate(IExcelWorkerProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var stream = new MemoryStream();

		using (var worker = provider.CreateNew(stream))
		{
			worker.AddSheet().RenameSheet(SummarySheetName);

			// Info section header
			worker.SetCell(0, 0, InfoHeader);

			// Info rows - labels only (values filled at runtime)
			worker.SetCell(0, 1, "Working Time");
			worker.SetCell(0, 2, "Position");
			worker.SetCell(0, 3, "PnL");
			worker.SetCell(0, 4, "Commission");
			worker.SetCell(0, 5, "Slippage");
			worker.SetCell(0, 6, "Latency");

			// Set column styles for numeric values
			worker.SetStyle(1, typeof(decimal));

			// Trades section header
			worker.SetCell(TradesColumnOffset, 0, TradesHeader);

			// Trades column headers
			var tradesHeaders = new[]
			{
				"ID", "Transaction", "Time", "Price", "Order Price",
				"Volume", "Direction", "Order", "Slippage", "Comment",
				"PnL", "Total PnL", "Position"
			};

			for (var i = 0; i < tradesHeaders.Length; i++)
			{
				worker.SetCell(TradesColumnOffset + i, 1, tradesHeaders[i]);
			}

			// Set column styles for trades
			worker.SetStyle(TradesColumnOffset + 0, typeof(long));     // ID
			worker.SetStyle(TradesColumnOffset + 1, typeof(long));     // Transaction
			worker.SetStyle(TradesColumnOffset + 2, "HH:mm:ss.fff");   // Time
			worker.SetStyle(TradesColumnOffset + 3, typeof(decimal));  // Price
			worker.SetStyle(TradesColumnOffset + 4, typeof(decimal));  // Order Price
			worker.SetStyle(TradesColumnOffset + 5, typeof(decimal));  // Volume
			worker.SetStyle(TradesColumnOffset + 7, typeof(long));     // Order
			worker.SetStyle(TradesColumnOffset + 8, typeof(decimal));  // Slippage
			worker.SetStyle(TradesColumnOffset + 10, typeof(decimal)); // PnL
			worker.SetStyle(TradesColumnOffset + 11, typeof(decimal)); // Total PnL
			worker.SetStyle(TradesColumnOffset + 12, typeof(decimal)); // Position

			// Orders section header
			worker.SetCell(OrdersColumnOffset, 0, OrdersHeader);

			// Orders column headers
			var ordersHeaders = new[]
			{
				"ID", "Transaction", "Direction", "Reg Time", "Change Time",
				"Duration", "Price", "Status", "State", "Balance",
				"Volume", "Type", "Latency Reg", "Latency Cancel", "Edition Latency", "Comment"
			};

			for (var i = 0; i < ordersHeaders.Length; i++)
			{
				worker.SetCell(OrdersColumnOffset + i, 1, ordersHeaders[i]);
			}

			// Set column styles for orders
			worker.SetStyle(OrdersColumnOffset + 0, typeof(long));     // ID
			worker.SetStyle(OrdersColumnOffset + 1, typeof(long));     // Transaction
			worker.SetStyle(OrdersColumnOffset + 3, "HH:mm:ss.fff");   // Reg Time
			worker.SetStyle(OrdersColumnOffset + 4, "HH:mm:ss.fff");   // Change Time
			worker.SetStyle(OrdersColumnOffset + 6, typeof(decimal));  // Price
			worker.SetStyle(OrdersColumnOffset + 9, typeof(decimal));  // Balance
			worker.SetStyle(OrdersColumnOffset + 10, typeof(decimal)); // Volume
		}

		stream.Position = 0;
		return stream;
	}

	/// <summary>
	/// Saves a template to the specified file path.
	/// </summary>
	/// <param name="provider">Excel worker provider.</param>
	/// <param name="filePath">Path to save the template.</param>
	public static void SaveTemplateToFile(IExcelWorkerProvider provider, string filePath)
	{
		using var template = CreateTemplate(provider);
		using var file = File.Create(filePath);
		template.CopyTo(file);
	}
}
