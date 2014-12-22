namespace StockSharp.Studio.Ribbon
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Common;

	using StockSharp.Licensing;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	class LicenseInfo
	{
		public string Name { get; set; }

		public License License { get; set; }

		public Tuple<string, string>[] Infos { get; set; }

		public string[] Features { get; set; }
	}

	public partial class LicenseTab
	{
		public readonly static RoutedCommand RenewLicenseCommand = new RoutedCommand();
		public readonly static RoutedCommand OpenLicenseCommand = new RoutedCommand();
		public readonly static RoutedCommand RemoveLicenseCommand = new RoutedCommand();
		public readonly static RoutedCommand RequestLicenseCommand = new RoutedCommand();

		private LicenseInfo SelectedLicenseInfo
		{
			get { return (LicenseInfo)LicensesCtrl.SelectedItem; }
		}

		private IEnumerable<License> _licenses;

		/// <summary>
		/// Лицензии.
		/// </summary>
		public IEnumerable<License> Licenses
		{
			get { return (IEnumerable<License>)LicensesCtrl.ItemsSource; }
			set
			{
				_licenses = value;

				if (_licenses == null)
					return;

				var licenses = _licenses.ToArray();

				var items = licenses
					.Select(l =>
					{
						var estimatedDays = (int)l.GetEstimatedTime().TotalDays;

						var item = new LicenseInfo
						{
							Name = "{0} ({1}{2})".Put(l.Id, l.HardwareId, l.Account),
							License = l,
							Infos = new[]
							{
								new Tuple<string, string>(LocalizedStrings.Str3585, l.IssuedTo.To<string>()),
								new Tuple<string, string>(LocalizedStrings.Str3586, estimatedDays == 0 ? LocalizedStrings.Str1536 : (estimatedDays + LocalizedStrings.Str1537)),
								new Tuple<string, string>(LocalizedStrings.Str3587, l.Id.To<string>()),
								new Tuple<string, string>(LocalizedStrings.Str3588, l.IssuedDate.ToString("dd.MM.yyyy")),
								new Tuple<string, string>(LocalizedStrings.Str3518 + ":", l.ExpirationDate.ToString("dd.MM.yyyy"))
							},
							Features = l.Features
						};
						return item;
					})
					.ToArray();

				LicensesCtrl.ItemsSource = items;
				LicensesCtrl.SelectedItem = items.FirstOrDefault();

				Foreground = licenses.IsExpired()
					? new SolidColorBrush(Colors.Red)
					: new SolidColorBrush(Colors.Black);
			}
		}

		private IEnumerable<Tuple<long, string>> _brokers;

		public IEnumerable<Tuple<long, string>> Brokers
		{
			get { return _brokers; }
			set
			{
				_brokers = value;

				BrokerId.ItemsSource = _brokers;
				BrokerId.SelectedIndex = -1;
			}
		}

		private IEnumerable<string> _features;

		public IEnumerable<string> Features
		{
			get { return _features; }
			set
			{
				_features = value;

				FeaturesCtrl.ItemsSource = _features;
			}
		}

		public LicenseTab()
		{
			InitializeComponent();

			BrokerId.DisplayMemberPath = "Item2";
			BrokerId.SelectedValuePath = "Item1";
		}

		private void ExecutedRenewLicenseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new RenewLicenseCommand(SelectedLicenseInfo.License).Process(this);
		}

		private void CanExecuteRenewLicenseCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedLicenseInfo != null;
		}

		private void ExecutedOpenLicenseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Process.Start("explorer.exe", "/select, \"{0}\"".Put(SelectedLicenseInfo.License.FileName));
		}

		private void CanExecuteOpenLicenseCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedLicenseInfo != null && !SelectedLicenseInfo.License.FileName.IsEmpty();
		}

		private void ExecutedRemoveLicenseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new RemoveLicenseCommand(SelectedLicenseInfo.License).Process(this);
		}

		private void CanExecuteRemoveLicenseCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedLicenseInfo != null && !SelectedLicenseInfo.License.FileName.IsEmpty();
		}

		private void ExecutedRequestLicenseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new RequestLicenseCommand((long)BrokerId.SelectedValue, AccountName.Text).Process(this);
		}

		private void CanExecuteRequestLicenseCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = BrokerId != null && BrokerId.SelectedValue != null && AccountName != null && !AccountName.Text.IsEmpty();
		}
	}

	class LicenseToVisibilityConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var license = value as License;
			return license == null || license.GetEstimatedTime() < LicenseHelper.RenewOffset ? Visibility.Visible : Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class FeatureIsCheckedConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] == null || values[1] == null || values[1] == DependencyProperty.UnsetValue)
				return Visibility.Hidden;

			var feature = (string)values[0];
			var license = (LicenseInfo)values[1];

			return license.Features.Contains(feature) ? Visibility.Visible : Visibility.Hidden;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
