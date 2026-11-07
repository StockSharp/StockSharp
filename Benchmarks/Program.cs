namespace StockSharp.Benchmarks;

using BenchmarkDotNet.Running;

internal static class Program
{
	private static void Main(string[] args)
		=> BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
