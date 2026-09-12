namespace StockSharp.Tests;

using StockSharp.Messages;

[TestClass]
public class UnitTests : BaseTestClass
{
	[TestMethod]
	public void Parse()
	{
		//var security = Helper.CreateSecurity();

		for (var i = 0; i < 1000; i++)
		{
			var v = RandomGen.GetInt(-1000, 1000);

			var u = v.To<string>().ToUnit();
			u.AssertEqual(new Unit(v, UnitTypes.Absolute));
			u.ToString().AssertEqual(v.To<string>());

			//u = v.ToString();
			//u.AssertEqual(new Unit(v, UnitTypes.Absolute));
			//u.ToString().AssertEqual(v.ToString());

			u = (v + "%").ToUnit();
			u.AssertEqual(new Unit(v, UnitTypes.Percent));
			u.ToString().AssertEqual(v + "%");

#pragma warning disable CS0618
			u = (v + "л").ToUnit();
			u.AssertEqual(new Unit(v, UnitTypes.Limit));
			u.ToString().AssertEqual(v + "l");
			(v + "л").ToUnit().AssertEqual((v + "l").ToUnit());
			(v + "л").ToUnit().AssertEqual((v + "Л").ToUnit());
			(v + "l").ToUnit().AssertEqual((v + "L").ToUnit());
#pragma warning restore CS0618

			//u = v + "%";
			//u.AssertEqual(new Unit(v, UnitTypes.Percent));
			//u.ToString().AssertEqual(v + "%");

			u = (v + "ш").ToUnit(false);
#pragma warning disable CS0618
			u.AssertEqual(new Unit(v, UnitTypes.Step)/*.SetSecurity(security)*/);
#pragma warning restore CS0618
			u.ToString().AssertEqual(v + "s");
			(v + "ш").ToUnit(false).AssertEqual((v + "s").ToUnit(false));
			(v + "ш").ToUnit(false).AssertEqual((v + "Ш").ToUnit(false));
			(v + "s").ToUnit(false).AssertEqual((v + "S").ToUnit(false));

			u = (v + "п").ToUnit(false);
#pragma warning disable CS0618 // Type or member is obsolete
			u.AssertEqual(new Unit(v, UnitTypes.Point)/*.SetSecurity(security)*/);
#pragma warning restore CS0618 // Type or member is obsolete
			u.ToString().AssertEqual(v + "p");
			(v + "п").ToUnit(false).AssertEqual((v + "p").ToUnit(false));
			(v + "п").ToUnit(false).AssertEqual((v + "П").ToUnit(false));
			(v + "p").ToUnit(false).AssertEqual((v + "P").ToUnit(false));
		}
	}

	[TestMethod]
	public void Cast()
	{
		var security = Helper.CreateSecurity();

		for (var i = 0; i < 1000; i++)
		{
			var v = RandomGen.GetInt(-1000, 1000);
#pragma warning disable CS0612, CS0618 // Type or member is obsolete
			var u = v.Points(security);
			u.AssertEqual(new Unit(v * security.StepPrice.Value, UnitTypes.Absolute));
#pragma warning restore CS0612, CS0618 // Type or member is obsolete
			((double)u).AssertEqual(v * 2);

			u = v.Pips(security);
			u.AssertEqual(new Unit(v * security.PriceStep.Value, UnitTypes.Absolute));
			((double)u).AssertEqual((double)v / 10);
		}
	}

	[TestMethod]
	public void InvalidCast()
	{
		ThrowsExactly<InvalidOperationException>(() => ((double)3.Percents()).AssertEqual(0));
	}

	[TestMethod]
	public void InvalidParse2()
	{
		ThrowsExactly<ArgumentException>(() => "10н".ToUnit());
	}

	[TestMethod]
	public void Percent()
	{
		var u = 10.Percents();
		(u == 0).AssertFalse();
		(u + 0 == 0).AssertTrue();
		(u - 0 == 0).AssertTrue();
		(u * 1 == 0.1m).AssertTrue();
		(u / 1 == 0.1m).AssertTrue();
		(u + 1 == 1.1m).AssertTrue();
		(u - 1 == -0.9m).AssertTrue();
		(0 + u == 0).AssertTrue();
		(0 - u == 0).AssertTrue();
		(1 * u == 0.1m).AssertTrue();
		(1 / u == 10).AssertTrue();
		(1 + u == 1.1m).AssertTrue();
		(1 - u == 0.9m).AssertTrue();
		(500 * u == 50).AssertTrue();
	}

