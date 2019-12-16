namespace SampleConnection
{
	using System;

	public partial class DepthSettingsWindow
	{
		public DepthSettingsWindow()
		{
			InitializeComponent();
			Settings = new DepthSettings();
		}

		public DepthSettings Settings
		{
			get => (DepthSettings)SettingsGrid.SelectedObject;
			set => SettingsGrid.SelectedObject = value ?? throw new ArgumentNullException(nameof(value));
		}
	}
}