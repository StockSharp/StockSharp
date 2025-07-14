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
public class CompilationTests
{
	private static readonly string _analyticsFolder = "../../../../Algo.Analytics.{0}";

	[TestMethod]
	public Task CSharpAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("CSharp"), FileExts.CSharp, default);

	[TestMethod]
	public Task FSharpAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("FSharp"), FileExts.FSharp, default);

	[TestMethod]
	public Task PythonAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("Python"), FileExts.Python, default);

	private static async Task TestAnalyticsScripts(string folderPath, string fileExtension, CancellationToken token)
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[fileExtension];
		var context = compiler.CreateContext();

		var usings = fileExtension == FileExts.CSharp
			? await File.ReadAllTextAsync(Path.Combine(folderPath, "Properties", "usings.cs"), token)
			: null;

		// Get all script files in the folder
		var scriptFiles = Directory.GetFiles(folderPath, $"*{fileExtension}");
		scriptFiles.Length.AssertGreater(0); // Ensure there are scripts to test

		var securities = new[]
		{
			"EUR/USD@DUKAS".ToSecurityId(),
			"EUR/AUD@DUKAS".ToSecurityId(),
			"GBP/AUD@DUKAS".ToSecurityId(),
		};
		var from = new DateTime(2025, 4, 1).UtcKind();
		var to = new DateTime(2025, 4, 30).UtcKind();
		var storageRegistry = Helper.GetResourceStorage();
		var format = StorageFormats.Binary;
		var timeFrame = TimeSpan.FromMinutes(1);

		var references = CodeExtensions.DefaultReferences
			.Concat(CodeExtensions.CreateAssemblyReferences(
			[
				"StockSharp.Algo.Analytics",
				"MathNet.Numerics"
			]));

		if (fileExtension == FileExts.FSharp)
			references = references.Concat(CodeExtensions.FSharpReferences);
		
		var refs = (await references.ToValidRefImages(token)).ToArray();

		foreach (var scriptFile in scriptFiles)
		{
			if (Path.GetFileNameWithoutExtension(scriptFile).StartsWithIgnoreCase("empty"))
				continue;

			var sourceCode = await File.ReadAllTextAsync(scriptFile, token);
			var scriptName = Path.GetFileNameWithoutExtension(scriptFile);

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
	}

	private static async Task RunAnalyticsScript(IAnalyticsScript script, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken token)
	{
		// Create a test panel to capture output
		var testPanel = new TestAnalyticsPanel();

		var (_, t) = token.CreateChildToken(TimeSpan.FromSeconds(30)); // 30 second timeout

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
			timeFrame,
			t
		);

		// Verify that the script produced some output
		testPanel.VerifyOutputProduced();
	}

	// Test implementation of IAnalyticsPanel to verify script execution
	private class TestAnalyticsPanel : IAnalyticsPanel
	{
		private bool _gridCreated;
		private bool _chartCreated;
		private bool _heatmapCreated;
		private bool _chart3dCreated;

		public IAnalyticsGrid CreateGrid(params string[] columns)
		{
			columns.AssertNotNull();
			columns.Length.AssertGreater(0);

			_gridCreated = true;
			return new TestAnalyticsGrid(columns);
		}

		public IAnalyticsChart<X, Y, Z> CreateChart<X, Y, Z>()
		{
			_chartCreated = true;
			return new TestAnalyticsChart<X, Y, Z>();
		}

		public IAnalyticsChart<X, Y, VoidType> CreateChart<X, Y>()
		{
			_chartCreated = true;
			return new TestAnalyticsChart<X, Y, VoidType>();
		}

		public void DrawHeatmap(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data)
		{
			xTitles.AssertNotNull();
			yTitles.AssertNotNull();
			data.AssertNotNull();

			_heatmapCreated = true;
		}

		public void Draw3D(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data, string xTitle, string yTitle, string zTitle)
		{
			xTitles.AssertNotNull();
			yTitles.AssertNotNull();
			data.AssertNotNull();

			_chart3dCreated = true;
		}

		public void VerifyOutputProduced()
		{
			// At least one type of output should have been produced
			(_gridCreated || _chartCreated || _heatmapCreated || _chart3dCreated).AssertTrue();
		}

		private class TestAnalyticsGrid(string[] columns) : IAnalyticsGrid
		{
			private readonly List<object[]> _rows = [];

			public void SetSort(string column, bool asc)
			{
				column.IsEmpty().AssertFalse();
				columns.Contains(column, StringComparer.OrdinalIgnoreCase).AssertTrue();
			}

			public void SetRow(params object[] row)
			{
				row.AssertNotNull();
				row.Length.AssertEqual(columns.Length);
				_rows.Add(row);
			}

			public int RowCount => _rows.Count;
		}

		private class TestAnalyticsChart<X, Y, Z> : IAnalyticsChart<X, Y, Z>
		{
			private int _seriesCount;

			public void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, DrawStyles style, Color? color)
			{
				title.IsEmpty().AssertFalse();
				xValues.AssertNotNull();
				yValues.AssertNotNull();
				_seriesCount++;
			}

			public void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, IEnumerable<Z> zValues, DrawStyles style, Color? color)
			{
				title.IsEmpty().AssertFalse();
				xValues.AssertNotNull();
				yValues.AssertNotNull();
				zValues.AssertNotNull();
				_seriesCount++;
			}

			public int SeriesCount => _seriesCount;
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
		var evts = type.GetEvents().ToArray();
		evts.Any().AssertTrue();

		var raisedCnt = 0;

		foreach (var evt in evts)
		{
			var handlerType = evt.EventHandlerType;
			var isFSharp = handlerType.IsFSharpHandler();

			if (handlerType != typeof(Action<Unit>) && !isFSharp)
				continue;

			var evtAttrs = evt.GetAttributes().ToArray();
			evtAttrs.Any(a => a is DiagramExternalAttribute).AssertTrue();

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
		methods.Any().AssertTrue();

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

	private static async Task CSharpCompile<T>(string fileName, Action<Type, T> custom = null)
		where T : IPersistable
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.CSharp];

		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));

		var res = await compiler.Compile("test", [sourceCode], await CodeExtensions.DefaultReferences.ToValidRefImages(default), default);
		Validate(res);

		var type = res.GetAssembly(compiler.CreateContext()).GetExportedTypes().First();
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

	private static async Task FSharpCompile<T>(string fileName, Action<Type, T> custom = null)
		where T : IPersistable
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.FSharp];

		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));

		var fsharpRefs = CodeExtensions.FSharpReferences;

		var res = await compiler.Compile("test", [sourceCode], await CodeExtensions.DefaultReferences.Concat(fsharpRefs).ToValidRefImages(default), default);
		Validate(res);

		var type = res.GetAssembly(compiler.CreateContext()).GetExportedTypes().First();
		type.IsRequiredType<T>().AssertTrue();
		
		var s = type.CreateInstance<T>();

		s.AssertNotNull();
		s.Load(s.Save());

		custom?.Invoke(type, s);
	}

	[TestMethod]
	public Task PythonEmptyStrategy()
		=> PythonCompile<Strategy>("Backtest/empty_strategy.py");

	[TestMethod]
	public Task PythonSmaStrategy()
		=> PythonCompile<Strategy>("Backtest/sma_strategy.py");

	[TestMethod]
	public Task PythonIndicator()
		=> PythonCompile<IIndicator>("Indicator/empty_indicator.py");

	[TestMethod]
	public Task PythonDiagramElem()
		=> PythonCompile<DiagramExternalElement>("Custom/empty_diagram_element.py", InvokeDiagramElem);

	private static async Task PythonCompile<T>(string fileName, Action<Type, T> custom = null)
		where T : IPersistable
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.Python];

		var context = compiler.CreateContext();
		
		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));
		
		var res = await compiler.Compile(typeof(T).Name, [sourceCode], await CodeExtensions.DefaultReferences.ToValidRefImages(default), default);

		Validate(res);

		var asm = res.GetAssembly(context);

		if (asm is null)
			return;

		var types = asm.GetExportedTypes();

		res = await compiler.Compile(typeof(T).Name, [sourceCode], await CodeExtensions.DefaultReferences.ToValidRefImages(default), default);
		asm = res.GetAssembly(context);
		var types2 = asm.GetExportedTypes().First();

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
			s.Connector = new();

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