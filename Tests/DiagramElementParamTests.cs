namespace StockSharp.Tests;

using System.ComponentModel.DataAnnotations;

using StockSharp.Diagram;
using StockSharp.Diagram.Elements;
using StockSharp.Localization;

[TestClass]
public class DiagramElementParamTests : BaseTestClass
{
	/// <summary>
	/// The chart panel names its collections of chart elements and of axes in the interface's language: a property grid
	/// shows these names, and a Russian one showed "Candles" and "Chart axes".
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void ChartPanelNamesItsCollectionsInTheInterfacesLanguage()
	{
		const string code = "q3";

		var previous = LocalizedStrings.ActiveLanguage;

		try
		{
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.CultureCodeKey, "en-US" },
				{ LocalizedStrings.CandlesKey, "Q candles" },
				{ LocalizedStrings.IndicatorsKey, "Q indicators" },
				{ LocalizedStrings.OrdersKey, "Q orders" },
				{ LocalizedStrings.TradesKey, "Q trades" },
				{ LocalizedStrings.XAxisKey, "Q x" },
				{ LocalizedStrings.YAxisKey, "Q y" },
				{ LocalizedStrings.ChartAxesKey, "Q axes" },
			});

			LocalizedStrings.ActiveLanguage = code;

			var panel = new DummyChartDiagramElement();

			DisplayAttribute Display(string name)
				=> panel.Parameters.Single(param => param.Name == name).Attributes.OfType<DisplayAttribute>().Single();

			AreEqual("Q candles", Display("CandlesCollectionParam").Name);
			AreEqual("Q indicators", Display("IndicatorsCollectionParam").Name);
			AreEqual("Q orders", Display("OrdersCollectionParam").Name);
			AreEqual("Q trades", Display("TradesCollectionParam").Name);

			var xAxes = Display("XAxisCollectionParam");

			AreEqual("Q x", xAxes.Name);
			AreEqual("Q axes", xAxes.GroupName);
			AreEqual("Q y", Display("YAxisCollectionParam").Name);
		}
		finally
		{
			LocalizedStrings.ActiveLanguage = previous;
			LocalizedStrings.RemoveLanguage(code);
			LocalizedStrings.ResetCache();
		}
	}

	/// <summary>
	/// The parameters an indicator element takes from its indicator are named in the interface's language, as its own
	/// are: a Russian property grid showed the period of an SMA as "Length".
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void AnIndicatorsParametersAreNamedInTheInterfacesLanguage()
	{
		const string code = "q4";

		var previous = LocalizedStrings.ActiveLanguage;

		try
		{
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.CultureCodeKey, "en-US" },
				{ LocalizedStrings.PeriodKey, "Q period" },
			});

			LocalizedStrings.ActiveLanguage = code;

			using var type = new IndicatorType(typeof(SimpleMovingAverage));
			var element = new IndicatorDiagramElement { Type = type };

			var length = element.Parameters.Single(param => param.Name == nameof(SimpleMovingAverage.Length));

			AreEqual("Q period", length.Attributes.OfType<DisplayAttribute>().Single().Name);
		}
		finally
		{
			LocalizedStrings.ActiveLanguage = previous;
			LocalizedStrings.RemoveLanguage(code);
			LocalizedStrings.ResetCache();
		}
	}

	private sealed class AsyncSettings : IAsyncPersistable
	{
		public int Number { get; set; }

		Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			Number = storage.GetValue<int>(nameof(Number));
			return Task.CompletedTask;
		}

		Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			storage.Set(nameof(Number), Number);
			return Task.CompletedTask;
		}
	}

	private async Task<T> RoundTripAsync<T>(T value)
	{
		var source = new DiagramElementParam<T> { Name = "value", Value = value };
		var storage = new SettingsStorage();
		await source.SaveAsync(storage, CancellationToken);

		var loaded = new DiagramElementParam<T> { Name = "value" };
		await loaded.LoadAsync(storage, CancellationToken);

		return loaded.Value;
	}

	[TestMethod]
	public async Task ValueThatPersistsItself_RoundTrips()
	{
		var restored = await RoundTripAsync(new AsyncSettings { Number = 5 });

		restored.AssertNotNull();
		restored.Number.AssertEqual(5);
	}

	[TestMethod]
	public async Task PlainValue_RoundTrips()
		=> (await RoundTripAsync(42)).AssertEqual(42);
}
