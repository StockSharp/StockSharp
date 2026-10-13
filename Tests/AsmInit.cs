namespace StockSharp.Tests;

using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Excel;
using Ecng.Data;

using Microsoft.Data.SqlClient;

using StockSharp.Alerts;
using StockSharp.Algo.Compilation;

[TestClass]
public static class AsmInit
{
	// Shipped in the repository next to the Diagram.Core project. A run started from a copied output
	// folder has no repository above it, and therefore no such file.
	private const string _designerPythonExtensions = "../../../../Diagram.Core/python/designer_extensions.py";

	/// <summary>
	/// Why the scripting toolchain cannot be used on this machine, or <see langword="null"/> when it
	/// can. A machine that cannot host the toolchain says nothing about whether StockSharp is
	/// correct, so a test that needs it reports itself inconclusive with this reason instead of
	/// failing: a suite that shows a missing dependency as red teaches its readers to ignore red.
	/// </summary>
	public static string ScriptingUnavailableReason { get; private set; }

	/// <summary>
	/// Why the sample history cannot be used on this machine, or <see langword="null"/> when it can.
	/// The month of real market data every backtest replays arrives as the StockSharp.Samples.HistoryData
	/// package rather than with the sources, so a machine that never restored it can say nothing about
	/// whether the backtester is correct. A test that needs it reports itself inconclusive with this
	/// reason: failing blames the product for an absent package, and returning quietly is worse still,
	/// because the run is then counted as one that covered the backtest.
	/// </summary>
	public static string SampleHistoryUnavailableReason
		=> Paths.HistoryDataPath is null
			? "The StockSharp.Samples.HistoryData package was not found in the NuGet packages folder: this run has no sample history to replay."
			: null;

	[AssemblyInitialize]
	public static async Task Init(TestContext _)
	{
		ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
		var secProvider = new CollectionSecurityProvider();
		ConfigManager.RegisterService<ISecurityProvider>(secProvider);
		ConfigManager.RegisterService<ISecurityStorage>(new InMemorySecurityStorage(secProvider));
		ConfigManager.RegisterService<IExchangeInfoProvider>(new InMemoryExchangeInfoProvider());
		ConfigManager.RegisterService<IExcelWorkerProvider>(new OpenXmlExcelWorkerProvider());
		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider([], typeof(MockRemoteAdapter)));
		// A diagram socket asks for a dispatcher as soon as an element is built; outside an
		// app there is no UI thread to marshal to, and the dummy one runs callbacks inline.
		ConfigManager.RegisterService<IDispatcher>(new DummyDispatcher());

		// An indicator element resolves the indicator it was saved with through this provider as it
		// loads; without one it comes back with nothing behind it.
		var indicators = new IndicatorProvider();
		indicators.Init();
		ConfigManager.RegisterService<IIndicatorProvider>(indicators);

		// An alert is delivered through whatever transport is registered here at the moment it fires.
		// A test that counts deliveries puts its own in this place and restores this one afterwards.
		ConfigManager.RegisterService<IAlertNotificationService>(new SilentAlertNotificationService());

		// The extensions are handed over only when they are on disk. Their absence disables the
		// scripting tests, and it must not take the rest of the assembly with it - which is exactly
		// what an exception out of an assembly initializer does to every test in the run.
		var extraPythonCommon = new List<(string name, string body)>();

		if (File.Exists(_designerPythonExtensions))
			extraPythonCommon.Add(("designer_extensions.py", File.ReadAllText(_designerPythonExtensions)));
		else
			ScriptingUnavailableReason = $"The designer's Python extensions were not found at '{_designerPythonExtensions}': this run has no repository to take scripts from.";

		await CompilationExtensions.Init(Paths.FileSystem, Helper.LogManager.Application, extraPythonCommon, default);

		ConfigManager.RegisterService<IDatabaseProvider>(new AdoDatabaseProvider());

		SqlServerDialect.Register(SqlClientFactory.Instance);

		Helper.FileSystem.ClearTemp();
	}

	[AssemblyCleanup]
	public static void UnInit()
	{
		Helper.FileSystem.ClearTemp();
	}

	// Takes every alert and does nothing with it, so the transport is always there to resolve.
	private class SilentAlertNotificationService : BaseLogReceiver, IAlertNotificationService
	{
		ValueTask IAlertNotificationService.NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time, CancellationToken cancellationToken)
			=> default;
	}
}