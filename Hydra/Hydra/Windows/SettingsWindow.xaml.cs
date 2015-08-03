namespace StockSharp.Hydra.Windows
{
	using System.Diagnostics;
	using System.Windows;

	using StockSharp.Hydra.Core;

	public partial class SettingsWindow
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		public HydraSettingsRegistry Settings
		{
			get { return (HydraSettingsRegistry)SettingsGrid.SelectedObject; }
			set { SettingsGrid.SelectedObject = value; }
		}

		private void Help_OnClick(object sender, RoutedEventArgs e)
		{
			Process.Start("http://stocksharp.com/doc/html/7d845e99-6bde-437e-b7f4-059be0438894.htm");
		}
	}
}