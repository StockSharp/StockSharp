namespace StockSharp.Tests;

using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Interop;

using Nito.AsyncEx;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Export;

[TestClass]
public static class AsmInit
{
	[AssemblyInitialize]
	public static void Init(TestContext _)
	{
		ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
		ConfigManager.RegisterService<ISecurityProvider>(new CollectionSecurityProvider());
		ConfigManager.RegisterService<IExchangeInfoProvider>(new InMemoryExchangeInfoProvider());
		ConfigManager.RegisterService<IExcelWorkerProvider>(new DevExpExcelWorkerProvider());

		AsyncContext.Run(() => CompilationExtensions.Init(Helper.LogManager.Application, [("designer_extensions.py", File.ReadAllText("../../../../Diagram.Core/python/designer_extensions.py"))], default));

		Helper.ClearTemp();
	}

	[AssemblyCleanup]
	public static void UnInit()
	{
		Helper.ClearTemp();
	}
}