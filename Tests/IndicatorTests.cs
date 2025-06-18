namespace StockSharp.Tests;

using Ecng.Reflection;

[TestClass]
public class IndicatorTests
{
	private static IIndicatorValue CreateValue(IndicatorType type, IIndicator indicator, SecurityId secId, DateTimeOffset now, int idx, TimeSpan tf, bool isFinal, bool isEmpty)
	{
		var time = now + tf.Multiply(idx);

		ICandleMessage createCandle()
		{
			var candle = new TimeFrameCandleMessage
			{
				OpenPrice = 100 + RandomGen.GetInt(1, 10),
				HighPrice = 101 + RandomGen.GetInt(1, 10),
				LowPrice = 99 - RandomGen.GetInt(1, 10),
				ClosePrice = 100.5m + RandomGen.GetInt(1, 10),
				OpenTime = time,
				CloseTime = time + tf,
				TotalVolume = RandomGen.GetInt(1, 1000),
				SecurityId = secId,
				TypedArg = tf,
				State = CandleStates.Finished,
			};

			if (candle.HighPrice < candle.OpenPrice)
				(candle.OpenPrice, candle.HighPrice) = (candle.HighPrice, candle.OpenPrice);

			if (candle.HighPrice < candle.ClosePrice)
				(candle.ClosePrice, candle.HighPrice) = (candle.HighPrice, candle.ClosePrice);

			return candle;
		}

		var input = type.InputValue;

		if (input == typeof(DecimalIndicatorValue))
			return isEmpty ? new DecimalIndicatorValue(indicator, time) : new DecimalIndicatorValue(indicator, 100 + RandomGen.GetInt(1, 10), time) { IsFinal = isFinal };
		else if (input == typeof(CandleIndicatorValue))
			return isEmpty ? new CandleIndicatorValue(indicator, time) : new CandleIndicatorValue(indicator, createCandle()) { IsFinal = isFinal };
		else
			throw new InvalidOperationException(input.ToString());
	}

	private static IEnumerable<IndicatorType> GetIndicatorTypes()
	{
		IIndicatorProvider provider = new IndicatorProvider();
		provider.Init();
		return provider.All.Where(t => t.Indicator != typeof(CandlePatternIndicator));
	}

	[TestMethod]
	public void StateNonFinalInput()
	{
		var now = DateTimeOffset.UtcNow;
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromDays(1);

		foreach (var type in GetIndicatorTypes())
		{
			var indicator = type.CreateIndicator();
			indicator.IsFormed.AssertFalse();

			static void stateEquals(object a, object b)
			{
				if (a == null && b == null)
					return;
				else if (a == null || b == null)
					Assert.Fail();
				else if (a.GetType() != b.GetType())
					Assert.Fail();
				else if (a is IIndicator indA && b is IIndicator indB)
				{
					foreach (var field in a.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						var va = field.GetValue(indA);
						var vb = field.GetValue(indB);

						stateEquals(va, vb);
					}

					return;
				}
				else if (a is System.Collections.IEnumerable ea && b is System.Collections.IEnumerable eb && a is not string)
				{
					var enumA = ea.Cast<object>().ToArray();
					var enumB = eb.Cast<object>().ToArray();

					if (enumA.Length != enumB.Length)
						Assert.Fail();

					for (var i = 0; i < enumA.Length; i++)
					{
						stateEquals(enumA[i], enumB[i]);
					}

					return;
				}

				a.AssertEqual(b);
			}

			var before = indicator.TypedClone();

			for (var i = 0; i < 100; i++)
			{
				var value = CreateValue(type, indicator, secId, now, i, tf, false, RandomGen.GetBool());

				indicator.Process(value).ValidateValue();
				indicator.IsFormed.AssertFalse();

				stateEquals(before, indicator);
			}
		}
	}

