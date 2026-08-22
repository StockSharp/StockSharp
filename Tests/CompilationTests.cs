namespace StockSharp.Tests;

using System.ComponentModel;
using System.Drawing;

using Ecng.Compilation;
using Ecng.Drawing;
using Ecng.Reflection;

using IronPython.Runtime.Types;

using Microsoft.FSharp.Control;

using StockSharp.Algo.Analytics;
using StockSharp.Algo.Compilation;
using StockSharp.Diagram;

[TestClass]
public class CompilationTests : BaseTestClass
{
	private static readonly string _analyticsFolder = "../../../../Algo.Analytics.{0}";
	private static readonly (string name, byte[] body)[] _noReferenceImages = [];

	private static readonly ReferenceImageCache _defaultReferenceImages = new(static () => CodeExtensions.DefaultReferences);
	private static readonly ReferenceImageCache _analyticsReferenceImages = new(static () => CodeExtensions.CreateAssemblyReferences(
	[
		"StockSharp.Algo.Analytics",
		"MathNet.Numerics"
	]));
	private static readonly ReferenceImageCache _fSharpReferenceImages = new(static () => CodeExtensions.FSharpReferences);

	private sealed class ReferenceImageCache(Func<IEnumerable<AssemblyReference>> getReferences)
	{
		private static readonly Task<(string name, byte[] body)[]> _emptyImages = Task.FromResult(Array.Empty<(string name, byte[] body)>());

		private readonly Lock _sync = new();
		private Task<(string name, byte[] body)[]> _images = _emptyImages;
		private bool _isInitialized;

		public async Task<(string name, byte[] body)[]> GetImages(CancellationToken token)
		{
			Task<(string name, byte[] body)[]> images;
			var observeFailure = false;

			using (_sync.EnterScope())
			{
				if (!_isInitialized)
				{
					_images = LoadImages(getReferences(), token);
					_isInitialized = true;
					observeFailure = true;
				}

				images = _images;
			}

			if (observeFailure)
				_ = ResetOnFailure(images);

			return await images.WaitAsync(token);
		}

		private async Task ResetOnFailure(Task<(string name, byte[] body)[]> images)
		{
			try
			{
				await images;
			}
			catch
			{
				using (_sync.EnterScope())
				{
					if (!ReferenceEquals(_images, images))
						return;

					_images = _emptyImages;
					_isInitialized = false;
				}
			}
		}

		private static async Task<(string name, byte[] body)[]> LoadImages(IEnumerable<AssemblyReference> references, CancellationToken token)
			=> (await references.ToValidRefImages(token)).ToArray();
	}

	private static async Task<(string name, byte[] body)[]> GetReferenceImages(bool includeAnalytics, bool includeFSharp, CancellationToken token)
	{
		var defaultReferences = await _defaultReferenceImages.GetImages(token);

		if (!includeAnalytics && !includeFSharp)
			return defaultReferences;

		var analyticsReferences = Array.Empty<(string name, byte[] body)>();
		var fSharpReferences = Array.Empty<(string name, byte[] body)>();

		if (includeAnalytics)
			analyticsReferences = await _analyticsReferenceImages.GetImages(token);

		if (includeFSharp)
			fSharpReferences = await _fSharpReferenceImages.GetImages(token);

		return [.. defaultReferences, .. analyticsReferences, .. fSharpReferences];
	}

