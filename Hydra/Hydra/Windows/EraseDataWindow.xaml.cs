namespace StockSharp.Hydra.Windows
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	public partial class EraseDataWindow
	{
		private CancellationTokenSource _token;
		private readonly string _templateTitle;

		public Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		public IStorageRegistry StorageRegistry { get; set; }
		public HydraEntityRegistry EntityRegistry { get; set; }

		public EraseDataWindow()
		{
			InitializeComponent();

			_templateTitle = Title;

			From.Value = DateTime.Today - TimeSpan.FromDays(7);
			To.Value = DateTime.Today + TimeSpan.FromDays(1);

			Drive.SelectedDrive = null;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_token != null)
			{
				if (Erase.IsEnabled)
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
			Erase.IsEnabled = false;
			_token.Cancel();
		}

		private void Erase_Click(object sender, RoutedEventArgs e)
		{
			if (_token != null)
			{
				StopSync();
				return;
			}

			var securities = AllSecurities.IsChecked == true
				? null
				: SelectSecurityBtn.SelectedSecurities.ToArray();

			var from = From.Value ?? DateTime.MinValue;
			var to = To.Value ?? DateTime.MaxValue;

			if (from > to)
			{
				new MessageBoxBuilder()
					.Caption(LocalizedStrings.Str2879)
					.Text(LocalizedStrings.Str1119Params.Put(from, to))
					.Warning()
					.Owner(this)
					.Show();

				return;
			}

			if (securities != null && securities.IsEmpty())
			{
				new MessageBoxBuilder()
					.Caption(LocalizedStrings.Str2879)
					.Text(LocalizedStrings.Str2881)
					.Warning()
					.Owner(this)
					.Show();

				return;
			}

			var msg = string.Empty;

			if (from != DateTime.MinValue || to != DateTime.MaxValue)
			{
				msg += LocalizedStrings.Str3846;

				if (from != DateTime.MinValue)
					msg += " " + LocalizedStrings.XamlStr624.ToLowerInvariant() + " " + from.ToString("d");

				if (to != DateTime.MaxValue)
					msg += " " + LocalizedStrings.XamlStr132 + " " + to.ToString("d");
			}

			//msg += AllSecurities.IsChecked == true
			//			   ? LocalizedStrings.Str2882
			//			   : LocalizedStrings.Str2883Params.Put(securities.Take(100).Select(sec => sec.Id).Join(", "));

			if (new MessageBoxBuilder()
					.Caption(LocalizedStrings.Str2879)
					.Text(LocalizedStrings.Str2884Params.Put(msg))
					.Warning()
					.YesNo()
					.Owner(this)
					.Show() != MessageBoxResult.Yes)
			{
				return;
			}

			Erase.Content = LocalizedStrings.Str2890;
			_token = new CancellationTokenSource();

			var drives = Drive.SelectedDrive == null
					? DriveCache.Instance.AllDrives.ToArray()
					: new[] { Drive.SelectedDrive };

			if (to != DateTime.MaxValue && to.TimeOfDay == TimeSpan.Zero)
				to = to.EndOfDay();

			Task.Factory.StartNew(() =>
			{
				var formats = Enumerator.GetValues<StorageFormats>().ToArray();

				var iterCount = drives.Length * (securities == null ? ((IStorageEntityList<Security>)EntityRegistry.Securities).Count : securities.Length) * 5 /* message types count */ * formats.Length;

				this.GuiSync(() => Progress.Maximum = iterCount);

				foreach (var drive in drives)
				{
					if (_token.IsCancellationRequested)
						break;

					foreach (var securityId in drive.AvailableSecurities)
					{
						if (_token.IsCancellationRequested)
							break;

						var id = securityId.SecurityCode + "@" + securityId.BoardCode;

						var security = securities == null
							? EntityRegistry.Securities.ReadById(id)
							: securities.FirstOrDefault(s => s.Id.CompareIgnoreCase(id));

						if (security == null)
						{
							continue;
						}

						foreach (var format in formats)
						{
							if (_token.IsCancellationRequested)
								break;
							
							foreach (var dataType in drive.GetAvailableDataTypes(securityId, format))
							{
								if (_token.IsCancellationRequested)
									break;

								StorageRegistry
										.GetStorage(security, dataType.Item1, dataType.Item2, drive, format)
										.Delete(from, to);

								this.GuiSync(() =>
								{
									Progress.Value++;
								});
							}
						}
					}
				}
			}, _token.Token)
			.ContinueWithExceptionHandling(this, res =>
			{
				Erase.Content = LocalizedStrings.Str2060;
				Erase.IsEnabled = true;

				Progress.Value = 0;
				_token = null;
			});
		}

		private void SelectSecurityBtn_SecuritySelected()
		{
			if (SelectedSecurity == null)
				Title = _templateTitle;
			else
				Title = _templateTitle + SelectedSecurity.Id;
		}

		// TODO
		//private static void ClearTypeInfo(HydraTaskSecurity.TypeInfo info)
		//{
		//	info.LastTime = null;
		//	info.Count = 0;
		//}

		private void AllSecurities_Click(object sender, RoutedEventArgs e)
		{
			SelectSecurityBtn.IsEnabled = AllSecurities.IsChecked != true;
		}
	}
}