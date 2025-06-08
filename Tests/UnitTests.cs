namespace StockSharp.Tests;

using StockSharp.Messages;

[TestClass]
public class UnitTests
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

			u = (v + "л").ToUnit();
			u.AssertEqual(new Unit(v, UnitTypes.Limit));
			u.ToString().AssertEqual(v + "l");
			(v + "л").ToUnit().AssertEqual((v + "l").ToUnit());
			(v + "л").ToUnit().AssertEqual((v + "Л").ToUnit());
			(v + "l").ToUnit().AssertEqual((v + "L").ToUnit());

			//u = v + "%";
			//u.AssertEqual(new Unit(v, UnitTypes.Percent));
			//u.ToString().AssertEqual(v + "%");

			u = (v + "ш").ToUnit(false);
#pragma warning disable CS0618 // Type or member is obsolete
			u.AssertEqual(new Unit(v, UnitTypes.Step)/*.SetSecurity(security)*/);
#pragma warning restore CS0618 // Type or member is obsolete
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
			var u = v.Points(security);
			u.AssertEqual(new Unit(v * security.StepPrice.Value, UnitTypes.Absolute));
			((double)u).AssertEqual(v * 2);

			u = v.Pips(security);
			u.AssertEqual(new Unit(v * security.PriceStep.Value, UnitTypes.Absolute));
			((double)u).AssertEqual((double)v / 10);
		}
	}

	[TestMethod]
	public void InvalidCast()
	{
		Assert.ThrowsExactly<InvalidOperationException>(() => ((double)3.Percents()).AssertEqual(0));
	}

	[TestMethod]
	public void InvalidParse2()
	{
		Assert.ThrowsExactly<ArgumentException>(() => "10н".ToUnit());
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
	}

	[TestMethod]
	public void InvalidCompare()
	{
		(10.Percents() > 10).AssertFalse();
		(10.Percents() < 10).AssertFalse();
		(10.Percents() == 10).AssertFalse();
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
		Assert.ThrowsExactly<ArgumentNullException>(() => ((decimal)value).AssertNull());
	}

	[TestMethod]
	public void NullCast3()
	{
		Unit value = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => ((double)value).AssertNull());
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

		(new Unit(10, UnitTypes.Limit) == new Unit(10, UnitTypes.Limit)).AssertTrue();
		new Unit(10, UnitTypes.Limit).AssertEqual(new Unit(10, UnitTypes.Limit));
		(new Unit(10, UnitTypes.Limit) > null).AssertFalse();
		(new Unit(10, UnitTypes.Limit) < null).AssertFalse();
		(new Unit(10, UnitTypes.Limit) == null).AssertFalse();
		(new Unit(10, UnitTypes.Limit) != null).AssertTrue();

		var security = Helper.CreateSecurity();

		(10.Pips(security) == 1).AssertTrue();
		(10.Pips(security) > 0.9).AssertTrue();
		(10.Pips(security) < 1.1).AssertTrue();
		(1 == 10.Pips(security)).AssertTrue();
		(0.9 < 10.Pips(security)).AssertTrue();
		(1.1 > 10.Pips(security)).AssertTrue();
		(20.Pips(security) == 1.Points(security)).AssertTrue();
		(10.Pips(security) > null).AssertFalse();
		(10.Pips(security) < null).AssertFalse();
		(10.Pips(security) == null).AssertFalse();
		(10.Pips(security) != null).AssertTrue();

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
	}

	[TestMethod]
	public void Convert()
	{
		var security = Helper.CreateSecurity();

		for (var i = 0; i < 1000; i++)
		{
			var v = RandomGen.GetInt(-1000, 1000);

			var points = v.Points(security);
			var steps = v.Pips(security);
			var abs = (Unit)(decimal)v;

			var pointAbs = points.Convert(UnitTypes.Absolute);
			//pointAbs.Security.AssertSame(security);
			pointAbs.Type.AssertEqual(UnitTypes.Absolute);
			pointAbs.Value.AssertEqual(v * security.StepPrice ?? 1m);
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

	[TestMethod]
	public void Arithmetic()
	{
		var security = Helper.CreateSecurity();

		for (var i = 0; i < 100000; i++)
		{
			var u1 = RandomUnit();
			var u2 = RandomUnit();

			ProcessArithmetic(u1, u2, u1 + u2, (v1, v2) => v1 + v2, true);
			ProcessArithmetic(u1, u2, u1 - u2, (v1, v2) => v1 - v2, true);
			ProcessArithmetic(u1, u2, u1 * u2, (v1, v2) => v1 * v2, false);

			if (u2.Value == 0 || (u1.Value == 0 && u2.Type == UnitTypes.Percent))
				continue;

			ProcessArithmetic(u1, u2, u1 / u2, (v1, v2) => v1 / v2, false);
		}
	}

	private static void ProcessArithmetic(Unit u1, Unit u2, Unit result, Func<decimal, decimal, decimal> opr, bool transAbs)
	{
		//result.Security.AssertSame(security);

		if (u1.Type == u2.Type)
		{
			var resultValue = opr(u1.Value, u2.Value);

			result.Value.AssertEqual(resultValue);
			result.Type.AssertEqual(u1.Type);
		}
		else
		{
			if (u1.Type != UnitTypes.Percent && u2.Type != UnitTypes.Percent)
			{
				result.Type.AssertEqual(u1.Type);

				var resultValue = transAbs ? u2.Convert(u1.Type).Value : (decimal)u2;

				resultValue = opr(u1.Value, resultValue);

				result.Value.Round(5).AssertEqual(resultValue.Round(5));
			}
			else
			{
				result.Type.AssertEqual(u1.Type != UnitTypes.Percent ? u1.Type : u2.Type);

				var abs = u1.Type != UnitTypes.Percent ? u1.Value : u2.Value;
				var per = u1.Type != UnitTypes.Percent ? u2.Value : u1.Value;

				per = (abs.Abs() * per) / 100;

				var resultValue = u1.Type != UnitTypes.Percent ? opr(abs, per) : opr(per, abs);

				result.Value.AssertEqual(resultValue);
			}
		}
	}

	private static Unit RandomUnit()
	{
		return new(RandomGen.GetInt(-100, 100), RandomGen.GetEnum(
		[
			UnitTypes.Absolute,
			UnitTypes.Percent
		]));
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
		Assert.ThrowsExactly<ArgumentNullException>(() => "".ToUnit().AssertNull());
		((string)null).ToUnit(false).AssertNull();
	}

	[TestMethod]
	public void Empty3()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ((string)null).ToUnit().AssertNull());
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

		var limit = new Unit(30m, UnitTypes.Limit);
		var negativeLimit = -limit;
		negativeLimit.Type.AssertEqual(UnitTypes.Limit);
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
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Unit(100m, (UnitTypes)999));
	}

	[TestMethod]
	public void ToStringFormatTest()
	{
		var unit = new Unit(1234.5678m, UnitTypes.Percent);

		// Стандартный вывод
		unit.ToString().AssertEqual("1234.5678%");

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
#pragma warning restore CS0618 // Type or member is obsolete
		Unit.GetTypeSuffix(UnitTypes.Limit).AssertEqual("l");

		// Проверка исключения для неизвестного типа
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Unit.GetTypeSuffix((UnitTypes)999));
	}

	[TestMethod]
	public void LimitInvalidOperationTest()
	{
		var limit = new Unit(100m, UnitTypes.Limit);
		var percent = new Unit(10m, UnitTypes.Percent);

		Assert.ThrowsExactly<ArgumentException>(() => limit + percent);
	}

	[TestMethod]
	public void GetTypeValueNullTest()
	{
#pragma warning disable CS0618 // Type or member is obsolete
		var unit = new Unit(100m, UnitTypes.Step);
#pragma warning restore CS0618 // Type or member is obsolete
		Assert.ThrowsExactly<InvalidOperationException>(() => unit.Convert(UnitTypes.Absolute));
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
}