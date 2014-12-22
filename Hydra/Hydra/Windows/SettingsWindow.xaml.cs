namespace StockSharp.Hydra.Windows
{
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
	}
}