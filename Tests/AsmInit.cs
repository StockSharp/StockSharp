namespace StockSharp.Tests;

using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Interop;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Export;

[TestClass]
public static class AsmInit
{
	[AssemblyInitialize]
	public static async Task Init(TestContext _)
	{
		ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
		ConfigManager.RegisterService<ISecurityProvider>(new CollectionSecurityProvider());
		ConfigManager.RegisterService<IExchangeInfoProvider>(new InMemoryExchangeInfoProvider());
		ConfigManager.RegisterService<IExcelWorkerProvider>(new DevExpExcelWorkerProvider());

		await CompilationExtensions.Init(Helper.LogManager.Application, [("designer_extensions.py", File.ReadAllText("../../../../Diagram.Core/python/designer_extensions.py"))], default);

		Helper.ClearTemp();
	}

	[AssemblyCleanup]
	public static void UnInit()
	{
		Helper.ClearTemp();
	}
}