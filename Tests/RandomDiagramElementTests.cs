namespace StockSharp.Tests;

using StockSharp.Diagram;
using StockSharp.Diagram.Elements;

/// <summary>
/// The random element feeds a strategy a drawn value, so a diagram containing one produced a
/// different backtest on every run - and an optimisation over such a diagram compared results
/// that were not made under the same conditions. The source is the strategy's, so one setting
/// covers every element in the diagram: a seeded source repeats a run, a stub states outright
/// what the element will output.
/// </summary>
[TestClass]
public class RandomDiagramElementTests : BaseTestClass
{
	/// <summary>
	/// A source handing out values that were decided in advance.
	/// </summary>
	private class StubRandomProvider(params double[] values) : IRandomProvider
	{
		private int _index;

		private double Take() => values[_index++ % values.Length];

		int IRandomProvider.Next(int min, int max) => min + (int)(Take() * (max - min));
		long IRandomProvider.NextLong(long min, long max) => min + (long)(Take() * (max - min));
		double IRandomProvider.NextDouble() => Take();
		void IRandomProvider.NextBytes(byte[] buffer) => throw new NotSupportedException();
	}

	private static readonly DateTime _time = new(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);

	private static RandomDiagramElement Attach(RandomDiagramElement element, IRandomProvider provider)
	{
		var model = new CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink>(new InMemoryCompositionModelBehavior());
		var strategy = new DiagramStrategy();

		if (provider is not null)
			strategy.RandomProvider = provider;

		var composition = new CompositionDiagramElement(model)
		{
			Strategy = strategy,
		};

		element.Init(composition);
		return element;
	}

	private static List<decimal> Run(IRandomProvider provider, int count)
	{
		var element = Attach(new RandomDiagramElement
		{
			Min = 0,
			Max = 100,
		}, provider);

		var values = new List<decimal>();
		element.ProcessOutput += v => values.Add(v.GetValue<Unit>().Value);

		// Start raises the first value, each Flush one more.
		element.Start(_time);

		while (values.Count < count)
			element.Flush(_time);

		return values;
	}

	[TestMethod]
	public void The_strategys_source_decides_what_the_element_outputs()
	{
		// The point of the whole arrangement: a test states the draws and knows the output.
		var values = Run(new StubRandomProvider(0, 0.25, 0.5), 3);

		AreEqual(0m, values[0]);
		AreEqual(25m, values[1]);
		AreEqual(50m, values[2]);
	}

	[TestMethod]
	public void A_seeded_strategy_runs_the_same_series_twice()
	{
		var a = Run(new SeededRandomProvider(42), 20);
		var b = Run(new SeededRandomProvider(42), 20);

		a.SequenceEqual(b).AssertTrue("the same seed must give the same series");
	}

	[TestMethod]
	public void Different_seeds_do_not_run_the_same_series()
	{
		var a = Run(new SeededRandomProvider(1), 20);
		var b = Run(new SeededRandomProvider(2), 20);

		a.SequenceEqual(b).AssertFalse("two seeds that differ must not give the same series");
	}

	[TestMethod]
	public void A_strategy_left_alone_still_varies()
	{
		var a = Run(null, 20);
		var b = Run(null, 20);

		a.SequenceEqual(b).AssertFalse("with nobody holding a seed the element must behave as it always did");
	}

	[TestMethod]
	public void Values_stay_within_the_bounds()
	{
		foreach (var value in Run(new SeededRandomProvider(3), 200))
			(value is >= 0 and <= 100).AssertTrue($"{value} is outside the range the element was given");
	}
}
