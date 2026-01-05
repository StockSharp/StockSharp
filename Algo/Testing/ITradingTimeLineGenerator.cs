namespace StockSharp.Algo.Testing;

/// <summary>
/// Interface for generating trading time line and working with trading schedules.
/// </summary>
public interface ITradingTimeLineGenerator
{
	/// <summary>
	/// Generate simple time line messages for a date when no market data is available.
	/// </summary>
	/// <param name="boards">Trading boards.</param>
	/// <param name="date">Date.</param>
	/// <param name="interval">Time interval.</param>
	/// <returns>Time messages.</returns>
	IEnumerable<TimeMessage> GetSimpleTimeLine(BoardMessage[] boards, DateTime date, TimeSpan interval);

	/// <summary>
	/// Generate post-trade time messages after trading session ends.
	/// </summary>
	/// <param name="date">Date.</param>
	/// <param name="lastTime">Last trading time.</param>
	/// <param name="interval">Time interval.</param>
	/// <param name="count">Number of messages to generate.</param>
	/// <returns>Time messages.</returns>
	IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTime date, TimeSpan lastTime, TimeSpan interval, int count);

	/// <summary>
	/// Get ordered and merged trading time ranges for boards.
	/// </summary>
	/// <param name="boards">Trading boards.</param>
	/// <param name="date">Date.</param>
	/// <returns>Ordered ranges with associated boards.</returns>
	IEnumerable<(BoardMessage board, Range<TimeSpan> range)> GetOrderedRanges(BoardMessage[] boards, DateTime date);

	/// <summary>
	/// Check if any board has trading on the specified date.
	/// </summary>
	/// <param name="boards">Trading boards.</param>
	/// <param name="date">Date to check.</param>
	/// <returns><see langword="true"/> if at least one board is trading.</returns>
	bool IsTradeDate(BoardMessage[] boards, DateTime date);
}
