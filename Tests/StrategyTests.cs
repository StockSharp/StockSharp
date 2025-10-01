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
		var p = new StrategyParam<T?>("p").SetNullOrMoreZero();
		p.Value = null;
		p.Value = positive;
		AssertInvalid(() => p.Value = zero);
		AssertInvalid(() => p.Value = negative);
	}

	private static void AssertNullOrNotNegative<T>(T zero, T positive, T negative)
		where T : struct
	{
		var p = new StrategyParam<T?>("p").SetNullOrNotNegative();
		p.Value = null;
		p.Value = zero;
		p.Value = positive;
		AssertInvalid(() => p.Value = negative);
	}

	private static void AssertNotNegative<T>(T zero, T positive, T negative)
	{
		var p = new StrategyParam<T>("p", zero).SetNotNegative();
		p.Value = zero;
		p.Value = positive;
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

	[TestMethod]
	public void GreaterThanZero_All()
	{
		AssertGreaterThanZero(1, 0, -1);
		AssertGreaterThanZero(1L, 0L, -1L);
		AssertGreaterThanZero(1m, 0m, -1m);
		AssertGreaterThanZero(1.0, 0.0, -1.0);
		AssertGreaterThanZero(1f, 0f, -1f);
		AssertGreaterThanZero(TimeSpan.FromTicks(1), TimeSpan.Zero, TimeSpan.FromTicks(-1));
		// Unit (will fail until Unit support is added to SetGreaterThanZero)
		AssertGreaterThanZero(new Unit(1m, UnitTypes.Absolute), new Unit(0m, UnitTypes.Absolute), new Unit(-1m, UnitTypes.Absolute));
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

		// Unit: emulate same semantics manually (null or >0). Will fail until SetNullOrMoreZero supports Unit.
		var u_zero = new Unit(0m, UnitTypes.Absolute);
		var u_pos = new Unit(5m, UnitTypes.Absolute);
		var u_neg = new Unit(-1m, UnitTypes.Absolute);
		var pUnit = new StrategyParam<Unit>("u_null_gt0");
		pUnit.SetNullOrMoreZero(); // expected future support
		pUnit.Value = null;
		pUnit.Value = u_pos;
		AssertInvalid(() => pUnit.Value = u_zero);
		AssertInvalid(() => pUnit.Value = u_neg);
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

		// Unit: null or >=0. Will fail until SetNullOrNotNegative supports Unit.
		var u_zero = new Unit(0m, UnitTypes.Absolute);
		var u_pos = new Unit(3m, UnitTypes.Absolute);
		var u_neg = new Unit(-2m, UnitTypes.Absolute);
		var pUnit = new StrategyParam<Unit>("u_null_ge0");
		pUnit.SetNullOrNotNegative();
		pUnit.Value = null;
		pUnit.Value = u_zero;
		pUnit.Value = u_pos;
		AssertInvalid(() => pUnit.Value = u_neg);
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
		AssertNotNegative(new Unit(0m, UnitTypes.Absolute), new Unit(10m, UnitTypes.Absolute), new Unit(-1m, UnitTypes.Absolute));
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
		// Unit range not added yet (RangeAttribute does not support Unit) – can be extended later.
	}

	[TestMethod]
	public void Required_String()
	{
		var sp = new StrategyParam<string>("s").SetRequired();
		sp.Value = "abc";
		sp.Value.AssertEqual("abc");
		AssertInvalid(() => sp.Value = null);
		AssertInvalid(() => sp.Value = string.Empty);
	}

	[TestMethod]
	public void Required_NullableInt()
	{
		var ip = new StrategyParam<int?>("i").SetRequired();
		ip.Value = 5;
		ip.Value.AssertEqual(5);
		AssertInvalid(() => ip.Value = null);
		ip.Value = 0;
		ip.Value.AssertEqual(0);
	}

	[TestMethod]
	public void Step_Int()
	{
		var p = new StrategyParam<int>("p", 0).SetStep(5); // base 0, step 5
		p.Value = 0;
		p.Value = 5;
		p.Value = 10;
		AssertInvalid(() => p.Value = 3); // not multiple
		p.Value = -5;
		AssertInvalid(() => p.Value = -4); // not multiple

		// base offset
		var p2 = new StrategyParam<int>("p2", 10).SetStep(5, 10); // allowed 10,15,20,...
		p2.Value = 10;
		p2.Value = 15;
		AssertInvalid(() => p2.Value = 11);
		p.Value = -5;
		AssertInvalid(() => p2.Value = -11);
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
		AssertInvalid(() => p.Value = 0.25); // precision mismatch
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

	[TestMethod]
	public void Step_NegativeBase()
	{
		var p = new StrategyParam<int>("p", -10).SetStep(5, -10);
		p.Value = -10;
		p.Value = -5;
		p.Value = 0;
		AssertInvalid(() => p.Value = -6);
		p.Value = -15;
	}

	[TestMethod]
	public void Step_Reassign()
	{
		var p = new StrategyParam<int>("p", 0).SetStep(5);
		p.Value = 5; // valid for step 5
		AssertInvalid(() => p.Value = 4);
		p.Value = 0;
		p.SetStep(2); // overwrite with new step specification
		p.Value = 4; // valid for step 2
		AssertInvalid(() => p.Value = 3);
	}

	[TestMethod]
	public void Step_WithRange()
	{
		var p = new StrategyParam<int>("p", 0).SetStep(5).SetRange(0, 30);
		p.Value = 25; // valid
		p.Value = 30; // valid (multiple & in range)
		AssertInvalid(() => p.Value = 27); // in range but not multiple
		AssertInvalid(() => p.Value = 35); // out of range
	}

	[TestMethod]
	public void Step_SameValue_NoChange()
	{
		var p = new StrategyParam<int>("p", 10).SetStep(5);
		p.Value = 10; // same value
		p.Value = 15; // valid next
	}

	[TestMethod]
	public void Step_UnsupportedType()
	{
		var p = new StrategyParam<string>("p");
		Assert.ThrowsExactly<NotSupportedException>(() => p.SetStep("x", "y"));
	}

	[TestMethod]
	public void Step_Double_ValidPrecision()
	{
		var p = new StrategyParam<double>("p", 0.0).SetStep(0.1);
		p.Value = 0.3;
		p.Value = 1.0;
		p.Value = 2.5;
	}

	[TestMethod]
	public void Step_Decimal_ValidPrecision()
	{
		var p = new StrategyParam<decimal>("p", 0m).SetStep(0.1m);
		p.Value = 0.3m;
		p.Value = 1.2m;
		p.Value = 5.0m;
	}

	[TestMethod]
	public void Step_Reassign_Incompatible()
	{
		var p = new StrategyParam<int>("p", 10).SetStep(5);
		AssertInvalid(() => p.SetStep(6));
	}

	[TestMethod]
	public void Step_UnsupportedType_Bool()
	{
		var p = new StrategyParam<bool>("p", true);
		Assert.ThrowsExactly<NotSupportedException>(() => p.SetStep(true, false));
	}

	[TestMethod]
	public void Step_SaveLoad_NoStep()
	{
		var p = new StrategyParam<int>("p", 3);
		var storage = new SettingsStorage();
		p.Save(storage);

		var p2 = new StrategyParam<int>("p");
		p2.Load(storage);
		p2.Value = 7; // any value accepted (no step restriction)
	}

	[TestMethod]
	public void Range_Then_Step()
	{
		var p = new StrategyParam<int>("p", 10).SetRange(0, 100).SetStep(5);
		p.Value = 15;
		AssertInvalid(() => p.Value = 16);
	}

	[TestMethod]
	public void Step_Then_Range()
	{
		var p = new StrategyParam<int>("p", 0).SetStep(5).SetRange(0, 25);
		p.Value = 20;
		AssertInvalid(() => p.Value = 30);
	}

	[TestMethod]
	public void Required_String_SetStep_Unsupported()
	{
		var p = new StrategyParam<string>("p").SetRequired();
		Assert.ThrowsExactly<NotSupportedException>(() => p.SetStep("a", "b"));
	}

	[TestMethod]
	public void Nullable_ToString_NoException()
	{
		var p = new StrategyParam<int?>("p");
		var s = p.ToString();
		s.Contains('p').AssertTrue();
	}

	[TestMethod]
	public void Step_NullableInt_NullAllowed()
	{
		var p = new StrategyParam<int?>("p", null).SetStep(5); // step applies only if value not null
		p.Value = null; // allowed
		p.Value = 0;    // multiple (base 0)
		p.Value = 10;   // multiple
		AssertInvalid(() => p.Value = 3); // not multiple
	}

	[TestMethod]
	public void Step_NullableInt_Required()
	{
		var p = new StrategyParam<int?>("p", 10).SetRequired().SetStep(5);
		p.Value = 15; // valid
		AssertInvalid(() => p.Value = null); // required
		AssertInvalid(() => p.Value = 16);   // not multiple
	}

	[TestMethod]
	public void Step_NullableInt_SetAfterNullValue()
	{
		var p = new StrategyParam<int?>("p", null);
		p.SetStep(5); // current null okay
		p.Value = 5;
		AssertInvalid(() => p.Value = 4);
	}

	[TestMethod]
	public void Step_Unit()
	{
		var uBase = new Unit(10m, UnitTypes.Absolute);
		var step = new Unit(2m, UnitTypes.Absolute);
		var p = new StrategyParam<Unit>("p", uBase).SetStep(step, uBase); // 10 + n*2
		p.Value = new Unit(12m, UnitTypes.Absolute);
		p.Value = new Unit(14m, UnitTypes.Absolute);
		AssertInvalid(() => p.Value = new Unit(13m, UnitTypes.Absolute));
		AssertInvalid(() => p.Value = new Unit(12m, UnitTypes.Percent)); // mismatched unit type
	}

	[TestMethod]
	public void Step_Optimize_NotAligned()
	{
		var p = new StrategyParam<int>("p", 10).SetStep(5);
		// Optimize range not aligned to step - allowed (no automatic validation for optimize values)
		p.SetOptimize(3, 17, 4);
		p.OptimizeFrom.AssertEqual(3);
		p.OptimizeTo.AssertEqual(17);
		p.OptimizeStep.AssertEqual(4);
		AssertInvalid(() => p.Value = 11); // still enforced for actual assignment
		p.Value = 15; // valid
	}

	[TestMethod]
	public void Step_Reassign_To_LargerValid()
	{
		var p = new StrategyParam<int>("p", 10).SetStep(5);
		p.SetStep(10); // new step invalidates former multiples like 15
		p.Value = 20; // valid
		AssertInvalid(() => p.Value = 15);
	}

	[TestMethod]
	public void Step_SaveLoad_Decimal()
	{
		var p = new StrategyParam<decimal>("p", 0.5m).SetStep(0.25m, 0.5m);
		var storage = new SettingsStorage();
		p.Save(storage);
		var p2 = new StrategyParam<decimal>("p").SetStep(0.25m, 0.5m);
		p2.Load(storage);
		p2.Value = 1.0m;
		AssertInvalid(() => p2.Value = 0.6m);
	}

	[TestMethod]
	public void Step_SaveLoad_TimeSpan()
	{
		var baseTs = TimeSpan.FromMinutes(10);
		var step = TimeSpan.FromMinutes(5);
		var p = new StrategyParam<TimeSpan>("p", baseTs).SetStep(step, baseTs);
		var storage = new SettingsStorage();
		p.Save(storage);
		var p2 = new StrategyParam<TimeSpan>("p").SetStep(step, baseTs);
		p2.Load(storage);
		p2.Value = TimeSpan.FromMinutes(20);
		AssertInvalid(() => p2.Value = TimeSpan.FromMinutes(23));
	}

	[TestMethod]
	public void Step_NullableInt_NullAfterValid()
	{
		var p = new StrategyParam<int?>("p", 0).SetStep(5);
		p.Value = 10;
		p.Value = null; // allowed - nullable removes step restriction when null
		p.Value = 15; // valid again
	}
}