	[TestMethod]
	public void InvalidCompare()
	{
		(10.Percents() > 10).AssertFalse();
		(10.Percents() < 10).AssertFalse();
		(10.Percents() == 10).AssertFalse();
	}

	// IComparable requires sign(a.CompareTo(b)) to be the opposite of sign(b.CompareTo(a)).
	// A percent and an absolute value must not both report themselves as the greater one.
	[TestMethod]
	public void CompareToPercentAndAbsoluteIsAntisymmetric()
	{
		var per = 10.Percents();
		var abs = new Unit(10m, UnitTypes.Absolute);

		var forward = Math.Sign(per.CompareTo(abs));
		var backward = Math.Sign(abs.CompareTo(per));

		forward.AssertEqual(-backward, $"CompareTo must be antisymmetric: '{per}' vs '{abs}' gave {forward}, the reverse gave {backward}");
	}

	[TestMethod]
	public void CompareToAgreesWithComparisonOperatorsForComparableUnits()
	{
		AssertAgrees(10.Percents(), 20.Percents());
		AssertAgrees(new(20m, UnitTypes.Absolute), new(10m, UnitTypes.Absolute));
		AssertAgrees(new(10m, UnitTypes.Absolute), new(10m, UnitTypes.Absolute));

		static void AssertAgrees(Unit u1, Unit u2)
		{
			var cmp = u1.CompareTo(u2);

			if (cmp > 0)
				(u1 > u2).AssertTrue($"CompareTo reported '{u1}' greater than '{u2}', but operator > disagrees");
			else if (cmp < 0)
				(u1 < u2).AssertTrue($"CompareTo reported '{u1}' less than '{u2}', but operator < disagrees");
			else
				(u1 == u2).AssertTrue($"CompareTo reported '{u1}' equal to '{u2}', but operator == disagrees");
		}
	}

	[TestMethod]
	public void NullCast()
	{
		Unit value = null;
		((decimal?)value).AssertNull();
		((double?)value).AssertNull();
	}

	[TestMethod]
	public void NullCast2()
	{
		Unit value = null;
		ThrowsExactly<ArgumentNullException>(() => ((decimal)value).AssertNull());
	}

	[TestMethod]
	public void NullCast3()
	{
		Unit value = null;
		ThrowsExactly<ArgumentNullException>(() => ((double)value).AssertNull());
	}

	[TestMethod]
	public void NullCompare()
	{
		Unit u1 = null;

		(u1 == null).AssertTrue();
		(u1 != null).AssertFalse();

		(null == u1).AssertTrue();
		(null != u1).AssertFalse();

		u1 = 10m;

		(u1 == null).AssertFalse();
		(u1 != null).AssertTrue();

		(null == u1).AssertFalse();
		(null != u1).AssertTrue();
	}

	[TestMethod]
	public void NullArithmetic()
	{
		Unit u1 = null;
		u1.AssertNull();

		u1 = u1 + 10;
		u1.AssertNull();

		u1 = 10 + u1;
		u1.AssertNull();

		u1 += 10;
		u1.AssertNull();

		u1 -= u1;
		u1.AssertNull();

		u1 = -u1;
		u1.AssertNull();

		u1 *= 10;
		u1.AssertNull();

		u1 /= 10;
		u1.AssertNull();

		Unit u2 = null;

		u1 *= u2;
		u1.AssertNull();

		u1 /= u2;
		u1.AssertNull();
	}

