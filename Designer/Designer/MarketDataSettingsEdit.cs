namespace StockSharp.Designer
{
	using System.Windows;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Helpers;
	using DevExpress.Xpf.Editors.Settings;

	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Studio.Core;

	public class MarketDataSettingsEdit : ComboBoxEdit
	{
		public static readonly DependencyProperty IsDefaultEditorProperty = DependencyProperty.Register("IsDefaultEditor", typeof(bool), typeof(MarketDataSettingsEdit));

		public bool IsDefaultEditor
		{
			get { return (bool)GetValue(IsDefaultEditorProperty); }
			set { SetValue(IsDefaultEditorProperty, value); }
		}

		static MarketDataSettingsEdit()
		{
			MarketDataSettingsEditSettings.RegisterCustomEdit();
		}
	}

	public class MarketDataSettingsEditSettings : ComboBoxEditSettings
	{
		public static readonly DependencyProperty IsDefaultEditorProperty = DependencyProperty.Register("IsDefaultEditor", typeof(bool), typeof(MarketDataSettingsEditSettings));

		public bool IsDefaultEditor
		{
			get { return (bool)GetValue(IsDefaultEditorProperty); }
			set { SetValue(IsDefaultEditorProperty, value); }
		}

		static MarketDataSettingsEditSettings()
		{
			RegisterCustomEdit();
		}

		public static void RegisterCustomEdit()
		{
			EditorSettingsProvider.Default.RegisterUserEditor(
				typeof(MarketDataSettingsEdit), typeof(MarketDataSettingsEditSettings),
				() => new MarketDataSettingsEdit(), () => new MarketDataSettingsEditSettings());
		}

		private MarketDataSettingsCache _cache;

		public MarketDataSettingsEditSettings()
		{
			_cache = ConfigManager.TryGetService<MarketDataSettingsCache>();

			if (_cache == null)
			{
				ConfigManager.ServiceRegistered += (t, s) =>
				{
					if (typeof(MarketDataSettingsCache) != t)
						return;

					_cache = (MarketDataSettingsCache)s;
					GuiDispatcher.GlobalDispatcher.AddAction(() => ItemsSource = _cache.Settings);
				};
			}
			else
				ItemsSource = _cache.Settings;

			DisplayMember = "Path";
		}
	}
}