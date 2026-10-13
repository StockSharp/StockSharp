namespace StockSharp.Tests;

/// <summary>
/// What an indicator type declares about itself: names, docs, attributes and input types.
/// </summary>
partial class IndicatorTests
{
	[TestMethod]
	public void DocUrlUnique()
	{
		var duplicates = GetIndicatorTypes()
			.Select(t => t.DocUrl)
			.Where(url => !url.IsEmpty())
			.Select(url => url.ToLowerInvariant())
			.GroupBy(x => x)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToArray();

		if (duplicates.Any())
			Fail($"Duplicate DocUrl(s) found: {duplicates.JoinCommaSpace()}");
	}

	[TestMethod]
	public void NameUnique()
	{
		var duplicates = GetIndicatorTypes()
			.Select(t => t.Name)
			.Select(n => n.ToLowerInvariant())
			.GroupBy(x => x)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToArray();

		if (duplicates.Any())
			Fail($"Duplicate Names(s) found: {duplicates.JoinCommaSpace()}");
	}

	[TestMethod]
	public void DescriptionUnique()
	{
		var duplicates = GetIndicatorTypes()
			.Select(t => t.Description)
			.Where(n => !n.IsEmpty())
			.Select(n => n.ToLowerInvariant())
			.GroupBy(x => x)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToArray();

		if (duplicates.Any())
			Fail($"Duplicate Descriptions(s) found: {duplicates.JoinCommaSpace()}");
	}

	[TestMethod]
	public void RequiredAttributes()
	{
		foreach (var type in GetIndicatorTypes())
		{
			var indicatorType = type.Indicator;

			// Check [IndicatorIn]
			var inAttr = indicatorType.GetAttribute<IndicatorInAttribute>();
			inAttr.AssertNotNull($"Indicator {indicatorType.Name} missing [IndicatorIn] attribute.");

			// Check [IndicatorOut]
			var outAttr = indicatorType.GetAttribute<IndicatorOutAttribute>();
			outAttr.AssertNotNull($"Indicator {indicatorType.Name} missing [IndicatorOut] attribute.");

			// Check [Doc]
			var docAttr = indicatorType.GetAttribute<DocAttribute>();
			docAttr.AssertNotNull($"Indicator {indicatorType.Name} missing [Doc] attribute.");
		}
	}

	/// <summary>
	/// Edges where the outer indicator holds another indicator but never hands it its own input, so that
	/// inner's input requirement says nothing about what the outer must be fed. Every entry has to name the
	/// exact reason, and <see cref="InputTypeCoversDelegation"/> fails on an entry that no longer matches a
	/// real edge, so the list cannot quietly turn into a blanket suppression.
	/// </summary>
	private static readonly (Type outer, Type inner)[] _nonForwardingDelegations =
	[
		// Both combine two lines' already computed values and never process them - the lines are driven by the
		// owning complex indicator instead: GatorHistogram.OnProcess reads Line1/Line2.GetNullableCurrentValue()
		// and IchimokuSenkouALine.OnProcessDecimal reads Tenkan/Kijun.GetCurrentValue().
		(typeof(GatorHistogram), typeof(AlligatorLine)),
		(typeof(IchimokuSenkouALine), typeof(IchimokuLine)),

		// Substitutes a derived scalar for the input, so StochasticK is used as a plain aggregator over the
		// MACD histogram: SchaffTrendCycle.OnProcessDecimal calls
		// StochasticK.Process(input, (macdHist - _buffer.Min.Value) / den), and that overload builds a brand
		// new DecimalIndicatorValue instead of passing the outer input on.
		(typeof(SchaffTrendCycle), typeof(StochasticK)),
	];

	/// <summary>
	/// An indicator hands its own input straight to the indicators it delegates to:
	/// <see cref="BaseComplexIndicator{TValue}.OnProcess"/> passes <c>input</c> to every inner, and the
	/// hand-rolled delegations (an indicator kept in a field, e.g. <c>AverageTrueRange._trueRange</c>) do the
	/// same. So the outer declaration has to satisfy every inner declaration. An outer that declares
	/// <see cref="DecimalIndicatorValue"/> while an inner requires <see cref="CandleIndicatorValue"/> quietly
	/// feeds that inner a degenerate candle whose open/high/low/close are all the same number - the inner keeps
	/// computing, just over bars that never existed.
	/// </summary>
	[TestMethod]
	public void InputTypeCoversDelegation()
	{
		var errors = new SortedSet<string>(StringComparer.Ordinal);
		var usedExceptions = new HashSet<(Type, Type)>();

		foreach (var indicator in ReachableIndicators())
		{
			var outerType = indicator.GetType();

			if (outerType.GetValueType(true) != typeof(DecimalIndicatorValue))
				continue;

			foreach (var inner in Delegates(indicator))
			{
				var innerType = inner.GetType();

				if (innerType.GetValueType(true) != typeof(CandleIndicatorValue))
					continue;

				if (_nonForwardingDelegations.Contains((outerType, innerType)))
				{
					usedExceptions.Add((outerType, innerType));
					continue;
				}

				errors.Add($"{outerType.Name} declares {nameof(DecimalIndicatorValue)} but delegates to {innerType.Name}, which requires {nameof(CandleIndicatorValue)}.");
			}
		}

		var stale = _nonForwardingDelegations.Where(e => !usedExceptions.Contains(e)).ToArray();

		if (stale.Length > 0)
			Fail($"Stale {nameof(_nonForwardingDelegations)} entries (no such delegation any more): {stale.Select(e => $"{e.outer.Name}->{e.inner.Name}").JoinCommaSpace()}");

		if (errors.Count > 0)
			Fail($"Indicators whose declared input does not cover what they delegate to:{Environment.NewLine}{errors.JoinN()}");
	}

