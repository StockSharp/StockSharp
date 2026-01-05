namespace StockSharp.Tests;

using StockSharp.Algo.Testing;

[TestClass]
public class TradingTimeLineGeneratorTests : BaseTestClass
{
	private static TradingTimeLineGenerator CreateGenerator() => new();

	private static BoardMessage CreateBoard(string code, TimeZoneInfo timeZone = null, params (TimeSpan from, TimeSpan to)[] workingTimes)
	{
		var board = new BoardMessage
		{
			Code = code,
			TimeZone = timeZone ?? TimeZoneInfo.Utc,
			WorkingTime = new WorkingTime { IsEnabled = true },
		};

		if (workingTimes.Length > 0)
		{
			var period = new WorkingTimePeriod { Till = DateTime.MaxValue };
			foreach (var (from, to) in workingTimes)
			{
				period.Times.Add(new Range<TimeSpan>(from, to));
			}
			board.WorkingTime.Periods.Add(period);
		}

		return board;
	}

	#region GetOrderedRanges Tests

	[TestMethod]
	public void GetOrderedRanges_EmptyBoards_ReturnsEmpty()
	{
		var generator = CreateGenerator();
		var boards = Array.Empty<BoardMessage>();

		var ranges = generator.GetOrderedRanges(boards, DateTime.Today).ToList();

		ranges.Count.AssertEqual(0);
	}

	[TestMethod]
	public void GetOrderedRanges_SingleBoard_ReturnsOneRange()
	{
		var generator = CreateGenerator();
		var board = CreateBoard("TEST", TimeZoneInfo.Utc, (TimeSpan.FromHours(9), TimeSpan.FromHours(18)));
		var boards = new[] { board };

		var ranges = generator.GetOrderedRanges(boards, DateTime.Today).ToList();

		ranges.Count.AssertEqual(1);
		ranges[0].range.Min.AssertEqual(TimeSpan.FromHours(9));
		ranges[0].range.Max.AssertEqual(TimeSpan.FromHours(18));
	}

	[TestMethod]
	public void GetOrderedRanges_OverlappingRanges_MergesThem()
	{
		var generator = CreateGenerator();
		var board1 = CreateBoard("TEST1", TimeZoneInfo.Utc, (TimeSpan.FromHours(9), TimeSpan.FromHours(14)));
		var board2 = CreateBoard("TEST2", TimeZoneInfo.Utc, (TimeSpan.FromHours(12), TimeSpan.FromHours(18)));
		var boards = new[] { board1, board2 };

		var ranges = generator.GetOrderedRanges(boards, DateTime.Today).ToList();

		// Should merge overlapping ranges
		ranges.Count.AssertEqual(1);
		ranges[0].range.Min.AssertEqual(TimeSpan.FromHours(9));
		ranges[0].range.Max.AssertEqual(TimeSpan.FromHours(18));
	}

	[TestMethod]
	public void GetOrderedRanges_NonOverlappingRanges_ReturnsBoth()
	{
		var generator = CreateGenerator();
		var board1 = CreateBoard("TEST1", TimeZoneInfo.Utc, (TimeSpan.FromHours(9), TimeSpan.FromHours(12)));
		var board2 = CreateBoard("TEST2", TimeZoneInfo.Utc, (TimeSpan.FromHours(14), TimeSpan.FromHours(18)));
		var boards = new[] { board1, board2 };

		var ranges = generator.GetOrderedRanges(boards, DateTime.Today).ToList();

		ranges.Count.AssertEqual(2);
		ranges[0].range.Min.AssertEqual(TimeSpan.FromHours(9));
		ranges[0].range.Max.AssertEqual(TimeSpan.FromHours(12));
		ranges[1].range.Min.AssertEqual(TimeSpan.FromHours(14));
		ranges[1].range.Max.AssertEqual(TimeSpan.FromHours(18));
	}

	[TestMethod]
	public void GetOrderedRanges_ContainedRange_RemovesInner()
	{
		var generator = CreateGenerator();
		var board1 = CreateBoard("TEST1", TimeZoneInfo.Utc, (TimeSpan.FromHours(9), TimeSpan.FromHours(18)));
		var board2 = CreateBoard("TEST2", TimeZoneInfo.Utc, (TimeSpan.FromHours(10), TimeSpan.FromHours(16)));
		var boards = new[] { board1, board2 };

		var ranges = generator.GetOrderedRanges(boards, DateTime.Today).ToList();

		// Inner range should be removed
		ranges.Count.AssertEqual(1);
		ranges[0].range.Min.AssertEqual(TimeSpan.FromHours(9));
		ranges[0].range.Max.AssertEqual(TimeSpan.FromHours(18));
	}

	[TestMethod]
	public void GetOrderedRanges_NullBoards_ThrowsArgumentNullException()
	{
		var generator = CreateGenerator();

		ThrowsExactly<ArgumentNullException>(() =>
			generator.GetOrderedRanges(null, DateTime.Today).ToList());
	}

	[TestMethod]
	public void GetOrderedRanges_BoardWithoutWorkingTime_ReturnsFullDay()
	{
		var generator = CreateGenerator();
		var board = CreateBoard("TEST", TimeZoneInfo.Utc);
		var boards = new[] { board };

		var ranges = generator.GetOrderedRanges(boards, DateTime.Today).ToList();

		ranges.Count.AssertEqual(1);
		ranges[0].range.Min.AssertEqual(TimeSpan.Zero);
		// LessOneDay is 23:59:59.9999999
	}

