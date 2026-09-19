namespace StockSharp.Tests;

static class IndicatorDataRunner
{
	private class TestIndicatorValue<TInner> : IIndicatorValue
	{
		private readonly TInner _value;

		public TestIndicatorValue(IIndicator indicator, DateTime time, TInner value, TInner initFrom = default)
		{
			Indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
			_value = value is ICloneable cl ? (TInner)cl.Clone() : value;

			if (initFrom is ICandleMessage initCandle && _value is ICandleMessage candle)
			{
				candle.OpenTime = initCandle.OpenTime;
				candle.CloseTime = initCandle.CloseTime;
			}

			Time = time;
		}

		public IIndicator Indicator { get; }
		public bool IsFinal { get; set; }
		public DateTime Time { get; }
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
					OpenTime = DateTime.UtcNow,
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
		public decimal?[] Values { get; init; }
	}

	/// <summary>
	/// An indicator run rendered in the on-disk shape of Resources/IndicatorsData/&lt;Indicator&gt;.txt.
	/// </summary>
	public class RenderedSeries
	{
		/// <summary>
		/// One line per candle.
		/// </summary>
		public string[] Rows { get; init; }

		/// <summary>
		/// Index of the first row from which <see cref="IIndicator.IsFormed"/> held. Rows before it are the
		/// warm-up: <see cref="Check{T}"/> returns before looking at them, so nothing there is validated.
		/// </summary>
		public int FormedFrom { get; init; }

		/// <summary>
		/// Comparison tolerance <see cref="Check{T}"/> applies, derived from <see cref="IIndicator.Measure"/>.
		/// </summary>
		public decimal Epsilon { get; init; }
	}

	/// <summary>
	/// Runs <paramref name="indicator"/> over <paramref name="candles"/> with final values only and renders one
	/// line per candle in the exact on-disk shape of Resources/IndicatorsData/&lt;Indicator&gt;.txt: every plain
	/// (flattened) component of the produced value, rounded to two decimals and formatted with the invariant
	/// culture, joined by comma; an empty component renders as an empty field. This is the single definition of
	/// that format - the reference-data generator and the feed-comparison test both go through it, so neither
	/// can drift away from what <see cref="Check{T}"/> parses.
	/// </summary>
	public static RenderedSeries Render<T>(this IIndicator indicator, CandleMessage[] candles, Func<ICandleMessage, T> getValue)
	{
		ArgumentNullException.ThrowIfNull(indicator);
		ArgumentNullException.ThrowIfNull(candles);
		ArgumentNullException.ThrowIfNull(getValue);

		var rows = new string[candles.Length];
		var formedFrom = candles.Length;

		Do.Invariant(() =>
		{
			for (var i = 0; i < candles.Length; i++)
			{
				var candle = candles[i];
				var value = indicator.Process(new TestIndicatorValue<T>(indicator, candle.OpenTime, getValue(candle)) { IsFinal = true });

				rows[i] = value.Plain().Select(v => v.IsEmpty ? string.Empty : v.ToDecimal().Round(2).ToString()).JoinComma();

				if (indicator.IsFormed && formedFrom > i)
					formedFrom = i;
			}
		});

		return new()
		{
			Rows = rows,
			FormedFrom = formedFrom,
			Epsilon = Epsilon(indicator),
		};
	}

	private static decimal Epsilon(IIndicator indicator)
		=> indicator.Measure switch
		{
			IndicatorMeasures.MinusOnePlusOne => 0.001m,
			IndicatorMeasures.Percent or IndicatorMeasures.Price or IndicatorMeasures.Volume => 0.1m,
			_ => throw new NotSupportedException(indicator.Measure.ToString()),
		};

	/// <summary>
	/// Splits one rendered or committed line into its columns, an empty field standing for no value.
	/// </summary>
	public static decimal?[] ParseRow(string row)
	{
		ArgumentNullException.ThrowIfNull(row);

		return [.. row.SplitByComma().Select(c => c.IsEmpty() ? (decimal?)null : c.To<decimal>())];
	}

	/// <summary>
	/// Lists the differences between one produced row and its reference row, over the union of both column
	/// sets: a column the reference gives a value for and the run no longer produces is a difference, as is a
	/// column the run produces and the reference does not have, as is a pair of values further apart than
	/// <paramref name="epsilon"/>.
	/// </summary>
	/// <param name="produced">Produced columns, null standing for no value.</param>
	/// <param name="reference">Reference columns, null standing for no value.</param>
	/// <param name="epsilon">Comparison tolerance.</param>
	/// <param name="line">1-based line number the messages carry.</param>
	/// <returns>Difference descriptions, empty when the rows agree.</returns>
	public static IEnumerable<string> RowDiffs(decimal?[] produced, decimal?[] reference, decimal epsilon, int line)
	{
		ArgumentNullException.ThrowIfNull(produced);
		ArgumentNullException.ThrowIfNull(reference);

		var count = produced.Length.Max(reference.Length);

		for (var col = 0; col < count; col++)
		{
			var now = col < produced.Length ? produced[col] : null;
			var was = col < reference.Length ? reference[col] : null;

			if (now is null && was is null)
				continue;
			else if (now is null)
				yield return $"line {line} column {col}: reference '{was}', now empty";
			else if (was is null)
				yield return $"line {line} column {col}: reference empty, now '{now}'";
			else if ((was.Value - now.Value).Abs() >= epsilon)
				yield return $"line {line} column {col}: reference '{was}', now '{now}'";
		}
	}

