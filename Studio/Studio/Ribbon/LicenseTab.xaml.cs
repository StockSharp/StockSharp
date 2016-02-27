#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: LicenseTab.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	}

	public partial class LicenseTab
	{
		public static readonly RoutedCommand RenewLicenseCommand = new RoutedCommand();
		public static readonly RoutedCommand OpenLicenseCommand = new RoutedCommand();
		public static readonly RoutedCommand RemoveLicenseCommand = new RoutedCommand();
		public static readonly RoutedCommand RequestLicenseCommand = new RoutedCommand();

		private LicenseInfo SelectedLicenseInfo => (LicenseInfo)LicensesCtrl.SelectedItem;

		private IEnumerable<License> _licenses = Enumerable.Empty<License>();

		/// <summary>
		/// Лицензии.
		/// </summary>
		public IEnumerable<License> Licenses
		{
			get { return ((IEnumerable<LicenseInfo>)LicensesCtrl.ItemsSource).Select(i => i.License); }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_licenses = value.ToArray();

				var items = _licenses
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
								new Tuple<string, string>(LocalizedStrings.Str3588, l.IssuedDate.ToString("d")),
								new Tuple<string, string>(LocalizedStrings.Str3518 + ":", l.ExpirationDate.ToString("d"))
							}
						};
						return item;
					})
					.ToArray();

				LicensesCtrl.ItemsSource = items;
				LicensesCtrl.SelectedItem = items.FirstOrDefault();

				Foreground = _licenses.IsExpired()
					? new SolidColorBrush(Colors.Red)
					: new SolidColorBrush(Colors.Black);
			}
		}

		private IEnumerable<Broker> _brokers = Enumerable.Empty<Broker>();

		public IEnumerable<Broker> Brokers
		{
			get { return _brokers; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_brokers = value.ToArray();

				BrokersComboBox.ItemsSource = _brokers;
				BrokersComboBox.SelectedItem = _brokers.FirstOrDefault();
			}
		}

		private Broker SelectedBroker => (Broker)BrokersComboBox.SelectedItem;

		private IEnumerable<string> _features = Enumerable.Empty<string>();

		public IEnumerable<string> Features
		{
			get { return _features; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_features = value;

				FeaturesCtrl.ItemsSource = _features;
			}
		}

		public LicenseTab()
		{
			InitializeComponent();
		}

		private void ExecutedRenewLicenseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new RenewLicenseCommand(SelectedLicenseInfo.License).Process(this);
		}

		private void CanExecuteRenewLicenseCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedLicenseInfo != null && !SelectedLicenseInfo.License.IsTrial();
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
			new RequestLicenseCommand(SelectedBroker.Id, AccountName.Text).Process(this);
		}

		private void CanExecuteRequestLicenseCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = BrokersComboBox != null && SelectedBroker != null && AccountName != null && !AccountName.Text.IsEmpty();
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

			return license.License.Features.Contains(feature) ? Visibility.Visible : Visibility.Hidden;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