	#endregion

	#region GetPostTradeTimeMessages Tests

	[TestMethod]
	public void GetPostTradeTimeMessages_ZeroCount_ReturnsEmpty()
	{
		var generator = CreateGenerator();
		var date = DateTime.Today;
		var lastTime = TimeSpan.FromHours(18);
		var interval = TimeSpan.FromSeconds(1);

		var messages = generator.GetPostTradeTimeMessages(date, lastTime, interval, 0).ToList();

		messages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void GetPostTradeTimeMessages_ValidCount_ReturnsCorrectNumber()
	{
		var generator = CreateGenerator();
		var date = DateTime.Today;
		var lastTime = TimeSpan.FromHours(18);
		var interval = TimeSpan.FromSeconds(1);
		var count = 5;

		var messages = generator.GetPostTradeTimeMessages(date, lastTime, interval, count).ToList();

		messages.Count.AssertEqual(count);
	}

	[TestMethod]
	public void GetPostTradeTimeMessages_CorrectTimes()
	{
		var generator = CreateGenerator();
		var date = DateTime.Today;
		var lastTime = TimeSpan.FromHours(18);
		var interval = TimeSpan.FromMinutes(1);
		var count = 3;

		var messages = generator.GetPostTradeTimeMessages(date, lastTime, interval, count).ToList();

		messages[0].ServerTime.AssertEqual(date + TimeSpan.FromHours(18) + TimeSpan.FromMinutes(1));
		messages[1].ServerTime.AssertEqual(date + TimeSpan.FromHours(18) + TimeSpan.FromMinutes(2));
		messages[2].ServerTime.AssertEqual(date + TimeSpan.FromHours(18) + TimeSpan.FromMinutes(3));
	}

	[TestMethod]
	public void GetPostTradeTimeMessages_StopsAtEndOfDay()
	{
		var generator = CreateGenerator();
		var date = DateTime.Today;
		var lastTime = TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59);
		var interval = TimeSpan.FromMinutes(5);
		var count = 10;

		var messages = generator.GetPostTradeTimeMessages(date, lastTime, interval, count).ToList();

		// Should stop before exceeding day boundary
		(messages.Count < count).AssertTrue();
	}

	[TestMethod]
	public void GetPostTradeTimeMessages_NegativeCount_ThrowsArgumentOutOfRangeException()
	{
		var generator = CreateGenerator();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			generator.GetPostTradeTimeMessages(DateTime.Today, TimeSpan.Zero, TimeSpan.FromSeconds(1), -1).ToList());
	}

	[TestMethod]
	public void GetPostTradeTimeMessages_ZeroInterval_ThrowsArgumentOutOfRangeException()
	{
		var generator = CreateGenerator();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			generator.GetPostTradeTimeMessages(DateTime.Today, TimeSpan.Zero, TimeSpan.Zero, 1).ToList());
	}

	#endregion

	#region GetSimpleTimeLine Tests

	[TestMethod]
	public void GetSimpleTimeLine_EmptyBoards_ReturnsEmpty()
	{
		var generator = CreateGenerator();
		var boards = Array.Empty<BoardMessage>();

		var messages = generator.GetSimpleTimeLine(boards, DateTime.Today, TimeSpan.FromSeconds(1)).ToList();

		messages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void GetSimpleTimeLine_SingleBoard_ReturnsStartAndEndTimes()
	{
		var generator = CreateGenerator();
		var board = CreateBoard("TEST", TimeZoneInfo.Utc, (TimeSpan.FromHours(9), TimeSpan.FromHours(18)));
		var boards = new[] { board };

		var messages = generator.GetSimpleTimeLine(boards, DateTime.Today, TimeSpan.FromSeconds(1)).ToList();

		// Should have at least start and end times
		(messages.Count >= 2).AssertTrue();
		messages[0].ServerTime.TimeOfDay.AssertEqual(TimeSpan.FromHours(9));
		messages[1].ServerTime.TimeOfDay.AssertEqual(TimeSpan.FromHours(18));
	}

	[TestMethod]
	public void GetSimpleTimeLine_NullBoards_ThrowsArgumentNullException()
	{
		var generator = CreateGenerator();

		ThrowsExactly<ArgumentNullException>(() =>
			generator.GetSimpleTimeLine(null, DateTime.Today, TimeSpan.FromSeconds(1)).ToList());
	}

	#endregion

	#region IsTradeDate Tests

	[TestMethod]
	public void IsTradeDate_EmptyBoards_ReturnsFalse()
	{
		var generator = CreateGenerator();
		var boards = Array.Empty<BoardMessage>();

		var result = generator.IsTradeDate(boards, DateTime.Today);

		result.AssertFalse();
	}

	[TestMethod]
	public void IsTradeDate_NullBoards_ThrowsArgumentNullException()
	{
		var generator = CreateGenerator();

		ThrowsExactly<ArgumentNullException>(() =>
			generator.IsTradeDate(null, DateTime.Today));
	}

	#endregion
}