	[TestMethod]
	public void Compare()
	{
		((Unit)10 > 10).AssertFalse();
		(new Unit(10, UnitTypes.Absolute) == 10).AssertTrue();
		(10 == new Unit(10, UnitTypes.Absolute)).AssertTrue();
		((Unit)10 > null).AssertFalse();
		((Unit)10 < null).AssertFalse();
		((Unit)10 == null).AssertFalse();
		((Unit)10 != null).AssertTrue();

		(new Unit(10, UnitTypes.Absolute) == new Unit(10, UnitTypes.Absolute)).AssertTrue();
		new Unit(10, UnitTypes.Absolute).AssertEqual(new Unit(10, UnitTypes.Absolute));
		(new Unit(10, UnitTypes.Absolute) > null).AssertFalse();
		(new Unit(10, UnitTypes.Absolute) < null).AssertFalse();
		(new Unit(10, UnitTypes.Absolute) == null).AssertFalse();
		(new Unit(10, UnitTypes.Absolute) != null).AssertTrue();

		var security = Helper.CreateSecurity();

		(10.Pips(security) == 1).AssertTrue();
		(10.Pips(security) > 0.9).AssertTrue();
		(10.Pips(security) < 1.1).AssertTrue();
		(1 == 10.Pips(security)).AssertTrue();
		(0.9 < 10.Pips(security)).AssertTrue();
		(1.1 > 10.Pips(security)).AssertTrue();
#pragma warning disable CS0612 // Type or member is obsolete
		(20.Pips(security) == 1.Points(security)).AssertTrue();
#pragma warning restore CS0612 // Type or member is obsolete
		(10.Pips(security) > null).AssertFalse();
		(10.Pips(security) < null).AssertFalse();
		(10.Pips(security) == null).AssertFalse();
		(10.Pips(security) != null).AssertTrue();

#pragma warning disable CS0612 // Type or member is obsolete
        (10.Points(security) == 1).AssertFalse();
        (10.Points(security) != 1).AssertTrue();
		(10.Points(security) > 0.9).AssertTrue();
		(10.Points(security) < 0.9).AssertFalse();
		(10.Points(security) > 1.1).AssertTrue();
		(10.Points(security) < 1.1).AssertFalse();
		(1 == 10.Points(security)).AssertFalse();
		(1 != 10.Points(security)).AssertTrue();
		(0.9 < 10.Points(security)).AssertTrue();
		(0.9 > 10.Points(security)).AssertFalse();
		(1.1 > 10.Points(security)).AssertFalse();
		(1.1 < 10.Points(security)).AssertTrue();
		(20.Pips(security) == 1.Points(security)).AssertTrue();
		(20.Pips(security) != 1.Points(security)).AssertFalse();
#pragma warning restore CS0612 // Type or member is obsolete
	}

	[TestMethod]
	public void Convert()
	{
		var security = Helper.CreateSecurity();

		for (var i = 0; i < 1000; i++)
		{
			var v = RandomGen.GetInt(-1000, 1000);

#pragma warning disable CS0612 // Type or member is obsolete
			var points = v.Points(security);
#pragma warning restore CS0612 // Type or member is obsolete
			var steps = v.Pips(security);
			var abs = (Unit)(decimal)v;

			var pointAbs = points.Convert(UnitTypes.Absolute);
			//pointAbs.Security.AssertSame(security);
			pointAbs.Type.AssertEqual(UnitTypes.Absolute);
#pragma warning disable CS0618 // Type or member is obsolete
			pointAbs.Value.AssertEqual(v * security.StepPrice ?? 1m);
#pragma warning restore CS0618// Type or member is obsolete
			(pointAbs == points).AssertTrue();
			pointAbs.Equals(points).AssertTrue();

			//var pointStep = points.Convert(UnitTypes.Step);
			////pointStep.Security.AssertSame(security);
			//pointStep.Type.AssertEqual(UnitTypes.Step);
			//pointStep.Value.AssertEqual((v * security.StepPrice) / security.PriceStep ?? 1m);
			//(pointStep == points).AssertTrue();
			//pointStep.Equals(points).AssertTrue();

			var stepAbs = steps.Convert(UnitTypes.Absolute);
			//stepAbs.Security.AssertSame(security);
			stepAbs.Type.AssertEqual(UnitTypes.Absolute);
			stepAbs.Value.AssertEqual(v * security.PriceStep ?? 1m);
			(stepAbs == steps).AssertTrue();
			stepAbs.Equals(steps).AssertTrue();

			//var stepPoint = steps.Convert(UnitTypes.Point);
			////stepPoint.Security.AssertSame(security);
			//stepPoint.Type.AssertEqual(UnitTypes.Point);
			//stepPoint.Value.AssertEqual((v * security.PriceStep ?? 1m) / security.StepPrice ?? 1m);
			//(stepPoint == steps).AssertTrue();
			//stepPoint.Equals(steps).AssertTrue();

			//var absStep = abs.Convert(UnitTypes.Step, security);
			////absStep.Security.AssertSame(security);
			//absStep.Type.AssertEqual(UnitTypes.Step);
			//absStep.Value.AssertEqual(v / security.PriceStep ?? 1m);
			//(absStep == abs).AssertTrue();
			//absStep.Equals(abs).AssertTrue();

			//var absPoint = abs.Convert(UnitTypes.Point, security);
			////absPoint.Security.AssertSame(security);
			//absPoint.Type.AssertEqual(UnitTypes.Point);
			//absPoint.Value.AssertEqual(v / security.StepPrice ?? 1m);
			//(absPoint == abs).AssertTrue();
			//absPoint.Equals(abs).AssertTrue();

			//points.Convert(UnitTypes.Point).AssertEqual(points);
			//steps.Convert(UnitTypes.Step).AssertEqual(steps);
			abs.Convert(UnitTypes.Absolute).AssertEqual(abs);

			//abs.Convert(UnitTypes.Absolute).Convert(UnitTypes.Point, security).Convert(UnitTypes.Step).Convert(UnitTypes.Absolute).AssertEqual(abs);
		}
	}