	[TestMethod]
	public void NumValuesToInitialize()
	{
		var now = DateTimeOffset.UtcNow;
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromDays(1);

		foreach (var type in GetIndicatorTypes())
		{
			// non deterministic indicators
			if (type.Indicator == typeof(AdaptiveLaguerreFilter) ||
				type.Indicator == typeof(DemandIndex))
				continue;

			var indicator = type.CreateIndicator();

			var k = 0;

			do
			{
				indicator.NumValuesToInitialize.AssertGreater(0);
				indicator.IsFormed.AssertFalse();

				var finalCount = 0;
				var i = 0;

				while (!indicator.IsFormed)
				{
					var isFinal = RandomGen.GetBool();

					if (isFinal)
						finalCount++;

					var value = CreateValue(type, indicator, secId, now, i, tf, isFinal, false);
					indicator.Process(value).ValidateValue();

					finalCount.AssertLess(1000);

					i++;
				}

				finalCount.AssertEqual(indicator.NumValuesToInitialize);

				for (var n = 0; n < 100; n++)
				{
					var value = CreateValue(type, indicator, secId, now, i + n, tf, RandomGen.GetBool(), false);
					indicator.Process(value).ValidateValue();

					indicator.IsFormed.AssertTrue();
				}

				// test 5 times to ensure the same final count
				for (var j = 0; j < 5; j++)
				{
					// Reset
					indicator.Reset();
					indicator.IsFormed.AssertFalse();

					indicator.NumValuesToInitialize.AssertEqual(finalCount);

					var finalCount2 = 0;

					while (!indicator.IsFormed)
					{
						var isFinal = RandomGen.GetBool();

						if (isFinal)
							finalCount2++;

						var value = CreateValue(type, indicator, secId, now, i, tf, isFinal, false);
						indicator.Process(value).ValidateValue();

						i++;
					}

					finalCount.AssertEqual(finalCount2);
				}

				var isAnySet = false;
				SetRandom(indicator, () => isAnySet = true);

				if (!isAnySet)
					indicator.Reset();
			}
			while (++k < 5);
		}
	}

	private static readonly HashSet<string> _ignoreProps =
	[
		nameof(IIndicator.Name),
		nameof(IIndicator.Container),
		nameof(BaseComplexIndicator.InnerIndicators),
	];