	/// <summary>
	/// Lists the differences between the committed reference lines and a fresh run that <see cref="Check{T}"/>
	/// would also trip over, applying its own reading rules: a row before the indicator is formed is never
	/// looked at, and from there on every row is compared through <see cref="RowDiffs"/> - over the union of
	/// the produced and the reference columns, so a value the reference has and the run no longer produces is
	/// reported rather than passed over.
	/// </summary>
	public static string[] ValidatedDiffs(this RenderedSeries rendered, string[] committed)
	{
		ArgumentNullException.ThrowIfNull(rendered);
		ArgumentNullException.ThrowIfNull(committed);

		return [.. Do.Invariant(() =>
		{
			var diffs = new List<string>();

			for (var i = rendered.FormedFrom; i < rendered.Rows.Length; i++)
			{
				if (i >= committed.Length)
				{
					diffs.Add($"line {i + 1}: reference data ends, indicator still produces '{rendered.Rows[i]}'");
					continue;
				}

				diffs.AddRange(RowDiffs(ParseRow(rendered.Rows[i]), ParseRow(committed[i]), rendered.Epsilon, i + 1));
			}

			return diffs;
		})];
	}

	public static void Check<T>(this IIndicator indicator, CandleMessage[] candles, Func<ICandleMessage, T> getValue)
	{
		ArgumentNullException.ThrowIfNull(indicator);
		ArgumentNullException.ThrowIfNull(getValue);

		var values = new List<IndicatorData>();

		var epsilon = Epsilon(indicator);

		// Counts the non-final values actually injected below. They are what proves ValidateValue's
		// complex-value contract on non-final input and that non-final input leaves no residue in the
		// state the next final comparison reads, so the run must not silently end up feeding none.
		var nonFinalCount = 0;

		var data = Do.Invariant(() => File.ReadAllLines(Path.Combine(Helper.ResFolder, "IndicatorsData", $"{indicator.GetType().Name}.txt")).Select((line, idx) =>
		{
			var parts = line.SplitByComma();

			return new IndicatorData
			{
				Line = idx,
				Candle = candles[idx],
				Values = [.. parts.Select(p => p.To<decimal?>())],
			};
		}).ToArray());

		for (var i = 0; i < data.Length; i++)
		{
			values.Add(data[i]);

			var inputValues = new List<TestIndicatorValue<T>>
			{
				new(indicator, data[i].Candle.OpenTime, getValue(data[i].Candle)) { IsFinal = true }
			};

			// 0..3 rather than 0..10 non-final values per bar. The injections stay - they are the point,
			// see nonFinalCount above - only their density drops, from ~5 extra values per bar to ~1.5,
			// which is what made this a ~6x input multiplier over the 1658 reference bars.
			var numNonFinals = RandomGen.GetInt(3);
			for (var j = 0; j < numNonFinals; ++j)
			{
				var i2 = 0.Max((data.Length - 1).Min(i + RandomGen.GetInt(-5, 5)));
				inputValues.Add(new(indicator, data[i2].Candle.OpenTime, getValue(data[i2].Candle), i < data.Length - 1 ? getValue(data[i+1].Candle) : default) { IsFinal = false });
			}

			nonFinalCount += numNonFinals;

			foreach (var inputValue in inputValues)
			{
				var value = indicator.Process(inputValue);

				ValidateValue(value);

				if (!inputValue.IsFinal || !indicator.IsFormed)
					continue;

				// Once formed, the whole reference row is answered for: every column it gives a value for
				// has to come out of the run again, and every column the run produces has to be in it.
				var row = values[values.Count - 1];
				var produced = value.Plain().Select(sv => sv.IsEmpty ? (decimal?)null : sv.ToDecimal().Round(2)).ToArray();
				var diffs = RowDiffs(produced, row.Values, epsilon, row.Line + 1).ToArray();

				(diffs.Length == 0).AssertTrue($"{indicator}: {diffs.JoinN()}");
			}
		}

		indicator.IsFormed.AssertTrue();
		nonFinalCount.AssertGreater(0, indicator.ToString());
	}

	private static readonly SynchronizedDictionary<IndicatorMeasures, Range<decimal>> _validators = [];

	public static void ValidateValue(this IIndicatorValue value)
	{
		ArgumentNullException.ThrowIfNull(value);

		if (value is IComplexIndicatorValue complex)
		{
			if (complex.InnerValues.Count > 0)
			{
				var allFinal = complex.InnerValues.Values.All(v => v.IsFinal);
				complex.IsFinal.AssertEqual(allFinal, $"IComplexIndicatorValue.IsFinal={complex.IsFinal}, but inner values: [{complex.InnerValues.Values.Select(v => v.IsFinal.ToString()).JoinCommaSpace()}]");
			}
		}

		value.Plain().ForEach(v =>
		{
			if (v.IsEmpty)
				return;

			var dec = v.ToDecimal();
			var range = _validators.SafeAdd(v.Indicator.Measure);
			range.Contains(dec).AssertTrue();
		});
	}

	public static IEnumerable<IIndicatorValue> Plain(this IIndicatorValue val)
	{
		if (val is not IComplexIndicatorValue civ)
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
