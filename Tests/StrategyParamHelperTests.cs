namespace StockSharp.Tests;

[TestClass]
public class StrategyParamHelperTests : BaseTestClass
{
	#region GetIterationsCount

	[TestMethod]
	public void GetIterationsCount_IntRange()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 50, 10);

		var count = param.GetIterationsCount();

		AreEqual(5, count); // 10, 20, 30, 40, 50
	}

	[TestMethod]
	public void GetIterationsCount_DecimalRange()
	{
		var param = new StrategyParam<decimal>("test");
		param.SetOptimize(1.0m, 2.0m, 0.5m);

		var count = param.GetIterationsCount();

		AreEqual(3, count); // 1.0, 1.5, 2.0
	}

	[TestMethod]
	public void GetIterationsCount_BoolRange()
	{
		var param = new StrategyParam<bool>("test");
		param.SetOptimize(false, true, default);

		var count = param.GetIterationsCount();

		AreEqual(2, count); // false, true
	}

	[TestMethod]
	public void GetIterationsCount_BoolSameValue()
	{
		var param = new StrategyParam<bool>("test");
		param.SetOptimize(true, true, default);

		var count = param.GetIterationsCount();

		AreEqual(1, count);
	}

	[TestMethod]
	public void GetIterationsCount_TimeSpanRange()
	{
		var param = new StrategyParam<TimeSpan>("test");
		param.SetOptimize(
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(15),
			TimeSpan.FromMinutes(5));

		var count = param.GetIterationsCount();

		AreEqual(3, count); // 5, 10, 15 min
	}

	[TestMethod]
	public void GetIterationsCount_NoOptimizeSettings_Returns1()
	{
		var param = new StrategyParam<int>("test") { Value = 42 };

		var count = param.GetIterationsCount();

		AreEqual(1, count);
	}

	[TestMethod]
	public void GetIterationsCount_CanOptimizeFalse_Returns1()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 50, 10);
		param.CanOptimize = false;

		var count = param.GetIterationsCount();

		AreEqual(1, count);
	}

	#endregion

	#region GetOptimizationValues

	[TestMethod]
	public void GetOptimizationValues_IntRange()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(1, 5, 1);

		var values = param.GetOptimizationValues().Cast<int>().ToArray();

		values.Length.AssertEqual(5);
		values.SequenceEqual([1, 2, 3, 4, 5]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_IntRangeWithStep()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 50, 10);

		var values = param.GetOptimizationValues().Cast<int>().ToArray();

		values.Length.AssertEqual(5);
		values.SequenceEqual([10, 20, 30, 40, 50]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_DecimalRange()
	{
		var param = new StrategyParam<decimal>("test");
		param.SetOptimize(1.0m, 2.0m, 0.5m);

		var values = param.GetOptimizationValues().Cast<decimal>().ToArray();

		values.Length.AssertEqual(3);
		values.SequenceEqual([1.0m, 1.5m, 2.0m]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_BoolRange()
	{
		var param = new StrategyParam<bool>("test");
		param.SetOptimize(false, true, default);

		var values = param.GetOptimizationValues().Cast<bool>().ToArray();

		values.Length.AssertEqual(2);
		values.SequenceEqual([false, true]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_TimeSpanRange()
	{
		var param = new StrategyParam<TimeSpan>("test");
		param.SetOptimize(
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(15),
			TimeSpan.FromMinutes(5));

		var values = param.GetOptimizationValues().Cast<TimeSpan>().ToArray();

		values.Length.AssertEqual(3);
		values.SequenceEqual([
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(10),
			TimeSpan.FromMinutes(15)
		]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_NoSettings_ReturnsCurrentValue()
	{
		var param = new StrategyParam<int>("test") { Value = 42 };

		var values = param.GetOptimizationValues().Cast<int>().ToArray();

		values.Length.AssertEqual(1);
		values.SequenceEqual([42]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_LongRange()
	{
		var param = new StrategyParam<long>("test");
		param.SetOptimize(100L, 500L, 100L);

		var values = param.GetOptimizationValues().Cast<long>().ToArray();

		values.Length.AssertEqual(5);
		values.SequenceEqual([100L, 200L, 300L, 400L, 500L]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_Unit()
	{
		var param = new StrategyParam<Unit>("test");
		param.SetOptimize(
			new Unit(1, UnitTypes.Percent),
			new Unit(3, UnitTypes.Percent),
			new Unit(1, UnitTypes.Percent));

		var values = param.GetOptimizationValues().Cast<Unit>().ToArray();

		AreEqual(3, values.Length);
		AreEqual(1m, values[0].Value);
		AreEqual(2m, values[1].Value);
		AreEqual(3m, values[2].Value);
	}

	#endregion

	#region GetRandom

	[TestMethod]
	public void GetRandom_IntRange()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 100, 10);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsInRange(value, 10, 100);
			AreEqual(0, value % 10); // should be on step boundary
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetRandom_DecimalRange()
	{
		var param = new StrategyParam<decimal>("test");
		param.SetOptimize(1.0m, 5.0m, 0.5m);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsInRange(value, 1.0m, 5.0m);
			// The engine generates from + k*step, so the value must sit on a step boundary.
			IsZero((value - 1.0m) % 0.5m, $"Value {value} is not aligned to step 0.5 from 1.0.");
		}
	}

	[TestMethod]
	public void GetRandom_BoolRange()
	{
		var param = new StrategyParam<bool>("test");
		param.SetOptimize(false, true, default);

		var trueCount = 0;
		var falseCount = 0;

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			if (value) trueCount++;
			else falseCount++;
		}

		// Should have both values
		IsGreater(trueCount, 0);
		IsGreater(falseCount, 0);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetRandom_BoolSameValue_StaysInRange()
	{
		// Degenerate range from=to=true. GetIterationsCount reports 1 for this case, so the only
		// admissible random value is 'true'. The engine, however, calls RandomGen.GetBool() and
		// ignores the from/to bounds, so it returns false ~half the time. Sampling many times makes a
		// false pass astronomically unlikely while keeping the assertion on the correct contract.
		var param = new StrategyParam<bool>("test");
		param.SetOptimize(true, true, default);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsTrue(value, "Random value for a true..true range must always be true.");
		}
	}

	[TestMethod]
	public void GetRandom_ThrowsWhenCanOptimizeFalse()
	{
		var param = new StrategyParam<int>("test") { Value = 42, CanOptimize = false };

		Throws<InvalidOperationException>(() => param.GetRandom());
	}

	[TestMethod]
	public void GetRandom_ThrowsWhenNoRange()
	{
        var param = new StrategyParam<int>("test")
        {
            CanOptimize = true
        };

        Throws<InvalidOperationException>(() => param.GetRandom());
	}

	#endregion

	#region GetRandomValues

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetRandomValues_ReturnsRequestedCount()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 1000, 10);

		var values = param.GetRandomValues(10);

		AreEqual(10, values.Count);

		// GetRandomValues returns a HashSet, so uniqueness is structural and proves nothing.
		// Instead verify every produced value honors the requested range and step grid (from + k*step).
		foreach (var v in values)
		{
			IsInRange(v, 10, 1000);
			AreEqual(0, (v - 10) % 10, $"Value {v} is not aligned to step 10 from 10.");
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetRandomValues_AllInRange()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 100, 5);

		var values = param.GetRandomValues(20);

		foreach (var v in values)
		{
			IsInRange(v, 10, 100);
			// Random values must land on the step grid (10 + k*5), not just inside the range.
			AreEqual(0, (v - 10) % 5, $"Value {v} is not aligned to step 5 from 10.");
		}
	}

	[TestMethod]
	public void GetRandomValues_ThrowsOnZeroCount()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(1, 100, 1);

		Throws<ArgumentOutOfRangeException>(() => param.GetRandomValues(0));
	}

	#endregion

	#region CanOptimize Type Check

	[TestMethod]
	public void CanOptimize_NumericTypes()
	{
		IsTrue(typeof(int).CanOptimize());
		IsTrue(typeof(long).CanOptimize());
		IsTrue(typeof(decimal).CanOptimize());
		IsTrue(typeof(double).CanOptimize());
		IsTrue(typeof(float).CanOptimize());
	}

	[TestMethod]
	public void CanOptimize_SpecialTypes()
	{
		IsTrue(typeof(bool).CanOptimize());
		IsTrue(typeof(Unit).CanOptimize());
		IsTrue(typeof(TimeSpan).CanOptimize());
		IsTrue(typeof(DataType).CanOptimize());
	}

	[TestMethod]
	public void CanOptimize_NullableNumeric()
	{
		IsTrue(typeof(int?).CanOptimize());
		IsTrue(typeof(decimal?).CanOptimize());
		IsTrue(typeof(TimeSpan?).CanOptimize());
	}

	[TestMethod]
	public void CanOptimize_NotSupported()
	{
		IsFalse(typeof(string).CanOptimize());
		IsFalse(typeof(DateTime).CanOptimize());
		IsFalse(typeof(object).CanOptimize());
	}

	[TestMethod]
	public void CanOptimize_EnumsNotSupported()
	{
		IsFalse(typeof(DayOfWeek).CanOptimize());
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void GetOptimizationValues_SingleValue()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(5, 5, 1);

		var values = param.GetOptimizationValues().Cast<int>().ToArray();

		values.Length.AssertEqual(1);
		values.SequenceEqual([5]).AssertTrue();
	}

	[TestMethod]
	public void GetIterationsCount_LargeRange()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(0, 1000, 1);

		var count = param.GetIterationsCount();

		AreEqual(1001, count);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetIterationsCount_UnalignedDecimalRange_MatchesValuesCount()
	{
		// Unaligned range: the last full step (2.5) overshoots 'to' (2.1), so the real values are
		// 1.0, 1.5, 2.0 -> 3. GetIterationsCount uses Ceiling((to-from+step)/step) = ceil(3.2) = 4,
		// over-reporting by one. ToBruteForce trusts GetIterationsCount for totalCount while iterating
		// GetOptimizationValues, so the two MUST agree. Bounded decimal range -> safe to enumerate.
		var param = new StrategyParam<decimal>("test");
		param.SetOptimize(1.0m, 2.1m, 0.5m);

		var count = param.GetIterationsCount();
		var values = param.GetOptimizationValues().Cast<decimal>().ToArray();

		AreEqual(values.Length, count,
			$"Iterations count must match the produced values [{values.Select(v => v.ToString(CultureInfo.InvariantCulture)).JoinComma()}].");
	}

	[TestMethod]
	public void GetOptimizationValues_LazyEvaluation()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(1, 1000000, 1);

		// Should not throw OOM - lazy evaluation
		var enumerable = param.GetOptimizationValues();

		// Take only first 5
		var first5 = enumerable.Take(5).Cast<int>().ToArray();

		first5.Length.AssertEqual(5);
		first5.SequenceEqual([1, 2, 3, 4, 5]).AssertTrue();
	}

	#endregion

	#region Float/Double Ranges

	[TestMethod]
	public void GetIterationsCount_FloatRange()
	{
		var param = new StrategyParam<float>("test");
		param.SetOptimize(1.0f, 2.0f, 0.5f);

		var count = param.GetIterationsCount();

		AreEqual(3, count); // 1.0, 1.5, 2.0
	}

	[TestMethod]
	public void GetIterationsCount_DoubleRange()
	{
		var param = new StrategyParam<double>("test");
		param.SetOptimize(0.1, 0.5, 0.1);

		var count = param.GetIterationsCount();

		AreEqual(5, count); // 0.1, 0.2, 0.3, 0.4, 0.5
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetRandom_FloatRange()
	{
		var param = new StrategyParam<float>("test");
		param.SetOptimize(1.0f, 10.0f, 0.5f);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsInRange(value, 1.0f, 10.0f);

			// The engine generates from + k*step. With from=1.0 and step=0.5 (both exact in binary),
			// the value must land exactly on the step grid.
			var k = (float)Math.Round((value - 1.0f) / 0.5f);
			AreEqual(1.0f + k * 0.5f, value, 1e-4f, $"Value {value} is not aligned to step 0.5 from 1.0.");
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetRandom_DoubleRange()
	{
		var param = new StrategyParam<double>("test");
		param.SetOptimize(0.0, 1.0, 0.1);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsInRange(value, 0.0, 1.0);

			// The engine generates from + k*step. 0.1 is not exactly representable, so allow a small
			// tolerance, but the value must still sit on a 0.1 grid point (from=0.0).
			var k = Math.Round(value / 0.1);
			AreEqual(k * 0.1, value, 1e-9, $"Value {value} is not aligned to step 0.1 from 0.0.");
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetOptimizationValues_FloatRange()
	{
		var param = new StrategyParam<float>("test");
		param.SetOptimize(1.0f, 2.0f, 0.5f);

		// GetIterationsCount returns 3 for this range (decimal branch). GetOptimizationValues must
		// agree and yield the fractional grid 1.0, 1.5, 2.0. The current engine routes float through
		// the IsPrimitive() branch and truncates step (0.5f -> 0) producing a degenerate sequence,
		// so this enumeration is bounded with Take to never hang the runner.
		var values = param.GetOptimizationValues().Take(3).Cast<float>().ToArray();

		IsTrue(values.SequenceEqual([1.0f, 1.5f, 2.0f]),
			$"Expected [1.0, 1.5, 2.0] but got [{values.Select(v => v.ToString(CultureInfo.InvariantCulture)).JoinComma()}].");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void GetOptimizationValues_DoubleRange()
	{
		var param = new StrategyParam<double>("test");
		param.SetOptimize(0.1, 0.5, 0.1);

		// GetIterationsCount returns 5 for this range (decimal branch). GetOptimizationValues must
		// agree and yield 0.1, 0.2, 0.3, 0.4, 0.5. The current engine routes double through the
		// IsPrimitive() branch where 0.1 -> 0 and step -> 0, which would loop forever, so the
		// enumeration is bounded with Take to never hang the runner.
		var values = param.GetOptimizationValues().Take(5).Cast<double>().ToArray();

		var expected = new[] { 0.1, 0.2, 0.3, 0.4, 0.5 };

		AreEqual(expected.Length, values.Length,
			$"Got [{values.Select(v => v.ToString(CultureInfo.InvariantCulture)).JoinComma()}].");

		for (var i = 0; i < expected.Length; i++)
			AreEqual(expected[i], values[i], 1e-9, $"Value at index {i} mismatch.");
	}

	#endregion

	#region DataType Explicit Values

	[TestMethod]
	public void CanOptimize_DataType()
	{
		IsTrue(typeof(DataType).CanOptimize());
	}

	[TestMethod]
	public void GetIterationsCount_DataTypeWithExplicitValues()
	{
		var param = new StrategyParam<DataType>("test");
		param.SetOptimizeValues([DataType.Ticks, DataType.Level1, DataType.MarketDepth]);

		var count = param.GetIterationsCount();

		AreEqual(3, count);
	}

	[TestMethod]
	public void GetOptimizationValues_DataTypeExplicit()
	{
		var param = new StrategyParam<DataType>("test");
		param.SetOptimizeValues([DataType.Ticks, DataType.Level1]);

		var values = param.GetOptimizationValues().Cast<DataType>().ToArray();

		AreEqual(2, values.Length);
		AreEqual(DataType.Ticks, values[0]);
		AreEqual(DataType.Level1, values[1]);
	}

	[TestMethod]
	public void GetRandom_DataTypeExplicit()
	{
		var param = new StrategyParam<DataType>("test");
		var expected = new[] { DataType.Ticks, DataType.Level1, DataType.MarketDepth };
		param.SetOptimizeValues(expected);

		for (var i = 0; i < 50; i++)
		{
			var value = param.GetRandom();
			IsTrue(expected.Contains(value));
		}
	}

	[TestMethod]
	public void GetIterationsCount_NoExplicitValues_Returns1()
	{
		var param = new StrategyParam<DataType>("test");
		param.CanOptimize = true;
		// No OptimizeValues, no From/To - should return 1

		var count = param.GetIterationsCount();

		AreEqual(1, count);
	}

	#endregion

	#region Security Explicit Values

	[TestMethod]
	public void CanOptimize_SecurityNotSupportedByDefault()
	{
		// Security doesn't support CanOptimize by default (not numeric, bool, Unit, TimeSpan, DataType)
		IsFalse(typeof(Security).CanOptimize());
	}

	[TestMethod]
	public void GetIterationsCount_SecurityWithExplicitValues()
	{
		var sec1 = new Security { Id = "AAPL@NASDAQ" };
		var sec2 = new Security { Id = "MSFT@NASDAQ" };
		var sec3 = new Security { Id = "GOOG@NASDAQ" };

		var param = new StrategyParam<Security>("test") { CanOptimize = true };
		param.SetOptimizeValues([sec1, sec2, sec3]);

		var count = param.GetIterationsCount();

		AreEqual(3, count);
	}

	[TestMethod]
	public void GetOptimizationValues_SecurityExplicit()
	{
		var sec1 = new Security { Id = "AAPL@NASDAQ" };
		var sec2 = new Security { Id = "MSFT@NASDAQ" };

		var param = new StrategyParam<Security>("test") { CanOptimize = true };
		param.SetOptimizeValues([sec1, sec2]);

		var values = param.GetOptimizationValues().Cast<Security>().ToArray();

		AreEqual(2, values.Length);
		AreEqual(sec1.Id, values[0].Id);
		AreEqual(sec2.Id, values[1].Id);
	}

	[TestMethod]
	public void GetRandom_SecurityExplicit()
	{
		var sec1 = new Security { Id = "AAPL@NASDAQ" };
		var sec2 = new Security { Id = "MSFT@NASDAQ" };
		var sec3 = new Security { Id = "GOOG@NASDAQ" };
		var expected = new[] { sec1, sec2, sec3 };

		var param = new StrategyParam<Security>("test") { CanOptimize = true };
		param.SetOptimizeValues(expected);

		for (var i = 0; i < 50; i++)
		{
			var value = param.GetRandom();
			IsTrue(expected.Contains(value));
		}
	}

	[TestMethod]
	public void GetIterationsCount_SecurityNoValues_Returns1()
	{
		var param = new StrategyParam<Security>("test") { CanOptimize = true };
		// No OptimizeValues set - should return 1

		var count = param.GetIterationsCount();

		AreEqual(1, count);
	}

	#endregion

	#region Inverted Range (from > to)

	[TestMethod]
	public void SetOptimize_IntRange_FromGreaterThanTo_ShouldThrow()
	{
		var param = new StrategyParam<int>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(100, 10, 10)); // from > to

		IsTrue(ex.Message.Contains("from") && ex.Message.Contains("to"));
	}

	[TestMethod]
	public void SetOptimize_DecimalRange_FromGreaterThanTo_ShouldThrow()
	{
		var param = new StrategyParam<decimal>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(10.0m, 1.0m, 0.5m)); // from > to

		IsTrue(ex.Message.Contains("from") && ex.Message.Contains("to"));
	}

	[TestMethod]
	public void SetOptimize_LongRange_FromGreaterThanTo_ShouldThrow()
	{
		var param = new StrategyParam<long>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(1000L, 100L, 100L)); // from > to

		IsTrue(ex.Message.Contains("from") && ex.Message.Contains("to"));
	}

	[TestMethod]
	public void SetOptimize_DoubleRange_FromGreaterThanTo_ShouldThrow()
	{
		var param = new StrategyParam<double>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(10.0, 1.0, 0.5)); // from > to

		IsTrue(ex.Message.Contains("from") && ex.Message.Contains("to"));
	}

	[TestMethod]
	public void SetOptimize_FloatRange_FromGreaterThanTo_ShouldThrow()
	{
		var param = new StrategyParam<float>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(10.0f, 1.0f, 0.5f)); // from > to

		IsTrue(ex.Message.Contains("from") && ex.Message.Contains("to"));
	}

	[TestMethod]
	public void SetOptimize_TimeSpanRange_FromGreaterThanTo_ShouldThrow()
	{
		var param = new StrategyParam<TimeSpan>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(
				TimeSpan.FromMinutes(60),
				TimeSpan.FromMinutes(5),
				TimeSpan.FromMinutes(5))); // from > to

		IsTrue(ex.Message.Contains("from") && ex.Message.Contains("to"));
	}

	#endregion

	#region Step Larger Than Range

	[TestMethod]
	public void GetRandom_IntRange_StepLargerThanRange_ShouldReturnValidValue()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 15, 100); // step (100) > range (5)

		for (var i = 0; i < 10; i++)
		{
			var value = param.GetRandom();
			// With step > range, should return from value or handle gracefully
			IsTrue(value >= 10 && value <= 15, $"Value {value} is outside expected range [10, 15]");
		}
	}

	[TestMethod]
	public void GetRandom_DecimalRange_StepLargerThanRange_ShouldReturnValidValue()
	{
		var param = new StrategyParam<decimal>("test");
		param.SetOptimize(1.0m, 1.5m, 10.0m); // step (10) > range (0.5)

		for (var i = 0; i < 10; i++)
		{
			var value = param.GetRandom();
			IsTrue(value >= 1.0m && value <= 1.5m, $"Value {value} is outside expected range [1.0, 1.5]");
		}
	}

	[TestMethod]
	public void GetIterationsCount_StepLargerThanRange_ShouldReturn1()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 15, 100); // step (100) > range (5)

		var count = param.GetIterationsCount();

		// With step > range, only 1 iteration possible (the from value)
		AreEqual(1, count);
	}

	#endregion

	#region Zero and Negative Step

	[TestMethod]
	public void SetOptimize_IntRange_ZeroStep_ShouldThrow()
	{
		var param = new StrategyParam<int>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(10, 100, 0)); // zero step

		IsTrue(ex.Message.Contains("step"));
	}

	[TestMethod]
	public void SetOptimize_IntRange_NegativeStep_ShouldThrow()
	{
		var param = new StrategyParam<int>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(10, 100, -10)); // negative step

		IsTrue(ex.Message.Contains("step"));
	}

	[TestMethod]
	public void SetOptimize_DecimalRange_ZeroStep_ShouldThrow()
	{
		var param = new StrategyParam<decimal>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(1.0m, 10.0m, 0m)); // zero step

		IsTrue(ex.Message.Contains("step"));
	}

	[TestMethod]
	public void SetOptimize_TimeSpanRange_ZeroStep_ShouldThrow()
	{
		var param = new StrategyParam<TimeSpan>("test");

		var ex = ThrowsExactly<ArgumentOutOfRangeException>(() =>
			param.SetOptimize(
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMinutes(10),
				TimeSpan.Zero)); // zero step

		IsTrue(ex.Message.Contains("step"));
	}

	[TestMethod]
	public void SetOptimize_BoolRange_DefaultStep_ShouldNotThrow()
	{
		var param = new StrategyParam<bool>("test");

		// Bool type should allow default step (step doesn't apply to bool)
		param.SetOptimize(false, true, default);

		// Should work without exception and store the exact range bounds.
		AreEqual(false, (bool)param.OptimizeFrom);
		AreEqual(true, (bool)param.OptimizeTo);
	}

	#endregion
}
