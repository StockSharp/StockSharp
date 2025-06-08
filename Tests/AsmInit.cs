namespace StockSharp.Tests;

using Ecng.Compilation;
using Ecng.Compilation.Roslyn;

using Nito.AsyncEx;

using StockSharp.Algo.Compilation;

[TestClass]
public static class AsmInit
{
	[AssemblyInitialize]
	public static void Init(TestContext _)
	{
		ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
		ConfigManager.RegisterService<ISecurityProvider>(new CollectionSecurityProvider());
		ConfigManager.RegisterService<IExchangeInfoProvider>(new InMemoryExchangeInfoProvider());

		AsyncContext.Run(() => CompilationExtensions.Init(Helper.LogManager.Application, [("designer_extensions.py", File.ReadAllText("../../../../Diagram.Core/python/designer_extensions.py"))], default));

		Helper.ClearTemp();
	}

	[AssemblyCleanup]
	public static void UnInit()
	{
		Helper.ClearTemp();
	}
}