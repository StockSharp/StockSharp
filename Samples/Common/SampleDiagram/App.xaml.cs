namespace SampleDiagram
{
	using Ecng.Configuration;

	using StockSharp.Alerts;

	public partial class App
	{
		public App()
		{
			//TODO remove after fix for loading elements from config
			ConfigManager.RegisterService<IAlertService>(new AlertService(".\\"));
		}
	}
}