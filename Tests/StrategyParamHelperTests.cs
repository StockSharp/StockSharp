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

		values.SequenceEqual([1, 2, 3, 4, 5]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_IntRangeWithStep()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 50, 10);

		var values = param.GetOptimizationValues().Cast<int>().ToArray();

		values.SequenceEqual([10, 20, 30, 40, 50]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_DecimalRange()
	{
		var param = new StrategyParam<decimal>("test");
		param.SetOptimize(1.0m, 2.0m, 0.5m);

		var values = param.GetOptimizationValues().Cast<decimal>().ToArray();

		values.SequenceEqual([1.0m, 1.5m, 2.0m]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_BoolRange()
	{
		var param = new StrategyParam<bool>("test");
		param.SetOptimize(false, true, default);

		var values = param.GetOptimizationValues().Cast<bool>().ToArray();

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

		values.SequenceEqual([42]).AssertTrue();
	}

	[TestMethod]
	public void GetOptimizationValues_LongRange()
	{
		var param = new StrategyParam<long>("test");
		param.SetOptimize(100L, 500L, 100L);

		var values = param.GetOptimizationValues().Cast<long>().ToArray();

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
	public void GetRandom_DecimalRange()
	{
		var param = new StrategyParam<decimal>("test");
		param.SetOptimize(1.0m, 5.0m, 0.5m);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsInRange(value, 1.0m, 5.0m);
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
	public void GetRandomValues_ReturnsRequestedCount()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(1, 1000, 1);

		var values = param.GetRandomValues(10);

		AreEqual(10, values.Count);
		AllItemsAreUnique(values.ToArray());
	}

	[TestMethod]
	public void GetRandomValues_AllInRange()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(10, 100, 5);

		var values = param.GetRandomValues(20);

		foreach (var v in values)
		{
			IsInRange(v, 10, 100);
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
	public void GetOptimizationValues_LazyEvaluation()
	{
		var param = new StrategyParam<int>("test");
		param.SetOptimize(1, 1000000, 1);

		// Should not throw OOM - lazy evaluation
		var enumerable = param.GetOptimizationValues();

		// Take only first 5
		var first5 = enumerable.Take(5).Cast<int>().ToArray();

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
	public void GetRandom_FloatRange()
	{
		var param = new StrategyParam<float>("test");
		param.SetOptimize(1.0f, 10.0f, 0.5f);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsInRange(value, 1.0f, 10.0f);
		}
	}

	[TestMethod]
	public void GetRandom_DoubleRange()
	{
		var param = new StrategyParam<double>("test");
		param.SetOptimize(0.0, 1.0, 0.1);

		for (var i = 0; i < 100; i++)
		{
			var value = param.GetRandom();
			IsInRange(value, 0.0, 1.0);
		}
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
}
