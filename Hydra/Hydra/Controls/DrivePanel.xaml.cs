namespace StockSharp.Hydra.Controls
{
	using System;
	using System.Windows.Controls;

	using Ecng.Common;

	using StockSharp.Algo.Storages;

	public partial class DrivePanel
	{
		public DrivePanel()
		{
			InitializeComponent();
		}

		public IMarketDataDrive SelectedDrive
		{
			get { return DriveCtrl.SelectedDrive; }
			set { DriveCtrl.SelectedDrive = value; }
		}

		public StorageFormats StorageFormat
		{
			get { return FormatCtrl.SelectedFormat; }
			set { FormatCtrl.SelectedFormat = value; }
		}

		public event SelectionChangedEventHandler SelectionChanged;
		public event Action FormatChanged;

		private void DriveCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectionChanged.Cast<SelectionChangedEventArgs>().SafeInvoke(this, e);
		}

		private void FormatCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			FormatChanged.SafeInvoke();
		}
	}
}