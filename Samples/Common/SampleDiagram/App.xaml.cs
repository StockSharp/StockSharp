namespace SampleDiagramPublic
{
	using Ecng.Configuration;

	using StockSharp.Alerts;

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		public App()
		{
			//TODO remove after fix for loading elements from config
			ConfigManager.RegisterService<IAlertService>(new AlertService(".\\"));
		}
	}
}
