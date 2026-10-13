namespace StockSharp.Samples.Advanced.MultiConnect;

using System;

using StockSharp.Samples;

internal static class Program
{
	[STAThread]
	public static int Main(string[] args)
		=> SampleApplication.Run<MainWindow>(args);
}