	/// <summary>
	/// A percent mixed with an absolute value is what <see cref="Unit"/> exists for - a stop set to
	/// "10% away" has to become a price - so the answer must be the one a person works out on paper.
	/// Two rules describe all of it: a percent is taken of the magnitude of the value it meets
	/// (10% of 100 and of -100 are both 10), and multiplying by a percent yields that share itself,
	/// so 100 * 10% is 10 and not 1000. Every expectation below is written out from those two rules;
	/// none is recomputed here from the formula the operators use, because an oracle that repeats the
	/// implementation agrees with it however wrong it has become.
	/// </summary>
	[TestMethod]
	public void ArithmeticAgainstWorkedExamples()
	{
		var absolute = new Unit(100m, UnitTypes.Absolute);
		var tenPercent = new Unit(10m, UnitTypes.Percent);

		(absolute + tenPercent).AssertEqual(new Unit(110m, UnitTypes.Absolute));
		(absolute - tenPercent).AssertEqual(new Unit(90m, UnitTypes.Absolute));
		(absolute * tenPercent).AssertEqual(new Unit(10m, UnitTypes.Absolute));
		(absolute / tenPercent).AssertEqual(new Unit(10m, UnitTypes.Absolute));

		var negative = new Unit(-100m, UnitTypes.Absolute);
		(negative + tenPercent).AssertEqual(new Unit(-90m, UnitTypes.Absolute));
		(negative - tenPercent).AssertEqual(new Unit(-110m, UnitTypes.Absolute));

		// Two values of one kind: the numbers are combined as they stand and the kind is kept.
		Check(new Unit(100m) + new Unit(25m), 125m, UnitTypes.Absolute, "100 + 25");
		Check(new Unit(100m) - new Unit(25m), 75m, UnitTypes.Absolute, "100 - 25");
		Check(new Unit(100m) * new Unit(25m), 2500m, UnitTypes.Absolute, "100 * 25");
		Check(new Unit(100m) / new Unit(25m), 4m, UnitTypes.Absolute, "100 / 25");

		Check(new Unit(30m, UnitTypes.Percent) + new Unit(12m, UnitTypes.Percent), 42m, UnitTypes.Percent, "30% + 12%");
		Check(new Unit(30m, UnitTypes.Percent) - new Unit(12m, UnitTypes.Percent), 18m, UnitTypes.Percent, "30% - 12%");
		Check(new Unit(30m, UnitTypes.Percent) * new Unit(12m, UnitTypes.Percent), 360m, UnitTypes.Percent, "30% * 12%");
		Check(new Unit(30m, UnitTypes.Percent) / new Unit(12m, UnitTypes.Percent), 2.5m, UnitTypes.Percent, "30% / 12%");

		// 10% of 100 is 10. The answer is an absolute value whichever side the percent stands on,
		// and the operands keep their order: 10% - 100 is 10 - 100, not 100 - 10.
		Check(new Unit(100m) + new Unit(10m, UnitTypes.Percent), 110m, UnitTypes.Absolute, "100 + 10%");
		Check(new Unit(100m) - new Unit(10m, UnitTypes.Percent), 90m, UnitTypes.Absolute, "100 - 10%");
		Check(new Unit(100m) * new Unit(10m, UnitTypes.Percent), 10m, UnitTypes.Absolute, "100 * 10%");
		Check(new Unit(100m) / new Unit(10m, UnitTypes.Percent), 10m, UnitTypes.Absolute, "100 / 10%");

		Check(new Unit(10m, UnitTypes.Percent) + new Unit(100m), 110m, UnitTypes.Absolute, "10% + 100");
		Check(new Unit(10m, UnitTypes.Percent) - new Unit(100m), -90m, UnitTypes.Absolute, "10% - 100");
		Check(new Unit(10m, UnitTypes.Percent) * new Unit(100m), 10m, UnitTypes.Absolute, "10% * 100");
		Check(new Unit(10m, UnitTypes.Percent) / new Unit(100m), 0.1m, UnitTypes.Absolute, "10% / 100");

		// The share is taken of the magnitude, so 10% of -100 is 10 and it is that 10 which is then
		// added, subtracted or reported: a loss of 100 grows to 110 by subtracting a tenth of itself.
		Check(new Unit(-100m) + new Unit(10m, UnitTypes.Percent), -90m, UnitTypes.Absolute, "-100 + 10%");
		Check(new Unit(-100m) - new Unit(10m, UnitTypes.Percent), -110m, UnitTypes.Absolute, "-100 - 10%");
		Check(new Unit(-100m) * new Unit(10m, UnitTypes.Percent), 10m, UnitTypes.Absolute, "-100 * 10%");
		Check(new Unit(-100m) / new Unit(10m, UnitTypes.Percent), -10m, UnitTypes.Absolute, "-100 / 10%");

		// A negative percent is a share owed back, so it reverses each of the four answers above.
		Check(new Unit(100m) + new Unit(-10m, UnitTypes.Percent), 90m, UnitTypes.Absolute, "100 + -10%");
		Check(new Unit(100m) - new Unit(-10m, UnitTypes.Percent), 110m, UnitTypes.Absolute, "100 - -10%");
		Check(new Unit(100m) * new Unit(-10m, UnitTypes.Percent), -10m, UnitTypes.Absolute, "100 * -10%");
		Check(new Unit(100m) / new Unit(-10m, UnitTypes.Percent), -10m, UnitTypes.Absolute, "100 / -10%");

		static void Check(Unit result, decimal expectedValue, UnitTypes expectedType, string expression)
		{
			result.Value.AssertEqual(expectedValue, $"{expression} must be {expectedValue}, got {result.Value}");
			result.Type.AssertEqual(expectedType, $"{expression} must be measured as {expectedType}, got {result.Type}");
		}
	}

