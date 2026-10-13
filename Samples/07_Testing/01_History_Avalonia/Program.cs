namespace StockSharp.Samples.Testing.History;

using System;

using StockSharp.Samples;

internal static class Program
{
	[STAThread]
	public static int Main(string[] args)
		=> SampleApplication.Run<MainWindow>(args);
}
