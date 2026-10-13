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

		foreach (var range in GetOrderedRanges(boards, date))
		{
			var time = GetTime(date, range.range.Min);
			yield return new TimeMessage { ServerTime = time };

			time = GetTime(date, range.range.Max);
			yield return new TimeMessage { ServerTime = time };
		}
	}

	/// <inheritdoc />
	public IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTime date, TimeSpan lastTime, TimeSpan interval, int count)
	{
		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be positive.");

		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");

		var endOfUtcDay = TimeSpan.FromDays(Math.Floor(lastTime.TotalDays)) + TimeHelper.LessOneDay;

		for (var i = 0; i < count; i++)
		{
			lastTime += interval;

			if (lastTime > endOfUtcDay)
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
			.Where(b => b.IsWorkingDate(date, true))
			.SelectMany(board =>
			{
				var period = board.WorkingTime.GetPeriod(date);

				IEnumerable<Range<TimeSpan>> ranges = period == null || period.Times.Count == 0
					? [new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay)]
					: period.Times;

				return ranges.Select(t => (board, ranges: ToUtc(board, date, t)));
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

	private static Range<TimeSpan> ToUtc(BoardMessage board, DateTime date, Range<TimeSpan> range)
	{
		var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

		DateTime toUtc(TimeSpan time)
		{
			var localTime = DateTime.SpecifyKind(date.Date + time, DateTimeKind.Unspecified);
			return TimeZoneInfo.ConvertTimeToUtc(localTime, board.TimeZone);
		}

		return new Range<TimeSpan>(toUtc(range.Min) - utcDate, toUtc(range.Max) - utcDate);
	}

	/// <inheritdoc />
	public bool IsTradeDate(BoardMessage[] boards, DateTime date)
	{
		if (boards == null)
			throw new ArgumentNullException(nameof(boards));

		return boards.Any(b => b.IsWorkingDate(date, true));
	}

	private static DateTime GetTime(DateTime date, TimeSpan timeOfDay)
		=> date.Date + timeOfDay;
}