	[TestMethod]
	public void Empty()
	{
		"".ToUnit(false).AssertNull();
		((string)null).ToUnit(false).AssertNull();
	}

	[TestMethod]
	public void Empty2()
	{
		ThrowsExactly<ArgumentNullException>(() => "".ToUnit().AssertNull());
		((string)null).ToUnit(false).AssertNull();
	}

	[TestMethod]
	public void Empty3()
	{
		ThrowsExactly<ArgumentNullException>(() => ((string)null).ToUnit().AssertNull());
	}

	[TestMethod]
	public void NotEquals()
	{
		var u1 = "1".ToUnit();
		var u2 = "1L".ToUnit();
		(u1 == u2).AssertFalse();
		u1.AssertNotEqual(u2);
	}

	[TestMethod]
	public void ReferenceEqualityTest()
	{
		var unit = new Unit(100m, UnitTypes.Absolute);
		var clone = unit.Clone();

		ReferenceEquals(unit, clone).AssertFalse();
		unit.Equals(clone).AssertTrue();
	}

	[TestMethod]
	public void UnaryMinusTest()
	{
		var absolute = new Unit(100m, UnitTypes.Absolute);
		var negativeAbsolute = -absolute;
		negativeAbsolute.Type.AssertEqual(UnitTypes.Absolute);
		negativeAbsolute.Value.AssertEqual(-100m);

		var percent = new Unit(50m, UnitTypes.Percent);
		var negativePercent = -percent;
		negativePercent.Type.AssertEqual(UnitTypes.Percent);
		negativePercent.Value.AssertEqual(-50m);

#pragma warning disable CS0618 // Verify unary negation for the legacy enum alias.
		var limit = new Unit(30m, UnitTypes.Limit);
		var negativeLimit = -limit;
		negativeLimit.Type.AssertEqual(UnitTypes.Limit);
#pragma warning restore CS0618
		negativeLimit.Value.AssertEqual(-30m);
	}