	private static IEnumerable<PropertyInfo> GetProps(Type type)
		=> [..
		type
			.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => !_ignoreProps.Contains(p.Name))
			.Where(p => p.IsBrowsable())
		];

	private static void SetRandom(IIndicator indicator, Action check)
	{
		if (indicator is AwesomeOscillator ao)
		{
			ao.ShortMa.Length = RandomGen.GetInt(5, 20);
			ao.LongMa.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is OscillatorOfMovingAverage oma)
		{
			oma.ShortPeriod = RandomGen.GetInt(5, 20);
			oma.LongPeriod = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is KasePeakOscillator kpo)
		{
			kpo.ShortPeriod = RandomGen.GetInt(5, 20);
			kpo.LongPeriod = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is MovingAverageRibbon mar)
		{
			mar.ShortPeriod = RandomGen.GetInt(5, 20);
			mar.LongPeriod = RandomGen.GetInt(20, 50);
			mar.RibbonCount = RandomGen.GetInt(2, 10);
			check();
		}
		else if (indicator is RangeActionVerificationIndex ravi)
		{
			ravi.ShortSma.Length = RandomGen.GetInt(5, 20);
			ravi.LongSma.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is KaufmannAdaptiveMovingAverage kama)
		{
			kama.FastSCPeriod = RandomGen.GetInt(5, 20);
			kama.SlowSCPeriod = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is Ichimoku i)
		{
			i.Tenkan.Length = RandomGen.GetInt(5, 10);
			i.Kijun.Length = RandomGen.GetInt(10, 20);
			i.SenkouB.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is MovingAverageConvergenceDivergence macd)
		{
			macd.ShortMa.Length = RandomGen.GetInt(5, 20);
			macd.LongMa.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is MovingAverageConvergenceDivergenceHistogram hist)
		{
			hist.Macd.ShortMa.Length = RandomGen.GetInt(5, 20);
			hist.Macd.LongMa.Length = RandomGen.GetInt(20, 50);
			hist.SignalMa.Length = RandomGen.GetInt(5, 20);
			check();
		}
		else
			SetRandomPropsRecursive(indicator, check);
	}

	private static void SetRandomPropsRecursive(IIndicator indicator, Action check)
	{
		ArgumentNullException.ThrowIfNull(indicator);

		var type = indicator.GetType();
		var props = GetProps(type);

		foreach (var prop in props)
		{
			var propType = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;

			if (propType.Is<IIndicator>())
			{
				var nested = (IIndicator)prop.GetValue(indicator);

				if (nested is not null)
				{
					SetRandom(nested, check);
				}
			}
			else
			{
				if (!prop.IsModifiable())
					continue;

				object value;

				if (indicator is Fractals f && prop.Name == nameof(f.Length))
				{
					f.Length = 39;
					continue;
				}
				else if (indicator is LaguerreRSI lrsi && prop.Name == nameof(lrsi.Gamma))
				{
					lrsi.Gamma = RandomGen.GetDecimal(0.01m, 0.99m, 2);
					continue;
				}
				else if (indicator is ZigZag zz && prop.Name == nameof(zz.Deviation))
				{
					zz.Deviation = RandomGen.GetDecimal(0.01m, 0.99m, 2);
					continue;
				}
				else if (indicator is T3MovingAverage t3ma && prop.Name == nameof(t3ma.VolumeFactor))
				{
					t3ma.VolumeFactor = RandomGen.GetDecimal(0.01m, 0.99m, 2);
					continue;
				}
				else if (indicator is ArnaudLegouxMovingAverage alma && prop.Name == nameof(alma.Offset))
				{
					alma.Offset = RandomGen.GetDecimal(0.01m, 0.99m, 2);
					continue;
				}
				else
				{
					if (propType == typeof(int))
						value = RandomGen.GetInt(10, 100);
					else if (propType == typeof(decimal))
						value = (decimal)RandomGen.GetInt(1, 100) / 10;
					else if (propType == typeof(bool))
						value = RandomGen.GetBool();
					else if (propType == typeof(string))
						value = RandomGen.GetString(5, 10);
					else if (propType.IsEnum)
						value = RandomGen.GetEnum(propType);
					else if (propType == typeof(Unit))
						value = new Unit { Value = RandomGen.GetInt(1, 100), Type = RandomGen.GetEnum<UnitTypes>() };
					else if (propType.IsNumeric())
						value = RandomGen.GetInt(1, 100).To(propType);
					else
						continue;
				}

				prop.SetValue(indicator, value);
				
				check();
			}
		}
	}

	[TestMethod]
	public void SaveLoad()
	{
		var reseted = false;
		void OnReseted() => reseted = true;

		void ComparePropsRecursive(IIndicator obj1, IIndicator obj2)
		{
			ArgumentNullException.ThrowIfNull(obj1);
			ArgumentNullException.ThrowIfNull(obj2);

			var props = GetProps(obj1.GetType());

			foreach (var prop in props)
			{
				var propType = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;

				if (propType.Is<IIndicator>())
				{
					var nested1 = (IIndicator)prop.GetValue(obj1);
					var nested2 = (IIndicator)prop.GetValue(obj2);

					if (obj1 is not null && obj2 is not null)
						ComparePropsRecursive(nested1, nested2);
				}
				else
				{
					var v1 = prop.GetValue(obj1);
					var v2 = prop.GetValue(obj2);
					v1.AssertEqual(v2);
				}
			}
		}

		foreach (var type in GetIndicatorTypes())
		{
			var indicator = type.CreateIndicator();
			indicator.Reseted += OnReseted;

			SetRandom(indicator, () =>
			{
				reseted.AssertTrue();
				reseted = false;
			});

			var storage = indicator.Save();

			var restoredIndicator = type.CreateIndicator();
			restoredIndicator.Load(storage);

			ComparePropsRecursive(indicator, restoredIndicator);
		}
	}

	[TestMethod]
	public void Process()
	{
		var time = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var tf = TimeSpan.FromDays(1);
		var secId = Helper.CreateSecurity().ToSecurityId();

		var candles = Do.Invariant(() => File.ReadAllLines(Path.Combine(Helper.ResFolder, "ohlcv.txt")).Select(line =>
		{
			var parts = line.SplitByComma().Select(p => p.To<decimal>()).ToArray();

			return new TimeFrameCandleMessage
			{
				TypedArg		= tf,
				SecurityId		= secId,
				
				OpenTime		= time,
				CloseTime		= time += tf,

				OpenPrice		= parts[0],
				HighPrice		= parts[1],
				LowPrice		= parts[2],
				ClosePrice		= parts[3],
				TotalVolume		= parts[4],
			};
		}).ToArray());

		foreach (var type in GetIndicatorTypes())
		{
			var indicator = type.CreateIndicator();
			var inputType = type.InputValue;

			if (inputType == typeof(DecimalIndicatorValue))
				indicator.Check(candles, data => data.ClosePrice);
			else if (inputType == typeof(CandleIndicatorValue))
				indicator.Check(candles, data => data);
			else
				throw new InvalidOperationException(inputType.To<string>());
		}
	}
}