	/// <summary>
	/// Indicators whose entire contract is to aggregate whatever stream they are given, so being fed a decimal
	/// instead of a candle is intended polymorphism rather than starvation: fed candles they aggregate the bar
	/// extremes, fed plain numbers they aggregate the numbers. They read a candle field, but they do not require
	/// one, so they stay on <see cref="DecimalIndicatorValue"/>.
	/// </summary>
	private static readonly Type[] _feedPolymorphic =
	[
		typeof(Highest),
		typeof(Lowest),
	];

	/// <summary>
	/// Declaring <see cref="DecimalIndicatorValue"/> is a promise that the close price is all the indicator
	/// looks at. Running the same series twice - once as candles, once as bare close prices - has to produce the
	/// same numbers for such an indicator. If it does not, the implementation reads open/high/low/volume and the
	/// declaration is wrong. This catches what <see cref="InputTypeCoversDelegation"/> cannot: an indicator that
	/// reads bar fields without going through an inner that declares candles (e.g. Donchian Channels, whose
	/// inners are the deliberately feed-polymorphic <see cref="Highest"/>/<see cref="Lowest"/>).
	/// </summary>
	[TestMethod]
	public async Task InputTypeCoversFeedSensitivity()
	{
		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromDays(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = await LoadCandles(secId, time, tf);

		var errors = new List<string>();
		var usedExceptions = new HashSet<Type>();

		foreach (var type in GetIndicatorTypes())
		{
			if (type.InputValue != typeof(DecimalIndicatorValue))
				continue;

			var byClose = type.CreateIndicator().Render(candles, c => c.ClosePrice).Rows;
			var byCandle = type.CreateIndicator().Render(candles, c => c).Rows;

			var diff = -1;

			for (var i = 0; i < byClose.Length; i++)
			{
				if (byClose[i] != byCandle[i])
				{
					diff = i;
					break;
				}
			}

			// The exemption is applied AFTER measuring rather than as an early skip, so an entry
			// that has stopped being feed-sensitive is reported as stale instead of silently
			// exempting an indicator that no longer needs exempting.
			if (_feedPolymorphic.Contains(type.Indicator))
			{
				if (diff >= 0)
					usedExceptions.Add(type.Indicator);

				continue;
			}

			if (diff < 0)
				continue;

			errors.Add($"{type.Indicator.Name} declares {nameof(DecimalIndicatorValue)} but reacts to the bar fields: line {diff + 1} is '{byClose[diff]}' fed the close and '{byCandle[diff]}' fed the candle.");
		}

		if (errors.Count > 0)
			Fail($"Indicators whose declared input does not match what they read:{Environment.NewLine}{errors.JoinN()}");

		var stale = _feedPolymorphic.Where(t => !usedExceptions.Contains(t)).ToArray();

		if (stale.Length > 0)
			Fail($"Stale {nameof(_feedPolymorphic)} entries (no longer feed-sensitive, or no longer registered as declaring {nameof(DecimalIndicatorValue)}): {stale.Select(t => t.Name).JoinCommaSpace()}");
	}

	/// <summary>
	/// Every indicator instance reachable from the registered ones through delegation, including the
	/// <see cref="IndicatorHiddenAttribute"/> building blocks that never appear in the provider on their own
	/// (e.g. <see cref="AlligatorLine"/>) - the invariant applies to them just the same.
	/// </summary>
	private static IEnumerable<IIndicator> ReachableIndicators()
	{
		var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
		var pending = new Queue<IIndicator>(GetIndicatorTypes().Select(t => t.CreateIndicator()));

		while (pending.Count > 0)
		{
			var indicator = pending.Dequeue();

			if (!visited.Add(indicator))
				continue;

			yield return indicator;

			foreach (var inner in Delegates(indicator))
				pending.Enqueue(inner);
		}
	}

	/// <summary>
	/// The indicators <paramref name="indicator"/> drives: the inner ones of a complex indicator plus anything
	/// of an indicator type kept in an instance field, private and inherited ones included (auto-property
	/// backing fields are covered by that, which is how the hand-rolled delegations are declared).
	/// </summary>
	private static IEnumerable<IIndicator> Delegates(IIndicator indicator)
	{
		if (indicator is IComplexIndicator complex)
		{
			foreach (var inner in complex.InnerIndicators)
				yield return inner;
		}

		for (var type = indicator.GetType(); type is not null && type != typeof(object); type = type.BaseType)
		{
			foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (!field.FieldType.Is<IIndicator>())
					continue;

				if (field.GetValue(indicator) is IIndicator inner)
					yield return inner;
			}
		}
	}
}