	[TestMethod]
	public void LoadSaveTest()
	{
		var storage = new SettingsStorage();
		var unit = new Unit(200m, UnitTypes.Percent);

		unit.Save(storage);

		var loadedUnit = new Unit();
		loadedUnit.Load(storage);

		loadedUnit.AssertEqual(unit);
	}

	[TestMethod]
	public void PositiveNegativeValuesTest()
	{
		var positive = new Unit(100m, UnitTypes.Absolute);
		var negative = new Unit(-50m, UnitTypes.Absolute);

		// Сложение
		var sum = positive + negative;
		sum.Type.AssertEqual(UnitTypes.Absolute);
		sum.Value.AssertEqual(50m);

		// Вычитание
		var difference = positive - negative;
		difference.Type.AssertEqual(UnitTypes.Absolute);
		difference.Value.AssertEqual(150m);

		// Умножение
		var product = positive * negative;
		product.Type.AssertEqual(UnitTypes.Absolute);
		product.Value.AssertEqual(-5000m);

		// Деление
		var quotient = positive / negative;
		quotient.Type.AssertEqual(UnitTypes.Absolute);
		quotient.Value.AssertEqual(-2m);
	}

	[TestMethod]
	public void UnknownUnitTypeTest()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Unit(100m, (UnitTypes)999));
	}

	[TestMethod]
	public void ToStringFormatTest()
	{
		var unit = new Unit(1234.5678m, UnitTypes.Percent);

		// Стандартный вывод
		unit.ToString(null, CultureInfo.InvariantCulture).AssertEqual("1234.5678%");

		// Формат с двумя десятичными знаками
		unit.ToString("F2", CultureInfo.InvariantCulture).AssertEqual("1234.57%");

		// Формат без десятичных знаков
		unit.ToString("F0", CultureInfo.InvariantCulture).AssertEqual("1235%");
	}

	[TestMethod]
	public void OperatorsWithNullTest()
	{
		Unit u1 = null;
		var u2 = new Unit(100m, UnitTypes.Absolute);

		// Сложение
		var sum = u1 + u2;
		sum.AssertNull();

		sum = u2 + u1;
		sum.AssertNull();

		// Вычитание
		var difference = u1 - u2;
		difference.AssertNull();

		difference = u2 - u1;
		difference.AssertNull();

		// Умножение
		var product = u1 * u2;
		product.AssertNull();

		product = u2 * u1;
		product.AssertNull();

		// Деление
		var quotient = u1 / u2;
		quotient.AssertNull();

		quotient = u2 / u1;
		quotient.AssertNull();
	}

	[TestMethod]
	public void GetTypeSuffixTest()
	{
		Unit.GetTypeSuffix(UnitTypes.Absolute).AssertEqual(string.Empty);
		Unit.GetTypeSuffix(UnitTypes.Percent).AssertEqual("%");
#pragma warning disable CS0618 // Type or member is obsolete
		Unit.GetTypeSuffix(UnitTypes.Step).AssertEqual("s");
		Unit.GetTypeSuffix(UnitTypes.Point).AssertEqual("p");
		Unit.GetTypeSuffix(UnitTypes.Limit).AssertEqual("l");
#pragma warning restore CS0618 // Type or member is obsolete

		// Проверка исключения для неизвестного типа
		ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Unit.GetTypeSuffix((UnitTypes)999));
	}

	[TestMethod]
	public void AbsolutePercentOperationTest()
	{
		var absolute = new Unit(100m, UnitTypes.Absolute);
		var percent = new Unit(10m, UnitTypes.Percent);

		// Absolute + Percent теперь работает (было исключение для Limit)
		var result = absolute + percent;
		result.Value.AssertEqual(110m);
		result.Type.AssertEqual(UnitTypes.Absolute);
	}

	[TestMethod]
	public void GetTypeValueNullTest()
	{
#pragma warning disable CS0618 // Type or member is obsolete
		var unit = new Unit(100m, UnitTypes.Step);
#pragma warning restore CS0618 // Type or member is obsolete
		ThrowsExactly<InvalidOperationException>(() => unit.Convert(UnitTypes.Absolute));
	}

	[TestMethod]
	public void BoundaryValuesTest()
	{
		// Максимальное значение
		var maxUnit = new Unit(decimal.MaxValue, UnitTypes.Absolute);
		maxUnit.ToString().AssertEqual(decimal.MaxValue.ToString());

		// Минимальное значение
		var minUnit = new Unit(decimal.MinValue, UnitTypes.Absolute);
		minUnit.ToString().AssertEqual(decimal.MinValue.ToString());

		// Нулевое значение
		var zeroUnit = new Unit(0m, UnitTypes.Percent);
		zeroUnit.ToString().AssertEqual("0%");
	}

	[TestMethod]
	public void CloneTest()
	{
		var original = new Unit(250m, UnitTypes.Percent);
		var clone = original.Clone();

		clone.AssertEqual(original);
		original.AssertNotSame(clone);
	}

	/// <summary>
	/// The validators take an object, so the bare number a UI editor or a wire frame hands over reaches
	/// them unchanged. A number is not a measured value - it carries no unit - and must be refused.
	/// </summary>
	[TestMethod]
	public void UnitValidatorsRejectValuesThatAreNotUnits()
	{
		IsFalse(new UnitGreaterThanZeroAttribute().IsValid(5m));
		IsFalse(new UnitNotNegativeAttribute().IsValid(5m));
		IsFalse(new UnitNullOrMoreZeroAttribute().IsValid(5m));
		IsFalse(new UnitNullOrNotNegativeAttribute().IsValid(5m));
	}

	/// <summary>
	/// The four simple validators differ in exactly two places: whether zero passes, and whether a
	/// missing value passes. Both are pinned here for all four, in both unit types.
	/// </summary>
	[TestMethod]
	public void UnitValidatorsAgreeOnZeroAndNull()
	{
		var zero = new Unit(0m);
		var positive = new Unit(0.5m, UnitTypes.Percent);
		var negative = new Unit(-1m);

		IsFalse(new UnitGreaterThanZeroAttribute().IsValid(zero));
		IsTrue(new UnitGreaterThanZeroAttribute().IsValid(positive));
		IsFalse(new UnitGreaterThanZeroAttribute().IsValid(negative));
		IsFalse(new UnitGreaterThanZeroAttribute().IsValid(null));

		IsTrue(new UnitNotNegativeAttribute().IsValid(zero));
		IsTrue(new UnitNotNegativeAttribute().IsValid(positive));
		IsFalse(new UnitNotNegativeAttribute().IsValid(negative));
		IsFalse(new UnitNotNegativeAttribute().IsValid(null));

		IsFalse(new UnitNullOrMoreZeroAttribute().IsValid(zero));
		IsTrue(new UnitNullOrMoreZeroAttribute().IsValid(positive));
		IsFalse(new UnitNullOrMoreZeroAttribute().IsValid(negative));
		IsTrue(new UnitNullOrMoreZeroAttribute().IsValid(null));

		IsTrue(new UnitNullOrNotNegativeAttribute().IsValid(zero));
		IsTrue(new UnitNullOrNotNegativeAttribute().IsValid(positive));
		IsFalse(new UnitNullOrNotNegativeAttribute().IsValid(negative));
		IsTrue(new UnitNullOrNotNegativeAttribute().IsValid(null));
	}

	/// <summary>
	/// Both bounds of the range are documented inclusive. A value in different units is not a point on
	/// the same scale - 3% is neither inside nor outside an absolute 1..5 - so it is refused.
	/// </summary>
	[TestMethod]
	public void UnitRangeIncludesBothBoundsAndItsUnitType()
	{
		var range = new UnitRangeAttribute(new Unit(1m), new Unit(5m));

		IsTrue(range.IsValid(new Unit(1m)));
		IsTrue(range.IsValid(new Unit(3m)));
		IsTrue(range.IsValid(new Unit(5m)));
		IsFalse(range.IsValid(new Unit(0.99m)));
		IsFalse(range.IsValid(new Unit(5.01m)));
		IsFalse(range.IsValid(new Unit(3m, UnitTypes.Percent)));
		IsFalse(range.IsValid("3"));
	}

	/// <summary>
	/// A range needs two bounds on one scale, the lower one first; neither pair below describes a range
	/// at all, so building the attribute is where it has to be refused.
	/// </summary>
	[TestMethod]
	public void UnitRangeRefusesBoundsItCannotUse()
	{
		Throws<ArgumentOutOfRangeException>(() => new UnitRangeAttribute(new Unit(1m), new Unit(5m, UnitTypes.Percent)));
		Throws<ArgumentOutOfRangeException>(() => new UnitRangeAttribute(new Unit(5m), new Unit(1m)));
	}

	/// <summary>
	/// The grid is Base + N*Step with N not negative. Base 0.1 and step 0.2 give 0.1, 0.3, 0.5, 0.7 and
	/// so on: 0.4 falls between two points, and 0.05 is below the base, so the grid never reaches it.
	/// </summary>
	[TestMethod]
	public void UnitStepAcceptsOnlyPointsOnItsGrid()
	{
		var step = new UnitStepAttribute(new Unit(0.2m), new Unit(0.1m));

		IsTrue(step.IsValid(new Unit(0.1m)));
		IsTrue(step.IsValid(new Unit(0.3m)));
		IsTrue(step.IsValid(new Unit(0.7m)));
		IsFalse(step.IsValid(new Unit(0.4m)));
		IsFalse(step.IsValid(new Unit(0.05m)));
		IsFalse(step.IsValid(new Unit(0.3m, UnitTypes.Percent)));
		IsFalse(step.IsValid("0.3"));
	}

	/// <summary>
	/// A step of zero or less names no sequence of points, and a step measured differently from its base
	/// cannot be added to it; both are refused when the attribute is built.
	/// </summary>
	[TestMethod]
	public void UnitStepRefusesAGridItCannotDefine()
	{
		Throws<ArgumentOutOfRangeException>(() => new UnitStepAttribute(new Unit(0m), new Unit(0m)));
		Throws<ArgumentOutOfRangeException>(() => new UnitStepAttribute(new Unit(-0.2m), new Unit(0m)));
		Throws<ArgumentOutOfRangeException>(() => new UnitStepAttribute(new Unit(0.2m), new Unit(0.1m, UnitTypes.Percent)));
	}

	/// <summary>
	/// A missing value is a separate question from the restriction itself, and only the property that
	/// declares it optional may pass it.
	/// </summary>
	[TestMethod]
	public void UnitRangeAndStepAcceptNullOnlyWhenNullChecksAreDisabled()
	{
		IsFalse(new UnitRangeAttribute(new Unit(1m), new Unit(5m)).IsValid(null));
		IsTrue(new UnitRangeAttribute(new Unit(1m), new Unit(5m)) { DisableNullCheck = true }.IsValid(null));

		IsFalse(new UnitStepAttribute(new Unit(0.2m), new Unit(0.1m)).IsValid(null));
		IsTrue(new UnitStepAttribute(new Unit(0.2m), new Unit(0.1m)) { DisableNullCheck = true }.IsValid(null));
	}
}