static class IndicatorDataRunner
{
	private class TestIndicatorValue<TInner> : IIndicatorValue
	{
		private readonly TInner _value;

		public TestIndicatorValue(IIndicator indicator, TInner value, TInner initFrom = default)
		{
			Indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
			_value = value is ICloneable cl ? (TInner)cl.Clone() : value;

			if (initFrom is ICandleMessage initCandle && _value is ICandleMessage candle)
			{
				candle.OpenTime = initCandle.OpenTime;
				candle.CloseTime = initCandle.CloseTime;
			}
		}

		public IIndicator Indicator { get; }
		public bool IsFinal { get; set; }
		DateTimeOffset IIndicatorValue.Time { get; }
		bool IIndicatorValue.IsFormed { get; set; }
		bool IIndicatorValue.IsEmpty => false;

		T IIndicatorValue.GetValue<T>(Level1Fields? field)
		{
			if (_value is T t)
				return t;
			else if (typeof(T).Is<ICandleMessage>())
			{
				var dec = _value.To<decimal>();

				return new TimeFrameCandleMessage
				{
					OpenPrice = dec,
					HighPrice = dec,
					LowPrice = dec,
					ClosePrice = dec,
					OpenTime = DateTimeOffset.UtcNow,
				}.To<T>();
			}
			else if (typeof(T) == typeof(decimal))
			{
				var c = _value.To<ICandleMessage>();
				return c.ClosePrice.To<T>();
			}
			else
				throw new NotSupportedException();
		}

		int IComparable<IIndicatorValue>.CompareTo(IIndicatorValue other)
			=> throw new NotSupportedException();

		int IComparable.CompareTo(object obj)
			=> throw new NotSupportedException();

		IEnumerable<object> IIndicatorValue.ToValues()
			=> throw new NotSupportedException();

		void IIndicatorValue.FromValues(object[] values)
			=> throw new NotSupportedException();
	}

	private class IndicatorData
	{
		public int Line { get; init; }
		public CandleMessage Candle { get; init; }
		public decimal Value { get; init; }
		public decimal Value2 { get; init; }
		public decimal Value3 { get; init; }
		public decimal Value4 { get; init; }
		public decimal Value5 { get; init; }
	}