	[TestMethod]
	public Task CSharpAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("CSharp"), FileExts.CSharp, CancellationToken);

	[TestMethod]
	public Task FSharpAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("FSharp"), FileExts.FSharp, CancellationToken);

	[TestMethod]
	public Task PythonAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("Python"), FileExts.Python, CancellationToken);

	[TestMethod]
	public Task PythonAnalyticsScriptsParallel() => TestAnalyticsScriptsParallel(_analyticsFolder.Put("Python"), FileExts.Python, CancellationToken);

	private static async Task TestAnalyticsScriptsParallel(string folderPath, string fileExtension, CancellationToken token)
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[fileExtension];

		// Get all script files in the folder
		var scriptFiles = Directory.GetFiles(folderPath, $"*{fileExtension}");
		(scriptFiles.Length > 0).AssertTrue("Ensure there are scripts to test");

		var securities = new[]
		{
			"EUR/USD@DUKAS".ToSecurityId(),
			"EUR/AUD@DUKAS".ToSecurityId(),
			"GBP/AUD@DUKAS".ToSecurityId(),
		};
		var from = new DateTime(2025, 4, 1).UtcKind();
		var to = GetPeriodEnd(fileExtension);
		var storageRegistry = Helper.GetResourceStorage();
		var format = StorageFormats.Binary;
		var timeFrame = TimeSpan.FromMinutes(1).TimeFrame();

		await EnsureCandlesExist(securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);

		var refs = compiler.IsReferencesSupported
			? await GetReferenceImages(true, fileExtension == FileExts.FSharp, token)
			: _noReferenceImages;

		// Run all compile-and-execute pipelines concurrently. The compiler owns any
		// synchronization required by its underlying script engine.
		var tasks = scriptFiles.Select(async scriptFile =>
		{
			var scriptName = Path.GetFileNameWithoutExtension(scriptFile);

			try
			{
				if (scriptName.StartsWithIgnoreCase("empty"))
					return;

				var sourceCode = await File.ReadAllTextAsync(scriptFile, token);

				// Compile the script
				var sources = new string[] { sourceCode };

				var res = await compiler.Compile(scriptName, sources, refs, token);

				Validate(res);

				using var context = compiler.CreateContext();
				var assembly = res.GetAssembly(context);
				assembly.AssertNotNull();

				var analyticsScriptType = assembly.GetExportedTypes().First(t => t.IsRequiredType<IAnalyticsScript>());
				var script = analyticsScriptType.CreateInstance<IAnalyticsScript>();
				script.AssertNotNull();

				await RunAnalyticsScript(script, securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Error running script '{scriptName}'.", ex);
			}
		});

		await Task.WhenAll(tasks);
	}

	private static async Task TestAnalyticsScripts(string folderPath, string fileExtension, CancellationToken token)
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[fileExtension];

		var usings = fileExtension == FileExts.CSharp
			? await File.ReadAllTextAsync(Path.Combine(folderPath, "Properties", "usings.cs"), token)
			: null;

		// Get all script files in the folder
		var scriptFiles = Directory.GetFiles(folderPath, $"*{fileExtension}");
		(scriptFiles.Length > 0).AssertTrue("Ensure there are scripts to test");

		var securities = new[]
		{
			"EUR/USD@DUKAS".ToSecurityId(),
			"EUR/AUD@DUKAS".ToSecurityId(),
			"GBP/AUD@DUKAS".ToSecurityId(),
		};
		var from = new DateTime(2025, 4, 1).UtcKind();
		var to = GetPeriodEnd(fileExtension);
		var storageRegistry = Helper.GetResourceStorage();
		var format = StorageFormats.Binary;
		var timeFrame = TimeSpan.FromMinutes(1).TimeFrame();

		await EnsureCandlesExist(securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);

		var refs = compiler.IsReferencesSupported
			? await GetReferenceImages(true, fileExtension == FileExts.FSharp, token)
			: _noReferenceImages;

		foreach (var scriptFile in scriptFiles)
		{
			var scriptName = Path.GetFileNameWithoutExtension(scriptFile);

			try
			{
				if (scriptName.StartsWithIgnoreCase("empty"))
					continue;

				var sourceCode = await File.ReadAllTextAsync(scriptFile, token);

				// Compile the script

				var sources = new string[] { sourceCode };

				if (usings is not null)
					sources = sources.Concat([usings]);

				var res = await compiler.Compile(
					scriptName,
					sources,
					refs,
					token);

				Validate(res);

				using var context = compiler.CreateContext();
				var assembly = res.GetAssembly(context);
				assembly.AssertNotNull();

				var types = assembly.GetExportedTypes();
				var analyticsScriptType = types.First(t => t.IsRequiredType<IAnalyticsScript>());

				// Create an instance of the script
				var script = analyticsScriptType.CreateInstance<IAnalyticsScript>();
				script.AssertNotNull();

				// Test script execution with mock data
				await RunAnalyticsScript(script, securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Error running script '{scriptName}'.", ex);
			}
		}
	}

	// IronPython interprets every candle (~24 us against ~1.9 us for compiled C#/F#), so a whole
	// month of 1-min candles costs seconds per script there while nothing the scripts are checked
	// for (VerifyOutputProduced) depends on the data volume. Python scripts therefore get a few
	// days only; compiled languages keep the original month.
	private static DateTime GetPeriodEnd(string fileExtension)
		=> (fileExtension == FileExts.Python ? new DateTime(2025, 4, 5) : new DateTime(2025, 4, 30)).UtcKind();

	// Guard for the language dependent period (see GetPeriodEnd): if the storage has no candles
	// inside the window, scripts produce no output at all and the failure would surface far away
	// from its cause. Fail here instead, naming the security and the empty range.
	private static async Task EnsureCandlesExist(SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken token)
	{
		foreach (var security in securities)
		{
			var dates = await storage.GetCandleMessageStorage(security, dataType, drive, format).GetDatesAsync(from, to).ToArrayAsync(token);
			(dates.Length > 0).AssertTrue($"No {dataType} data for {security} in {from:yyyy-MM-dd}..{to:yyyy-MM-dd}.");
		}
	}

	private static async Task RunAnalyticsScript(IAnalyticsScript script, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken token)
	{
		// Create a test panel to capture output
		var testPanel = new TestAnalyticsPanel();

		var (_, t) = token.CreateChildToken(TimeSpan.FromSeconds(60)); // 60 second timeout

		// Execute the script
		await script.Run(
			Helper.LogManager.Application,
			testPanel,
			securities,
			from,
			to,
			storage,
			drive,
			format,
			dataType,
			t
		);

		// Verify that the script produced some output
		testPanel.VerifyOutputProduced();
	}

	// Test implementation of IAnalyticsPanel to verify script execution
	private class TestAnalyticsPanel : IAnalyticsPanel
	{
		private readonly List<TestAnalyticsGrid> _grids = [];
		private readonly List<ITestAnalyticsChart> _charts = [];
		private bool _heatmapHasData;
		private bool _chart3dHasData;

		public IAnalyticsGrid CreateGrid(params string[] columns)
		{
			columns.AssertNotNull();
			(columns.Length > 0).AssertTrue("columns must not be empty");

			var grid = new TestAnalyticsGrid(columns);
			_grids.Add(grid);
			return grid;
		}

		public IAnalyticsChart<X, Y, Z> CreateChart<X, Y, Z>()
		{
			var chart = new TestAnalyticsChart<X, Y, Z>();
			_charts.Add(chart);
			return chart;
		}

		public IAnalyticsChart<X, Y, VoidType> CreateChart<X, Y>()
		{
			var chart = new TestAnalyticsChart<X, Y, VoidType>();
			_charts.Add(chart);
			return chart;
		}

		public void DrawHeatmap(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data)
		{
			xTitles.AssertNotNull();
			yTitles.AssertNotNull();
			data.AssertNotNull();

			// Check that data has actual content
			var hasData = data.GetLength(0) > 0 && data.GetLength(1) > 0;
			if (hasData)
				_heatmapHasData = true;
		}

		public void Draw3D(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data, string xTitle, string yTitle, string zTitle)
		{
			xTitles.AssertNotNull();
			yTitles.AssertNotNull();
			data.AssertNotNull();

			// Check that data has actual content
			var hasData = data.GetLength(0) > 0 && data.GetLength(1) > 0;
			if (hasData)
				_chart3dHasData = true;
		}

		public void VerifyOutputProduced()
		{
			// Check grids have rows
			var gridsHaveData = _grids.Count > 0 && _grids.Any(g => g.RowCount > 0);

			// Check charts have series with data points
			var chartsHaveData = _charts.Count > 0 && _charts.Any(c => c.SeriesCount > 0 && c.TotalDataPoints > 0);

			// At least one type of output should have been produced with actual data
			(gridsHaveData || chartsHaveData || _heatmapHasData || _chart3dHasData).AssertTrue();
		}

		private interface ITestAnalyticsChart
		{
			int SeriesCount { get; }
			int TotalDataPoints { get; }
		}

		private class TestAnalyticsGrid(string[] columns) : IAnalyticsGrid
		{
			private readonly List<object[]> _rows = [];

			public void SetSort(string column, bool asc)
			{
				column.IsEmpty().AssertFalse();
				columns.Count(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase)).AssertEqual(1);
			}

			public void SetRow(params object[] row)
			{
				row.AssertNotNull();
				row.Length.AssertEqual(columns.Length);
				_rows.Add(row);
			}

			public int RowCount => _rows.Count;
		}

		private class TestAnalyticsChart<X, Y, Z> : IAnalyticsChart<X, Y, Z>, ITestAnalyticsChart
		{
			private int _seriesCount;
			private int _totalDataPoints;

			public void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, DrawStyles style, Color? color)
			{
				title.IsEmpty().AssertFalse();
				xValues.AssertNotNull();
				yValues.AssertNotNull();

				// Count actual data points
				var xCount = xValues.Count();
				var yCount = yValues.Count();
				xCount.AssertEqual(yCount);

				_totalDataPoints += xCount;
				_seriesCount++;
			}

			public void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, IEnumerable<Z> zValues, DrawStyles style, Color? color)
			{
				title.IsEmpty().AssertFalse();
				xValues.AssertNotNull();
				yValues.AssertNotNull();
				zValues.AssertNotNull();

				// Count actual data points
				var xCount = xValues.Count();
				var yCount = yValues.Count();
				var zCount = zValues.Count();
				xCount.AssertEqual(yCount);
				xCount.AssertEqual(zCount);

				_totalDataPoints += xCount;
				_seriesCount++;
			}

			public int SeriesCount => _seriesCount;
			public int TotalDataPoints => _totalDataPoints;
		}
	}

	private static readonly string _designerFolder = "../../../../Designer.Templates/";

	private static void Validate(CompilationResult res)
	{
		ArgumentNullException.ThrowIfNull(res);

		foreach (var e in res.Errors)
		{
			if (e.Type == CompilationErrorTypes.Error)
				throw new InvalidOperationException(e.ToString());
		}
	}

	private static List<PropertyDescriptor> GetBrowsableProperties(ICustomTypeDescriptor customTypeDescriptor)
	{
		if (customTypeDescriptor == null)
			throw new ArgumentNullException(nameof(customTypeDescriptor));

		var allProperties = customTypeDescriptor.GetProperties();

		List<PropertyDescriptor> browsableProperties = [];

		foreach (PropertyDescriptor prop in allProperties)
		{
			if (prop.Attributes[typeof(BrowsableAttribute)] is not BrowsableAttribute browsableAttr ||
				!browsableAttr.Browsable)
				continue;

			if (prop.Attributes[typeof(EditorBrowsableAttribute)] is EditorBrowsableAttribute editorBrowsableAttr &&
				editorBrowsableAttr.State == EditorBrowsableState.Never)
				continue;

			if (prop.Attributes[typeof(DesignOnlyAttribute)] is DesignOnlyAttribute designOnlyAttr &&
				designOnlyAttr.IsDesignOnly)
				continue;

			browsableProperties.Add(prop);
		}

		return browsableProperties;
	}

	private void InvokeDiagramElem(Type type, DiagramExternalElement instance)
	{
		var evts = type.GetEvents()
			.Where(DiagramExternalAttribute.IsExternal)
			.ToArray();
		evts.Length.AssertEqual(2);

		var raisedCnt = 0;

		foreach (var evt in evts)
		{
			var handlerType = evt.EventHandlerType;
			var isFSharp = handlerType.IsFSharpHandler();

			if (handlerType != typeof(Action<Unit>) && !isFSharp)
				continue;

			var evtAttrs = evt.GetAttributes().ToArray();
			evtAttrs.Count(a => a is DiagramExternalAttribute).AssertEqual(1);

			Delegate dlg;

			if (isFSharp)
			{
				FSharpHandler<Unit> handler = (s, value) => raisedCnt++;
				dlg = handler;
			}
			else
			{
				Action<Unit> handler = value => raisedCnt++;
				dlg = handler;
			}

			evt.AddEventHandler(instance, dlg);
			evt.RemoveEventHandler(instance, dlg);

			evt.AddEventHandler(instance, dlg);
			evt.AddEventHandler(instance, dlg);
		}

		var methods = type.GetMethods().ToArray();
		methods.Count(m => m.Name == "Process").AssertEqual(1);

		foreach (var method in methods)
		{
			if (method.Name != "Process")
				continue;

			var methodAttrs = method.GetAttributes().ToArray();

			method.Invoke(instance,
			[
				new TimeFrameCandleMessage { ClosePrice = 100 },
			(Unit)10,
		]);
		}

		raisedCnt.AssertEqual(2);
	}

	[TestMethod]
	public Task CSharpEmptyStrategy()
		=> CSharpCompile<Strategy>("Backtest/EmptyStrategy.cs");

	[TestMethod]
	public Task CSharpSmaStrategy()
		=> CSharpCompile<Strategy>("Backtest/SmaStrategy.cs");

	[TestMethod]
	public Task CSharpIndicator()
		=> CSharpCompile<IIndicator>("Indicator/EmptyIndicator.cs");

	[TestMethod]
	public Task CSharpDiagramElem()
		=> CSharpCompile<DiagramExternalElement>("Custom/EmptyDiagramElement.cs", InvokeDiagramElem);

	private async Task CSharpCompile<T>(string fileName, Action<Type, T> custom = null)
		where T : IPersistable
	{
		var token = CancellationToken;

		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.CSharp];

		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));

		var res = await compiler.Compile("test", [sourceCode], await GetReferenceImages(false, false, token), token);
		Validate(res);

		using var context = compiler.CreateContext();
		var type = res.GetAssembly(context).GetExportedTypes().First();
		type.IsRequiredType<T>().AssertTrue();
		
		var s = type.CreateInstance<T>();
		
		s.AssertNotNull();
		s.Load(s.Save());

		custom?.Invoke(type, s);
	}

	[TestMethod]
	public Task FSharpEmptyStrategy()
		=> FSharpCompile<Strategy>("Backtest/EmptyStrategy.fs");

	[TestMethod]
	public Task FSharpSmaStrategy()
		=> FSharpCompile<Strategy>("Backtest/SmaStrategy.fs");

	[TestMethod]
	public Task FSharpIndicator()
		=> FSharpCompile<IIndicator>("Indicator/EmptyIndicator.fs");

	[TestMethod]
	public Task FSharpDiagramElem()
		=> FSharpCompile<DiagramExternalElement>("Custom/EmptyDiagramElement.fs", InvokeDiagramElem);

	private async Task FSharpCompile<T>(string fileName, Action<Type, T> custom = null)
		where T : IPersistable
	{
		var token = CancellationToken;

		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.FSharp];

		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));

		var res = await compiler.Compile("test", [sourceCode], await GetReferenceImages(false, true, token), token);
		Validate(res);

		using var context = compiler.CreateContext();
		var type = res.GetAssembly(context).GetExportedTypes().First();
		type.IsRequiredType<T>().AssertTrue();
		
		var s = type.CreateInstance<T>();

		s.AssertNotNull();
		s.Load(s.Save());

		custom?.Invoke(type, s);
	}

	// Recompiling the same module into the same context is a compiler level check (see the
	// recompile branch in PythonCompile), so it runs once here instead of in every template test.
	[TestMethod]
	public Task PythonEmptyStrategy()
		=> PythonCompile<Strategy>("Backtest/empty_strategy.py", true);

	[TestMethod]
	public Task PythonSmaStrategy()
		=> PythonCompile<Strategy>("Backtest/sma_strategy.py", false);

	[TestMethod]
	public Task PythonIndicator()
		=> PythonCompile<IIndicator>("Indicator/empty_indicator.py", false);

	[TestMethod]
	public Task PythonDiagramElem()
		=> PythonCompile<DiagramExternalElement>("Custom/empty_diagram_element.py", false, InvokeDiagramElem);

	private async Task PythonCompile<T>(string fileName, bool recompile, Action<Type, T> custom = null)
		where T : IPersistable
	{
		var token = CancellationToken;

		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.Python];

		using var context = compiler.CreateContext();
		
		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));
		
		var res = await compiler.Compile(typeof(T).Name, [sourceCode], _noReferenceImages, token);

		Validate(res);

		var asm = res.GetAssembly(context);
		asm.AssertNotNull();

		var types = asm.GetExportedTypes();

		if (recompile)
		{
			// Compiling the same module name into the same context again must succeed
			// and expose the same type, not clash with the already loaded module.
			var res2 = await compiler.Compile(typeof(T).Name, [sourceCode], _noReferenceImages, token);

			Validate(res2);

			var asm2 = res2.GetAssembly(context);
			asm2.AssertNotNull();

			asm2.GetExportedTypes().Any(t => t.IsRequiredType<T>()).AssertTrue("recompiled module must expose the same type");
		}

		var arrs = types.Where(t => t.IsRequiredType<T>());
		var type = arrs.First();
		var ns = type.Namespace;
		var fn = type.FullName;

		var attrs = type.GetAttributes().ToArray();
		var docUrl = type.GetDocUrl();

		type.IsRequiredType<T>().AssertTrue();

		var name = type.GetDisplayName();
		var desc = type.GetDescription();
		var iconUri = type.GetIconUrl();

		var instance = TypeHelper.CreateInstance<T>(type);

		if (instance is Strategy s)
			s.Connector = new Connector();

		var props = GetBrowsableProperties((ICustomTypeDescriptor)instance);

		var descriptor = TypeDescriptor.GetProvider(instance).GetTypeDescriptor(instance);

		(instance is IPythonObject).AssertTrue();

		var pythonClass = type.CreateInstance<T>();

		var properties = type.GetProperties().ToArray();
		var modifiableProperties = properties.Where(p => p.IsBrowsable() && p.IsModifiable()).ToArray();
		//foreach (var prop in modifiableProperties)
		//{
		//	Console.WriteLine($"{prop.Name}={prop.PropertyType}");
		//	Console.WriteLine(prop.GetValue(pythonClass));
		//	Console.WriteLine();
		//}

		custom?.Invoke(type, instance);

		pythonClass.Load(pythonClass.Save());
	}
}
