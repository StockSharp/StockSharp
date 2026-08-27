namespace StockSharp.Samples.Basic.ConnectAndDownloadInstruments.Avalonia;

using System;

using StockSharp.Samples.Avalonia;

internal static class Program
{
	[STAThread]
	public static int Main(string[] args)
		=> SampleApplication.Run<MainWindow>(args);
}
