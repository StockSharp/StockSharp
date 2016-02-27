#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: TargetPlatformWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.IO;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.Localization;

	class AppStartSettings
	{
		public bool AutoStart { get; set; }
		public Platforms Platform { get; set; }
		public Languages Language { get; set; }
	}

	/// <summary>
	/// The component to select a platform for the application.
	/// </summary>
	public partial class TargetPlatformWindow
	{
		sealed class LanguageSorter : IComparer
		{
			private readonly Languages _language;

			public LanguageSorter(Languages language)
			{
				_language = language;
			}

			public int Compare(object x, object y)
			{
				var xFeature = (TargetPlatformFeature)x;
				var yFeature = (TargetPlatformFeature)y;

				var xKey = xFeature.PreferLanguage == _language ? -1 : (int)xFeature.PreferLanguage;
				var yKey = yFeature.PreferLanguage == _language ? -1 : (int)yFeature.PreferLanguage;

				if (xKey == yKey)
					return string.Compare(xFeature.ToString(), yFeature.ToString(), StringComparison.Ordinal);

				return xKey.CompareTo(yKey);
			}
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="TargetPlatformWindow.AppName"/>.
		/// </summary>
		public static readonly DependencyProperty AppNameProperty = DependencyProperty.Register(nameof(AppName), typeof(string), typeof(TargetPlatformWindow), new PropertyMetadata(TypeHelper.ApplicationName));

		/// <summary>
		/// The application name.
		/// </summary>
		public string AppName
		{
			get { return (string)GetValue(AppNameProperty); }
			set { SetValue(AppNameProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="TargetPlatformWindow.AppIcon"/>.
		/// </summary>
		public static readonly DependencyProperty AppIconProperty = DependencyProperty.Register(nameof(AppIcon), typeof(string), typeof(TargetPlatformWindow));

		/// <summary>
		/// The application icon.
		/// </summary>
		public string AppIcon
		{
			get { return (string)GetValue(AppIconProperty); }
			private set { SetValue(AppIconProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="TargetPlatformWindow.AutoStart"/>.
		/// </summary>
		public static readonly DependencyProperty AutoStartProperty = DependencyProperty.Register(nameof(AutoStart), typeof(bool), typeof(TargetPlatformWindow));

		/// <summary>
		/// Autostart of the selected configuration.
		/// </summary>
		public bool AutoStart
		{
			get { return (bool)GetValue(AutoStartProperty); }
			private set { SetValue(AutoStartProperty, value); }
		}

		private readonly ObservableCollection<TargetPlatformFeature> _features = new ObservableCollection<TargetPlatformFeature>();
		private readonly ListCollectionView _featuresView;

		/// <summary>
		/// Available functionality for all platforms.
		/// </summary>
		public ObservableCollection<TargetPlatformFeature> Features => _features;

		/// <summary>
		/// The selected platform.
		/// </summary>
		public Platforms SelectedPlatform { get; private set; }

		private Languages _selectedLanguage;

		/// <summary>
		/// The selected culture.
		/// </summary>
		public Languages SelectedLanguage
		{
			get { return _selectedLanguage; }
			private set
			{
				_selectedLanguage = value;

				HintLabel.Text = LocalizedStrings.GetString(LocalizedStrings.XamlStr178Key, value);
				AutoCheckBox.Content = LocalizedStrings.GetString(LocalizedStrings.XamlStr176Key, value);
				SelectPlatformLabel.Text = LocalizedStrings.GetString(LocalizedStrings.SelectAppModeKey, value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TargetPlatformWindow"/>.
		/// </summary>
		public TargetPlatformWindow()
		{
			InitializeComponent();

			Title = TypeHelper.ApplicationNameWithVersion;

			var app = (BaseApplication)Application.Current;

			var features = new[]
			{
				new TargetPlatformFeature("Quik", Languages.Russian),
				new TargetPlatformFeature("SmartCOM", Languages.Russian),
				new TargetPlatformFeature("Plaza2", Languages.Russian),
				new TargetPlatformFeature("Transaq", Languages.Russian),
				new TargetPlatformFeature("Micex", Languages.Russian),
				new TargetPlatformFeature("Alfa Direct", Languages.Russian, Platforms.x86),
				new TargetPlatformFeature("OpenECry"),
				new TargetPlatformFeature("Interactive Brokers"),
				new TargetPlatformFeature("E*Trade"),
				new TargetPlatformFeature("Blackwood/Fusion"),
				new TargetPlatformFeature("LMAX"),
				new TargetPlatformFeature("IQFeed"),
				new TargetPlatformFeature("BarChart"),
				new TargetPlatformFeature("OANDA"),
				new TargetPlatformFeature("Rithmic"),
				new TargetPlatformFeature("FIX/FAST"),
				new TargetPlatformFeature("ITCH"),
				new TargetPlatformFeature("BTCE"),
				new TargetPlatformFeature("BitStamp"),
				new TargetPlatformFeature("RSS")
			};

			_features.AddRange(features);
			_features.AddRange(app.ExtendedFeatures);

			_featuresView = (ListCollectionView)CollectionViewSource.GetDefaultView(_features);
			_featuresView.CustomSort = new LanguageSorter(SelectedLanguage);

			AppIcon = app.AppIcon;

			if (!Environment.Is64BitOperatingSystem)
			{
				PlatformCheckBox.IsEnabled = false;
				PlatformCheckBox.IsChecked = false;
			}
			else
			{
				SelectedPlatform = Platforms.x64;
				UpdatePlatformCheckBox();
			}

			SelectedLanguage = LocalizedStrings.ActiveLanguage;
			UpdateLangButtons();

			var configFile = BaseApplication.PlatformConfigurationFile;

			if (configFile.IsEmptyOrWhiteSpace() || !File.Exists(configFile))
				return;

			var settings = new XmlSerializer<AppStartSettings>().Deserialize(configFile);

			SelectedPlatform = PlatformCheckBox.IsEnabled ? settings.Platform : Platforms.x86;
			AutoStart = settings.AutoStart;
			LocalizedStrings.ActiveLanguage = SelectedLanguage = settings.Language;

			UpdateLangButtons();
			UpdatePlatformCheckBox();
		}

		private void Save()
		{
			if (BaseApplication.PlatformConfigurationFile.IsEmptyOrWhiteSpace())
				return;

			new XmlSerializer<AppStartSettings>().Serialize(new AppStartSettings
			{
				Platform = SelectedPlatform,
				AutoStart = AutoStart,
				Language = SelectedLanguage
			}, BaseApplication.PlatformConfigurationFile);
		}

		private void UpdateLangButtons()
		{
			if (SelectedLanguage == Languages.English)
			{
				EnglishLang.IsChecked = true;
				RussianLang.IsChecked = false;
			}
			else
			{
				EnglishLang.IsChecked = false;
				RussianLang.IsChecked = true;
			}
		}

		private void UpdatePlatformCheckBox()
		{
			PlatformCheckBox.IsChecked = SelectedPlatform == Platforms.x64;
		}

		private void OnLangClick(object sender, RoutedEventArgs e)
		{
			if (ReferenceEquals(sender, RussianLang))
				EnglishLang.IsChecked = !RussianLang.IsChecked;
			else
				RussianLang.IsChecked = !EnglishLang.IsChecked;

			SelectedLanguage = EnglishLang.IsChecked == true ? Languages.English : Languages.Russian;

			_featuresView.CustomSort = new LanguageSorter(SelectedLanguage);
			_featuresView.Refresh();
		}

		private void PlatformCheckBox_OnChecked(object sender, RoutedEventArgs e)
		{
			SelectedPlatform = PlatformCheckBox.IsChecked == true ? Platforms.x64 : Platforms.x86;
			_featuresView.Refresh();
		}

		private void OkButton_OnClick(object sender, RoutedEventArgs e)
		{
			//if (!AutoStart)
			//	SelectedPlatform = Platforms.x64;

			LocalizedStrings.ActiveLanguage = SelectedLanguage;

			Save();

			DialogResult = true;
			//Close();
		}
	}

	sealed class PlatformToColorConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var feature = values[0].WpfCast<TargetPlatformFeature>();
			var platform = values[1].WpfCast<bool?>();

			if (feature == null || platform == null)
				return Binding.DoNothing;

			return feature.Platform == Platforms.AnyCPU || (!platform.Value && feature.Platform == Platforms.x86) || (platform.Value && feature.Platform == Platforms.x64)
				? Brushes.Black
				: Brushes.Transparent;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}