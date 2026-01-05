namespace StockSharp.Algo.Testing;

/// <summary>
/// Default implementation of <see cref="ITradingTimeLineGenerator"/>.
/// </summary>
public class TradingTimeLineGenerator : ITradingTimeLineGenerator
{
	/// <inheritdoc />
	public IEnumerable<TimeMessage> GetSimpleTimeLine(BoardMessage[] boards, DateTime date, TimeSpan interval)
	{
		if (boards == null)
			throw new ArgumentNullException(nameof(boards));

		var ranges = GetOrderedRanges(boards, date);
		var lastTime = TimeSpan.Zero;

		foreach (var range in ranges)
		{
			var time = GetTime(date, range.range.Min);
			if (time.Date >= date.Date)
				yield return new TimeMessage { ServerTime = time };

			time = GetTime(date, range.range.Max);
			if (time.Date >= date.Date)
				yield return new TimeMessage { ServerTime = time };

			lastTime = range.range.Max;
		}
	}

	/// <inheritdoc />
	public IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTime date, TimeSpan lastTime, TimeSpan interval, int count)
	{
		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be positive.");

		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");

		for (var i = 0; i < count; i++)
		{
			lastTime += interval;

			if (lastTime > TimeHelper.LessOneDay)
				break;

			yield return new TimeMessage
			{
				ServerTime = GetTime(date, lastTime)
			};
		}
	}

	/// <inheritdoc />
	public IEnumerable<(BoardMessage board, Range<TimeSpan> range)> GetOrderedRanges(BoardMessage[] boards, DateTime date)
	{
		if (boards == null)
			throw new ArgumentNullException(nameof(boards));

		var orderedRanges = boards
			.Where(b => b.IsTradeDate(date, true))
			.SelectMany(board =>
			{
				var period = board.WorkingTime.GetPeriod(date);

				return period == null || period.Times.Count == 0
					? [(board, new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay))]
					: period.Times.Select(t => (board, ranges: ToUtc(board, t)));
			})
			.OrderBy(i => i.ranges.Min)
			.ToList();

		for (var i = 0; i < orderedRanges.Count - 1;)
		{
			if (orderedRanges[i].ranges.Contains(orderedRanges[i + 1].ranges))
			{
				orderedRanges.RemoveAt(i + 1);
			}
			else if (orderedRanges[i + 1].ranges.Contains(orderedRanges[i].ranges))
			{
				orderedRanges.RemoveAt(i);
			}
			else if (orderedRanges[i].ranges.Intersect(orderedRanges[i + 1].ranges) != null)
			{
				orderedRanges[i] = (orderedRanges[i].board, new Range<TimeSpan>(orderedRanges[i].ranges.Min, orderedRanges[i + 1].ranges.Max));
				orderedRanges.RemoveAt(i + 1);
			}
			else
				i++;
		}

		return orderedRanges;
	}

	private static Range<TimeSpan> ToUtc(BoardMessage board, Range<TimeSpan> range)
	{
		var min = DateTime.MinValue + range.Min;
		var max = DateTime.MinValue + range.Max;

		var utcMin = min.To(board.TimeZone);
		var utcMax = max.To(board.TimeZone);

		return new Range<TimeSpan>(utcMin.TimeOfDay, utcMax.TimeOfDay);
	}

	/// <inheritdoc />
	public bool IsTradeDate(BoardMessage[] boards, DateTime date)
	{
		if (boards == null)
			throw new ArgumentNullException(nameof(boards));

		return boards.Any(b => b.IsTradeDate(date, true));
	}

	private static DateTime GetTime(DateTime date, TimeSpan timeOfDay)
		=> date.Date + timeOfDay;
}
