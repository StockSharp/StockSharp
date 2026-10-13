namespace StockSharp.Fix.Native;

/// <summary>
/// Data from HistoryStart FIX message.
/// </summary>
/// <param name="StartDate">Start date for history request.</param>
/// <param name="EndDate">End date for history request.</param>
public record struct FixHistoryStart(
	DateTime? StartDate,
	DateTime? EndDate);
