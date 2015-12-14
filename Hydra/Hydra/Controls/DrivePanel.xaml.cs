#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Controls.HydraPublic
File: DrivePanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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