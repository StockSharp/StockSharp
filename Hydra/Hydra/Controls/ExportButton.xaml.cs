namespace StockSharp.Hydra.Controls
{
	using System;
	using System.Windows.Controls;
	using Ecng.Common;
	using Ecng.Xaml;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Hydra.Windows;
	using StockSharp.Localization;

	public partial class ExportButton
	{
		public ExportButton()
		{
			InitializeComponent();
		}

		public void EnableType(ExportTypes type, bool isEnabled)
		{
			((ListBoxItem)ExportTypeCtrl.Items[(int)type]).IsEnabled = isEnabled;
		}

		public ExportTypes ExportType { get; private set; }

		public event Action ExportStarted;

		private void ExportTypeCtrlSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ExportType = (ExportTypes)ExportTypeCtrl.SelectedIndex;

			ExportTypeCtrl.SelectionChanged -= ExportTypeCtrlSelectionChanged;

			try
			{
				ExportTypeCtrl.SelectedIndex = -1;
				ExportStarted.SafeInvoke();
			}
			finally
			{
				ExportTypeCtrl.SelectionChanged += ExportTypeCtrlSelectionChanged;
			}
		}

		public object GetPath(Security security, Type dataType, object arg, DateTime? from, DateTime? to, IMarketDataDrive drive)
		{
			var fileName = security.GetFileName(dataType, arg, from, to, ExportType);

			var dlg = new VistaSaveFileDialog
			{
				FileName = fileName,
				RestoreDirectory = true
			};

			switch (ExportType)
			{
				case ExportTypes.Excel:
					dlg.Filter = @"xlsx files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
					break;
				case ExportTypes.Xml:
					dlg.Filter = @"xml files (*.xml)|*.xml|All files (*.*)|*.*";
					break;
				case ExportTypes.Txt:
					dlg.Filter = @"text files (*.txt)|*.txt|All files (*.*)|*.*";
					break;
				case ExportTypes.Sql:
				{
					var wnd = new DatabaseConnectionWindow();

					if (wnd.ShowModal(this))
					{
						DatabaseConnectionCache.Instance.AddConnection(wnd.Connection);
						return wnd.Connection;
					}

					return null;
				}
				case ExportTypes.Bin:
				{
					var wndFolder = new VistaFolderBrowserDialog();

					if (drive is LocalMarketDataDrive)
						wndFolder.SelectedPath = drive.Path;

					return wndFolder.ShowDialog(this.GetWindow()) == true
						? DriveCache.Instance.GetDrive(wndFolder.SelectedPath)
						: null;
				}
				default:
				{
					new MessageBoxBuilder()
						.Error()
						.Owner(this)
						.Text(LocalizedStrings.Str2910Params.Put(ExportType))
							.Show();

					return null;
				}
			}

			return dlg.ShowDialog(this.GetWindow()) == true ? dlg.FileName : null;
		}
	}
}