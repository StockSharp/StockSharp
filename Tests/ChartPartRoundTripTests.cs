namespace StockSharp.Tests;

using System.Drawing;
using System.Reflection;

using StockSharp.Charting;

/// <summary>
/// A chart panel names its input sockets after the identifiers of the parts drawn on it, and a schema
/// is read and written by whatever opens it - a strategy running without a window included. A part that
/// comes back different from how it went in changes the file by the act of reading it, and reopening
/// the schema in a designer cannot undo that.
/// </summary>
[TestClass]
public class ChartPartRoundTripTests : BaseTestClass
{
	private static readonly IChartBuilder _builder = new DummyChartBuilder();

	/// <summary>
	/// Every part the builder makes, by the name of the method that makes it.
	/// </summary>
	private static IEnumerable<(string Name, Func<IAsyncPersistable> Create)> Parts()
	{
		yield return (nameof(IChartBuilder.CreateArea), _builder.CreateArea);
		yield return (nameof(IChartBuilder.CreateAxis), _builder.CreateAxis);
		yield return (nameof(IChartBuilder.CreateCandleElement), _builder.CreateCandleElement);
		yield return (nameof(IChartBuilder.CreateIndicatorElement), _builder.CreateIndicatorElement);
		yield return (nameof(IChartBuilder.CreateLineElement), _builder.CreateLineElement);
		yield return (nameof(IChartBuilder.CreateBandElement), _builder.CreateBandElement);
		yield return (nameof(IChartBuilder.CreateBubbleElement), _builder.CreateBubbleElement);
		yield return (nameof(IChartBuilder.CreateOrderElement), _builder.CreateOrderElement);
		yield return (nameof(IChartBuilder.CreateTradeElement), _builder.CreateTradeElement);
		yield return (nameof(IChartBuilder.CreateActiveOrdersElement), _builder.CreateActiveOrdersElement);
		yield return (nameof(IChartBuilder.CreateAnnotation), _builder.CreateAnnotation);
	}

	/// <summary>
	/// The parts a panel finds by an identifier: an area, and every element drawn in one.
	/// </summary>
	private static IEnumerable<(string Name, Func<IAsyncPersistable> Create, Func<IAsyncPersistable, Guid> Id)> IdentifiedParts()
	{
		yield return (nameof(IChartBuilder.CreateArea), _builder.CreateArea, p => ((IChartArea)p).Id);

		foreach (var (name, create) in Parts())
		{
			if (create() is IChartElement)
				yield return (name, create, p => ((IChartElement)p).Id);
		}
	}

	/// <summary>
	/// A part that comes back under another identifier is another part, and every link into the panel
	/// that drew it loses its end.
	/// </summary>
	[TestMethod]
	public async Task EveryPartComesBackUnderTheIdentifierItWasSavedWith()
	{
		foreach (var (name, create, id) in IdentifiedParts())
		{
			var saved = create();

			var storage = new SettingsStorage();
			await saved.SaveAsync(storage, CancellationToken);

			var loaded = create();
			await loaded.LoadAsync(storage, CancellationToken);

			id(loaded).AssertEqual(id(saved),
				$"{name} has to write the identifier it reads back, or a schema saved without a window renames every part in it");
		}
	}

	/// <summary>
	/// A part that is saved, loaded and saved again writes the same thing twice. Whatever the second
	/// save leaves out, or leaves different, the load did not carry - and a schema opened by anything
	/// that then saves it comes back without it.
	/// </summary>
	/// <remarks>
	/// The values are set before the first save, so a part is compared holding something rather than
	/// its defaults: two saves of an untouched part agree on nothing being there.
	/// </remarks>
	[TestMethod]
	public async Task EveryPartSurvivesBeingSavedLoadedAndSavedAgain()
	{
		var lost = new List<string>();

		foreach (var (name, create) in Parts())
		{
			var part = create();

			Fill(part);

			var first = new SettingsStorage();
			await part.SaveAsync(first, CancellationToken);

			var again = create();
			await again.LoadAsync(first, CancellationToken);

			var second = new SettingsStorage();
			await again.SaveAsync(second, CancellationToken);

			var differences = Differences(first, second).OrderBy(d => d).ToArray();

			if (differences.Length > 0)
				lost.Add($"{name}: {differences.JoinComma()}");
		}

		lost.Count.AssertEqual(0,
			$"a part that does not come back as it went in changes the schema by the act of reading it:{Environment.NewLine}{lost.JoinN()}");
	}

