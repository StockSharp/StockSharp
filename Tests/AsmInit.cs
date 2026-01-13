namespace StockSharp.Tests;

using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Excel;
#if NET10_0_OR_GREATER
using Ecng.Data;

using Microsoft.Data.SqlClient;
#endif

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
		await CompilationExtensions.Init(Paths.FileSystem, Helper.LogManager.Application, [("designer_extensions.py", File.ReadAllText("../../../../Diagram.Core/python/designer_extensions.py"))], default);

#if NET10_0_OR_GREATER
		ConfigManager.RegisterService<IDatabaseProvider>(new AdoDatabaseProvider());
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SqlServer, SqlClientFactory.Instance);
#endif

		Helper.FileSystem.ClearTemp();
	}

	[AssemblyCleanup]
	public static void UnInit()
	{
		Helper.FileSystem.ClearTemp();
	}
}