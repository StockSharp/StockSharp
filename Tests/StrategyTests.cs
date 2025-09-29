namespace StockSharp.Tests;

[TestClass]
public class StrategyParamTests
{
	private static void AssertInvalid(Action action)
		=> Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);

	// --- Helper methods --------------------------------------------------
	private static void AssertGreaterThanZero<T>(T valid, T zero, T negative)
	{
		var p = new StrategyParam<T>("p", valid).SetGreaterThanZero();
		p.Value.AssertEqual(valid);
		AssertInvalid(() => p.Value = zero);
		AssertInvalid(() => p.Value = negative);
	}

	private static void AssertNullOrMoreZero<T>(T zero, T positive, T negative)
		where T : struct
	{
		// Semantics: null or > 0 (zero is NOT allowed)
		var p = new StrategyParam<T?>("p").SetNullOrMoreZero();
		p.Value = null;      // allowed
		p.Value = positive;  // allowed (>0)
		AssertInvalid(() => p.Value = zero);     // zero must be invalid
		AssertInvalid(() => p.Value = negative); // negative invalid
	}

	private static void AssertNullOrNotNegative<T>(T zero, T positive, T negative)
		where T : struct
	{
		var p = new StrategyParam<T?>("p").SetNullOrNotNegative();
		p.Value = null; // allowed
		p.Value = zero; // allowed (>=0)
		p.Value = positive; // allowed
		AssertInvalid(() => p.Value = negative);
	}

	private static void AssertNotNegative<T>(T zero, T positive, T negative)
	{
		var p = new StrategyParam<T>("p", zero).SetNotNegative();
		p.Value = zero;
		p.Value = positive;
		AssertInvalid(() => p.Value = negative);
	}

	private static void AssertPositive<T>(T one, T anotherPositive, T zero, T negative)
	{
		var p = new StrategyParam<T>("p", one).SetPositive();
		p.Value = one;
		p.Value = anotherPositive;
		AssertInvalid(() => p.Value = zero);
		AssertInvalid(() => p.Value = negative);
	}

	private static void AssertRange<T>(T min, T mid, T max, T below, T above)
	{
		var p = new StrategyParam<T>("p", min).SetRange(min, max);
		p.Value = min;
		p.Value = mid;
		p.Value = max;
		AssertInvalid(() => p.Value = below);
		AssertInvalid(() => p.Value = above);
	}

	// --- Tests per validator across all supported primitive types --------

	[TestMethod]
	public void GreaterThanZero_All()
	{
		AssertGreaterThanZero(1, 0, -1);
		AssertGreaterThanZero(1L, 0L, -1L);
		AssertGreaterThanZero(1m, 0m, -1m);
		AssertGreaterThanZero(1.0, 0.0, -1.0);
		AssertGreaterThanZero(1f, 0f, -1f);
		AssertGreaterThanZero(TimeSpan.FromTicks(1), TimeSpan.Zero, TimeSpan.FromTicks(-1));
	}

	[TestMethod]
	public void NullOrMoreZero_All()
	{
		AssertNullOrMoreZero(0, 10, -1);
		AssertNullOrMoreZero(0L, 10L, -1L);
		AssertNullOrMoreZero(0m, 10m, -1m);
		AssertNullOrMoreZero(0.0, 10.0, -1.0);
		AssertNullOrMoreZero(0f, 10f, -1f);
		AssertNullOrMoreZero(TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
	}

	[TestMethod]
	public void NullOrNotNegative_All()
	{
		AssertNullOrNotNegative(0, 5, -1);
		AssertNullOrNotNegative(0L, 5L, -1L);
		AssertNullOrNotNegative(0m, 5m, -1m);
		AssertNullOrNotNegative(0.0, 5.0, -1.0);
		AssertNullOrNotNegative(0f, 5f, -1f);
		AssertNullOrNotNegative(TimeSpan.Zero, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(-1));
	}

	[TestMethod]
	public void NotNegative_All()
	{
		AssertNotNegative(0, 7, -1);
		AssertNotNegative(0L, 7L, -1L);
		AssertNotNegative(0m, 7m, -1m);
		AssertNotNegative(0.0, 7.0, -1.0);
		AssertNotNegative(0f, 7f, -1f);
		AssertNotNegative(TimeSpan.Zero, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(-1));
	}

	[TestMethod]
	public void Positive_All()
	{
		AssertPositive(1, 2, 0, -1);
		AssertPositive(1L, 2L, 0L, -1L);
		AssertPositive(1m, 2m, 0m, -1m);
		AssertPositive(1.0, 2.0, 0.0, -1.0);
		AssertPositive(1f, 2f, 0f, -1f);
		AssertPositive(TimeSpan.FromTicks(1), TimeSpan.FromTicks(2), TimeSpan.Zero, TimeSpan.FromTicks(-1));
	}

	[TestMethod]
	public void Range_All()
	{
		AssertRange(10, 15, 20, 9, 21);
		AssertRange(10L, 15L, 20L, 9L, 21L);
		AssertRange(1.5m, 2.0m, 2.5m, 1.4m, 2.6m);
		AssertRange(1.5, 2.0, 2.5, 1.4, 2.6);
		AssertRange(1.5f, 2.0f, 2.5f, 1.4f, 2.6f);
		AssertRange(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1500), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(3));
	}

	[TestMethod]
	public void Required_String_And_NullableInt()
	{
		var sp = new StrategyParam<string>("s").SetRequired();
		sp.Value = "abc";
		sp.Value.AssertEqual("abc");
		AssertInvalid(() => sp.Value = null);

		var ip = new StrategyParam<int?>("i").SetRequired();
		ip.Value = 5;
		ip.Value.AssertEqual(5);
		AssertInvalid(() => ip.Value = null);
	}

	// --- SetStep tests ---------------------------------------------------

	[TestMethod]
	public void Step_Int()
	{
		var p = new StrategyParam<int>("p", 0).SetStep(5); // base 0 step 5
		p.Value = 0;
		p.Value = 5;
		p.Value = 10;
		AssertInvalid(() => p.Value = 3); // not multiple
		AssertInvalid(() => p.Value = -5); // negative diff from base

		// base offset
		var p2 = new StrategyParam<int>("p2", 10).SetStep(5, 10); // allowed 10,15,20,...
		p2.Value = 10;
		p2.Value = 15;
		AssertInvalid(() => p2.Value = 11);
	}

	[TestMethod]
	public void Step_Int_InvalidCurrentOnSet()
	{
		// current value 3, step 5 from base 0 -> should throw immediately
		AssertInvalid(() => new StrategyParam<int>("p", 3).SetStep(5));
	}

	[TestMethod]
	public void Step_Decimal()
	{
		var p = new StrategyParam<decimal>("pd", 0m).SetStep(0.25m);
		p.Value = 0.25m;
		p.Value = 1.0m;
		p.Value = 2.5m;
		AssertInvalid(() => p.Value = 0.3m);
	}

	[TestMethod]
	public void Step_Decimal_Base()
	{
		var p = new StrategyParam<decimal>("pd2", 0.5m).SetStep(0.25m, 0.5m); // 0.5 + n*0.25
		p.Value = 0.5m;
		p.Value = 0.75m;
		p.Value = 1.0m;
		AssertInvalid(() => p.Value = 0.6m);
	}

	[TestMethod]
	public void Step_Double()
	{
		var p = new StrategyParam<double>("pdouble", 0.0).SetStep(0.1);
		p.Value = 0.0;
		p.Value = 0.2;
		p.Value = 1.3;
		AssertInvalid(() => p.Value = 0.25); // not multiple of 0.1 adequately (will fail precise check)
	}

	[TestMethod]
	public void Step_TimeSpan()
	{
		var step = TimeSpan.FromMinutes(5);
		var p = new StrategyParam<TimeSpan>("pts", TimeSpan.Zero).SetStep(step);
		p.Value = TimeSpan.FromMinutes(0);
		p.Value = TimeSpan.FromMinutes(5);
		p.Value = TimeSpan.FromMinutes(15);
		AssertInvalid(() => p.Value = TimeSpan.FromMinutes(3));
	}

	[TestMethod]
	public void Step_InvalidStep()
	{
		AssertInvalid(() => new StrategyParam<int>("p", 0).SetStep(0));
		AssertInvalid(() => new StrategyParam<long>("p", 0).SetStep(-1));
		AssertInvalid(() => new StrategyParam<decimal>("p", 0m).SetStep(0m));
		AssertInvalid(() => new StrategyParam<double>("p", 0.0).SetStep(0.0));
		AssertInvalid(() => new StrategyParam<TimeSpan>("p", TimeSpan.Zero).SetStep(TimeSpan.Zero));
	}
}