	public static void Check<T>(this IIndicator indicator, CandleMessage[] candles, Func<ICandleMessage, T> getValue)
	{
		ArgumentNullException.ThrowIfNull(indicator);
		ArgumentNullException.ThrowIfNull(getValue);

		var values = new List<IndicatorData>();

		var epsilon = indicator.Measure switch
		{
			IndicatorMeasures.MinusOnePlusOne => 0.001m,
			IndicatorMeasures.Percent or IndicatorMeasures.Price or IndicatorMeasures.Volume => 0.1m,
			_ => throw new NotSupportedException(indicator.Measure.ToString()),
		};

		var data = Do.Invariant(() => File.ReadAllLines(Path.Combine(Helper.ResFolder, "IndicatorsData", $"{indicator.GetType().Name}.txt")).Select((line, idx) =>
		{
			var parts = line.SplitByComma();

			return new IndicatorData
			{
				Line = idx,
				Candle = candles[idx],
				Value = parts.Length > 0 ? parts[0].To<decimal>() : 0,
				Value2 = parts.Length > 1 ? parts[1].To<decimal>() : 0,
				Value3 = parts.Length > 2 ? parts[2].To<decimal>() : 0,
				Value4 = parts.Length > 3 ? parts[3].To<decimal>() : 0,
				Value5 = parts.Length > 4 ? parts[4].To<decimal>() : 0,
			};
		}).ToArray());

		for (var i = 0; i < data.Length; i++)
		{
			values.Add(data[i]);

			var inputValues = new List<TestIndicatorValue<T>>
			{
				new(indicator, getValue(data[i].Candle)) { IsFinal = true }
			};

			var numNonFinals = RandomGen.GetInt(10);
			for (var j = 0; j < numNonFinals; ++j)
			{
				var i2 = Math.Max(0, Math.Min(data.Length - 1, i + RandomGen.GetInt(-5, 5)));
				inputValues.Add(new(indicator, getValue(data[i2].Candle), i < data.Length - 1 ? getValue(data[i+1].Candle) : default) { IsFinal = false });
			}

			void CheckValue(IIndicatorValue value, int column)
			{
				if (!indicator.IsFormed)
					return;

				var shift = value is ShiftedIndicatorValue sv ? sv.Shift : 0;

				var data = values[values.Count - shift - 1];
				var testValue = column switch
				{
					0 => data.Value,
					1 => data.Value2,
					2 => data.Value3,
					3 => data.Value4,
					4 => data.Value5,
					_ => throw new ArgumentOutOfRangeException(column.ToString())
				};
				var indValue = value.IsEmpty ? 0 : value.ToDecimal().Round(2);

				((testValue - indValue).Abs() < epsilon).AssertTrue();
			}

			foreach (var inputValue in inputValues)
			{
				var value = indicator.Process(inputValue);

				ValidateValue(value);

				if (!inputValue.IsFinal)
					continue;

				value
					.Plain()
					.Select((sv, idx) => (v: sv, column: idx))
					.ForEach(p => CheckValue(p.v, p.column));
			}
		}

		indicator.IsFormed.AssertTrue();
	}

	private static readonly SynchronizedDictionary<IndicatorMeasures, Range<decimal>> _validators = [];

	public static void ValidateValue(this IIndicatorValue value)
	{
		ArgumentNullException.ThrowIfNull(value);

		value.Plain().ForEach(v =>
		{
			if (v.IsEmpty)
				return;

			var dec = v.GetValue<decimal>();
			var range = _validators.SafeAdd(v.Indicator.Measure);
			range.Contains(dec).AssertTrue();
		});
	}

	public static IEnumerable<IIndicatorValue> Plain(this IIndicatorValue val)
	{
		if (val is not ComplexIndicatorValue civ)
		{
			yield return val;
		}
		else
		{
			foreach (var v in civ.InnerValues.SelectMany(kv => Plain(kv.Value)))
				yield return v;
		}
	}
}