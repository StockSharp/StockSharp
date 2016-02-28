#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Controls.HydraPublic
File: ExportProgress.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Controls
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;
	using Ecng.Xaml.Database;

	using StockSharp.Algo;
	using StockSharp.Algo.Export;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Hydra.Windows;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class ExportProgress
	{
		private BackgroundWorker _worker;
		private ExportButton _button;
		private Grid _mainGrid;
		private string _fileName;

		public ExportProgress()
		{
			InitializeComponent();
		}

		public bool IsStarted => _worker != null;

		public void Init(ExportButton button, Grid mainGrid)
		{
			if (button == null)
				throw new ArgumentNullException(nameof(button));

			if (mainGrid == null)
				throw new ArgumentNullException(nameof(mainGrid));

			_button = button;
			_mainGrid = mainGrid;
		}

		public void Start(Security security, Type dataType, object arg, IEnumerable values, int valuesCount, object path, StorageFormats storageFormat)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (security == null && dataType != typeof(NewsMessage) && dataType != typeof(SecurityMessage))
				throw new ArgumentNullException(nameof(security));

			if (values == null)
				throw new ArgumentNullException(nameof(values));

			var currProgress = 5;

			var valuesPerPercent = (valuesCount / (100 - currProgress)).Max(1);
			var valuesPerCount = (valuesPerPercent / 10).Max(1);
			var currCount = 0;
			var valuesProcessed = 0;

			Func<int, bool> isCancelled = count =>
			{
				var isCancelling = _worker.CancellationPending;

				if (!isCancelling)
				{
					valuesProcessed += count;

					if (valuesProcessed / valuesPerPercent > currProgress)
					{
						currProgress = valuesProcessed / valuesPerPercent;
						_worker.ReportProgress(currProgress);
					}

					if (valuesProcessed > currCount)
					{
						currCount = valuesProcessed + valuesPerCount;
						this.GuiAsync(() => UpdateCount(valuesProcessed));
					}
				}

				return isCancelling;
			};

			string fileName;
			BaseExporter exporter;

			switch (_button.ExportType)
			{
				case ExportTypes.Excel:
					fileName = (string)path;
					exporter = new ExcelExporter(security, arg, isCancelled, fileName, () => GuiDispatcher.GlobalDispatcher.AddSyncAction(TooManyValues));
					break;
				case ExportTypes.Xml:
					fileName = (string)path;
					exporter = new XmlExporter(security, arg, isCancelled, fileName);
					break;
				case ExportTypes.Txt:
					var wnd = new ExportTxtPreviewWindow
					{
						DataType = dataType,
						Arg = arg
					};

					var registry = ((HydraEntityRegistry)ConfigManager.GetService<IEntityRegistry>()).Settings.TemplateTxtRegistry;

					if (dataType == typeof(SecurityMessage))
						wnd.TxtTemplate = registry.TemplateTxtSecurity;
					else if (dataType == typeof(NewsMessage))
						wnd.TxtTemplate = registry.TemplateTxtNews;
					else if (dataType.IsCandleMessage())
						wnd.TxtTemplate = registry.TemplateTxtCandle;
					else if (dataType == typeof(Level1ChangeMessage))
						wnd.TxtTemplate = registry.TemplateTxtLevel1;
					else if (dataType == typeof(QuoteChangeMessage))
						wnd.TxtTemplate = registry.TemplateTxtDepth;
					else if (dataType == typeof(ExecutionMessage))
					{
						if (arg == null)
							throw new ArgumentNullException(nameof(arg));

						switch ((ExecutionTypes)arg)
						{
							case ExecutionTypes.Tick:
								wnd.TxtTemplate = registry.TemplateTxtTick;
								break;
							case ExecutionTypes.Transaction:
								wnd.TxtTemplate = registry.TemplateTxtTransaction;
								break;
							case ExecutionTypes.OrderLog:
								wnd.TxtTemplate = registry.TemplateTxtOrderLog;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
						throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str721);


					if (!wnd.ShowModal(this))
						return;

					if (dataType == typeof(SecurityMessage))
						registry.TemplateTxtSecurity = wnd.TxtTemplate;
					else if (dataType == typeof(NewsMessage))
						registry.TemplateTxtNews = wnd.TxtTemplate;
					else if (dataType.IsCandleMessage())
						registry.TemplateTxtCandle = wnd.TxtTemplate;
					else if (dataType == typeof(Level1ChangeMessage))
						registry.TemplateTxtLevel1 = wnd.TxtTemplate;
					else if (dataType == typeof(QuoteChangeMessage))
						registry.TemplateTxtDepth = wnd.TxtTemplate;
					else if (dataType == typeof(ExecutionMessage))
					{
						if (arg == null)
							throw new ArgumentNullException(nameof(arg));

						switch ((ExecutionTypes)arg)
						{
							case ExecutionTypes.Tick:
								registry.TemplateTxtTick = wnd.TxtTemplate;
								break;
							case ExecutionTypes.Transaction:
								registry.TemplateTxtTransaction = wnd.TxtTemplate;
								break;
							case ExecutionTypes.OrderLog:
								registry.TemplateTxtOrderLog = wnd.TxtTemplate;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
						throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str721);

					fileName = (string)path;
					exporter = new TextExporter(security, arg, isCancelled, fileName, wnd.TxtTemplate, wnd.TxtHeader);
					break;
				case ExportTypes.Sql:
					fileName = null;
					exporter = new DatabaseExporter(security, arg, isCancelled, (DatabaseConnectionPair)path) { CheckUnique = false };
					break;
				case ExportTypes.StockSharp:
					var drive = (IMarketDataDrive)path;
					fileName = drive is LocalMarketDataDrive ? drive.Path : null;
					exporter = new StockSharpExporter(security, arg, isCancelled, drive, storageFormat);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			CreateWorker(valuesCount, fileName);

			_worker.DoWork += (s, e) =>
			{
				_worker.ReportProgress(currProgress);

				exporter.Export(dataType, values);

				_worker.ReportProgress(100);
				Thread.Sleep(500);
				_worker.ReportProgress(0);
			};

			_worker.RunWorkerAsync();
		}

		public void Start(IMarketDataDrive destDrive, DateTime? startDate, DateTime? endDate, Security security, IMarketDataDrive sourceDrive, StorageFormats format, Type dataType, object arg)
		{
			var storageRegistry = ConfigManager.GetService<IStorageRegistry>();
			var storage = storageRegistry.GetStorage(security, dataType, arg, sourceDrive, format);

			var dates = storage.Dates.ToArray();

			if (dates.IsEmpty())
				return;

			var allDates = (startDate ?? dates.First()).Range(endDate ?? dates.Last(), TimeSpan.FromDays(1));

			var datesToExport = storage.Dates
						.Intersect(allDates)
						.ToArray();

			CreateWorker(datesToExport.Length, null);

			_worker.DoWork += (s, e) =>
			{
				try
				{
					_worker.ReportProgress(0);

					var currentValuesCount = 0;
					//var totalValuesCount = datesToExport.Select(d => d.Item2).Sum();

					if (!Directory.Exists(destDrive.Path))
						Directory.CreateDirectory(destDrive.Path);

					var dataPath = ((LocalMarketDataDrive)sourceDrive).GetSecurityPath(security.ToSecurityId());
					var fileName = LocalMarketDataDrive.GetFileName(dataType, arg) + LocalMarketDataDrive.GetExtension(StorageFormats.Binary);

					foreach (var date in datesToExport)
					{
						var dateStr = date.ToString("yyyy_MM_dd");
						var file = Path.Combine(dataPath, dateStr, fileName);

						if (File.Exists(file))
						{
							if (!Directory.Exists(Path.Combine(destDrive.Path, dateStr)))
								Directory.CreateDirectory(Path.Combine(destDrive.Path, dateStr));

							File.Copy(file, Path.Combine(destDrive.Path, dateStr, Path.GetFileName(file)), true);
						}

						//if (date.Item2 == 0)
						//	continue;

						currentValuesCount++;
						_worker.ReportProgress((int)Math.Round(currentValuesCount * 100m / datesToExport.Length));
						this.GuiAsync(() => UpdateCount(currentValuesCount));
					}
				}
				finally
				{
					_worker.ReportProgress(100);
					Thread.Sleep(500);
					_worker.ReportProgress(0);	
				}
			};

			_worker.RunWorkerAsync();
		}

		public void Stop()
		{
			_worker?.CancelAsync();
		}

		private int _totalCount;

		private void CreateWorker(int totalCount, string fileName)
		{
			ClearStatus();

			_totalCount = totalCount;

			StopBtn.Visibility = Visibility.Visible;
			OpenFilePanel.Visibility = Visibility.Collapsed;

			_fileName = fileName;

			_mainGrid.IsEnabled = false;

			_worker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };

			_worker.RunWorkerCompleted += (s, e) =>
			{
				if (e.Error != null)
				{
					e.Error.LogError();
					StopBtn.Visibility = Visibility.Collapsed;
					ProgressBar.Value = 0;
				}

				_mainGrid.IsEnabled = true;

				ProgressBarPanel.Visibility = Visibility.Collapsed;

				if (_fileName != null)
					OpenFilePanel.Visibility = Visibility.Visible;

				_worker = null;
			};

			_worker.ProgressChanged += (s, e) =>
			{
				ProgressBarPanel.Visibility = Visibility.Visible;
				ProgressBar.Value = e.ProgressPercentage;
			};
		}

		public void ClearStatus()
		{
			StatusText.Text = string.Empty;
			ProgressText.Text = string.Empty;

			StatusTextPanel.Visibility = Visibility.Collapsed;
			ProgressBarPanel.Visibility = Visibility.Collapsed;
		}

		private void TooManyValues()
		{
			UpdateStatus(LocalizedStrings.Str2911);
		}

		private void UpdateCount(int count)
		{
			ProgressText.Text = LocalizedStrings.Str2912Params.Put(count, _totalCount);

			StatusTextPanel.Visibility = Visibility.Collapsed;
			ProgressBarPanel.Visibility = Visibility.Visible;
		}

		public void DoesntExist()
		{
			UpdateStatus(LocalizedStrings.Str2913);
		}

		private void UpdateStatus(string status)
		{
			StatusText.Text = status;

			StatusTextPanel.Visibility = Visibility.Visible;
			ProgressBarPanel.Visibility = Visibility.Collapsed;
		}

		public void Load<T>(IEnumerable<T> source, Action<IEnumerable<T>> addValues, int maxValueCount, Action<T> itemLoaded = null)
		{
			CreateWorker(maxValueCount, null);

			_worker.DoWork += (sender, args) =>
			{
				var count = 0;

				foreach (var v in source)
				{
					if (_worker.CancellationPending)
						break;

					var value = v;

					var canContinue = this.GuiSync(() =>
					{
						addValues(new[] { value });
						count++;

						UpdateCount(count);

						itemLoaded?.Invoke(value);

						if (count > maxValueCount)
						{
							TooManyValues();
							return false;
						}

						return true;
					});

					if (!canContinue)
						break;
				}

				if (count == 0)
					this.GuiSync(DoesntExist);
			};

			_worker.RunWorkerAsync();
		}

		private void StopBtn_Click(object sender, RoutedEventArgs e)
		{
			Stop();
		}

		private void OpenFileBtn_OnClick(object sender, RoutedEventArgs e)
		{
			Process.Start(_fileName);
		}
	}
}