	/// <summary>
	/// What a part reads on load, it has to have written on save. A save-load-save comparison cannot
	/// see this one: a value written by nobody is absent from both saves, and two absences agree.
	/// </summary>
	[TestMethod]
	public async Task EveryPartWritesEverythingItReadsBack()
	{
		var gaps = new List<string>();

		foreach (var (name, create) in Parts())
		{
			var written = new SettingsStorage();
			await create().SaveAsync(written, CancellationToken);

			// Loaded from a storage that answers everything with nothing and remembers what it was
			// asked for: the keys a part names are the keys its own Save has to have written.
			var asked = new AskedKeysStorage();
			await create().LoadAsync(asked, CancellationToken);

			var missing = asked.Asked.Where(key => !written.ContainsKey(key)).OrderBy(k => k).ToArray();

			if (missing.Length > 0)
				gaps.Add($"{name}: {missing.JoinComma()}");
		}

		gaps.Count.AssertEqual(0,
			$"a value read from a storage nobody wrote it to comes back as a default the first time a schema is saved without a window:{Environment.NewLine}{gaps.JoinN()}");
	}

	/// <summary>
	/// A storage that answers every question with nothing and remembers what it was asked for.
	/// </summary>
	private class AskedKeysStorage : SettingsStorage
	{
		public List<string> Asked { get; } = [];

		public override bool TryGetValue(string key, out object value)
		{
			Asked.Add(key);
			value = null;
			return false;
		}

		public override bool ContainsKey(string key)
		{
			Asked.Add(key);
			return false;
		}
	}

	/// <summary>
	/// Gives every settable property of a part a value of its own, so a round trip has something to
	/// lose. A property whose type this does not know how to vary is left alone rather than guessed at.
	/// </summary>
	private static void Fill(object part)
	{
		var seed = 1;

		foreach (var prop in part.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length > 0)
				continue;

			var value = Vary(prop.PropertyType, seed++);

			if (value is not null)
				prop.SetValue(part, value);
		}
	}

	/// <summary>
	/// A value of the given type that is not its default, or null when this does not know one.
	/// </summary>
	private static object Vary(Type type, int seed)
	{
		var t = type.GetUnderlyingType() ?? type;

		if (t == typeof(bool))
			return true;

		if (t == typeof(int))
			return seed + 11;

		if (t == typeof(long))
			return (long)seed + 11;

		if (t == typeof(decimal))
			return seed + 1.5m;

		if (t == typeof(double))
			return seed + 1.5;

		if (t == typeof(string))
			return $"value{seed}";

		if (t == typeof(TimeSpan))
			return TimeSpan.FromSeconds(seed + 11);

		if (t == typeof(Guid))
			return Guid.NewGuid();

		if (t == typeof(Color))
			return Color.FromArgb(255, 10 + seed, 20 + seed, 30 + seed);

		if (t == typeof(DateTime))
			return new DateTime(2020, 1, 1).AddDays(seed);

		if (t.IsEnum)
			return Enum.GetValues(t).Cast<object>().LastOrDefault();

		return null;
	}

	/// <summary>
	/// The keys the two storages do not agree on, each said in the way a reader can act on.
	/// </summary>
	private static IEnumerable<string> Differences(SettingsStorage first, SettingsStorage second)
	{
		foreach (var key in first.Keys)
		{
			if (!second.ContainsKey(key))
			{
				yield return $"{key} (lost)";
				continue;
			}

			if ($"{first[key]}" != $"{second[key]}")
				yield return $"{key} ({first[key]} -> {second[key]})";
		}

		foreach (var key in second.Keys.Where(k => !first.ContainsKey(k)))
			yield return $"{key} (appeared)";
	}

}
