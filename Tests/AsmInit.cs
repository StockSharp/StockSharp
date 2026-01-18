namespace StockSharp.Tests;

using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Excel;
using Ecng.Data;

using Microsoft.Data.SqlClient;

using StockSharp.Algo.Compilation;

[TestClass]
public static class AsmInit
{
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
		await CompilationExtensions.Init(Paths.FileSystem, Helper.LogManager.Application, [("designer_extensions.py", File.ReadAllText("../../../../Diagram.Core/python/designer_extensions.py"))], default);

		ConfigManager.RegisterService<IDatabaseProvider>(new AdoDatabaseProvider());
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SqlServer, SqlClientFactory.Instance);

		Helper.FileSystem.ClearTemp();
	}

	[AssemblyCleanup]
	public static void UnInit()
	{
		Helper.FileSystem.ClearTemp();
	}
}