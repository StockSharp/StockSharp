#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Windows.HydraPublic
File: SynchronizeWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Interop;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	public partial class SynchronizeWindow
	{
		private CancellationTokenSource _token;

		public SynchronizeWindow()
		{
			InitializeComponent();
		}

		public IStorageRegistry StorageRegistry { get; set; }
		public IEntityRegistry EntityRegistry { get; set; }

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_token != null)
			{
				if (Sync.IsEnabled)
				{
					var result =
						new MessageBoxBuilder()
							.Text(LocalizedStrings.Str2928)
							.Error()
							.YesNo()
							.Owner(this)
							.Show();

					if (result == MessageBoxResult.Yes)
					{
						StopSync();
					}
				}

				e.Cancel = true;
			}

			base.OnClosing(e);
		}

		private void StopSync()
		{
			Sync.IsEnabled = false;
			_token.Cancel();
		}

		private void Sync_Click(object sender, RoutedEventArgs e)
		{
			if (_token != null)
			{
				StopSync();
				return;
			}

			Sync.Content = LocalizedStrings.Str2890;

			_token = new CancellationTokenSource();

			Task.Factory.StartNew(() =>
			{
				var securityPaths = new List<string>();

				foreach (var dir in DriveCache.Instance.Drives
					.OfType<LocalMarketDataDrive>()
					.Select(drive => drive.Path)
					.Distinct())
				{
					foreach (var letterDir in InteropHelper.GetDirectories(dir))
					{
						if (_token.IsCancellationRequested)
							break;

						var name = Path.GetFileName(letterDir);

						if (name == null || name.Length != 1)
							continue;

						securityPaths.AddRange(InteropHelper.GetDirectories(letterDir));
					}

					if (_token.IsCancellationRequested)
						break;
				}

				if (_token.IsCancellationRequested)
					return;

				var iterCount = 
					securityPaths.Count + // кол-во проходов по директории для создания инструмента
					DriveCache.Instance.Drives.Count() * (((IList<Security>)EntityRegistry.Securities).Count + securityPaths.Count); // кол-во сбросов кэша дат

				this.GuiSync(() => Progress.Maximum = iterCount);

				var logSource = ConfigManager.GetService<LogManager>().Application;

				var securityIdGenerator = new SecurityIdGenerator();

				var securities = EntityRegistry.Securities.ToDictionary(s => s.Id, s => new KeyValuePair<Security, bool>(s, true), StringComparer.InvariantCultureIgnoreCase);

				foreach (var securityPath in securityPaths)
				{
					if (_token.IsCancellationRequested)
						break;

					var securityId = Path.GetFileName(securityPath).FolderNameToSecurityId();

					var isNew = false;

					var security = securities.TryGetValue(securityId).Key;
					if (security == null)
					{
						var firstDataFile =
							Directory.EnumerateDirectories(securityPath)
								.SelectMany(d => Directory.EnumerateFiles(d, "*.bin")
									.Concat(Directory.EnumerateFiles(d, "*.csv"))
									.OrderBy(f => Path.GetExtension(f).CompareIgnoreCase(".bin") ? 0 : 1))
								.FirstOrDefault();

						if (firstDataFile != null)
						{
							var id = securityIdGenerator.Split(securityId);

							decimal priceStep;

							if (Path.GetExtension(firstDataFile).CompareIgnoreCase(".bin"))
							{
								try
								{
									priceStep = File.ReadAllBytes(firstDataFile).Range(6, 16).To<decimal>();
								}
								catch (Exception ex)
								{
									throw new InvalidOperationException(LocalizedStrings.Str2929Params.Put(firstDataFile), ex);
								}
							}
							else
								priceStep = 0.01m;

							security = new Security
							{
								Id = securityId,
								PriceStep = priceStep,
								Name = id.SecurityCode,
								Code = id.SecurityCode,
								Board = ExchangeBoard.GetOrCreateBoard(id.BoardCode),
								ExtensionInfo = new Dictionary<object, object>()
							};

							securities.Add(securityId, new KeyValuePair<Security, bool>(security, false));

							isNew = true;
						}
					}

					this.GuiSync(() =>
					{
						Progress.Value++;

						if (isNew)
							Logs.Messages.Add(new LogMessage(logSource, TimeHelper.NowWithOffset, LogLevels.Info, LocalizedStrings.Str2930Params.Put(security)));
					});
				}

				EntityRegistry.Securities.AddRange(securities.Values.Where(p => !p.Value).Select(p => p.Key));

				if (_token.IsCancellationRequested)
					return;

				//var dataTypes = new[]
				//{
				//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Tick),
				//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.OrderLog),
				//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Order),
				//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Trade),
				//	Tuple.Create(typeof(QuoteChangeMessage), (object)null),
				//	Tuple.Create(typeof(Level1ChangeMessage), (object)null),
				//	Tuple.Create(typeof(NewsMessage), (object)null)
				//};

				var formats = Enumerator.GetValues<StorageFormats>().ToArray();

				foreach (var drive in DriveCache.Instance.Drives)
				{
					foreach (var secId in drive.AvailableSecurities)
					{
						foreach (var format in formats)
						{
							foreach (var dataType in drive.GetAvailableDataTypes(secId, format))
							{
								if (_token.IsCancellationRequested)
									break;

								drive
									.GetStorageDrive(secId, dataType.MessageType, dataType.Arg, format)
									.ClearDatesCache();
							}
						}

						if (_token.IsCancellationRequested)
							break;

						this.GuiSync(() =>
						{
							Progress.Value++;
							Logs.Messages.Add(new LogMessage(logSource, TimeHelper.NowWithOffset, LogLevels.Info,
								LocalizedStrings.Str2931Params.Put(secId, drive.Path)));
						});
					}

					if (_token.IsCancellationRequested)
						break;
				}
			}, _token.Token)
			.ContinueWithExceptionHandling(this, res =>
			{
				Sync.Content = LocalizedStrings.Str2932;
				Sync.IsEnabled = true;

				Progress.Value = 0;

				_token = null;
			});
		}
	}
}