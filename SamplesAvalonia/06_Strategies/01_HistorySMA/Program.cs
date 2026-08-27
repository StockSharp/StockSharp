namespace StockSharp.Samples.Strategies.HistorySMA.Avalonia;

using System;

using StockSharp.Samples.Avalonia;

internal static class Program
{
	[STAThread]
	public static int Main(string[] args)
		=> SampleApplication.Run<MainWindow>(args);
}
