namespace StockSharp.Studio.Controls
{
	using System;
	using System.ComponentModel;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;

	using ActiproSoftware.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Studio.Controls.Converters;
	using StockSharp.Studio.Core;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.SessionsKey)]
	[DescriptionLoc(LocalizedStrings.SessionsKey, true)]
	public partial class SessionsPanel : IStudioControl
	{
		private readonly ICollectionView _view;
		
		public SessionsPanel()
		{
			InitializeComponent();

			Sessions.GroupingColumnConverters.Add("Session.Type", (IValueConverter)Resources["enumConverter"]);

			var source = new ObservableCollectionEx<SessionStrategy>();
			var strategies = new ThreadSafeObservableCollection<SessionStrategy>(source);
			strategies.AddRange(Registry.Sessions.SelectMany(s =>
			{
				s.Strategies.Added += strategies.Add;
				return s.Strategies.ToArray();
			}));
			Registry.Sessions.Added += s =>
			{
				s.Strategies.Added += strategies.Add;
			};

			IValueConverter converter = new StatisticsStorageConverter();

			_view = CollectionViewSource.GetDefaultView(source);
			_view.Filter = obj =>
			{
				var sessionStrategy = obj as SessionStrategy;
				if (sessionStrategy != null)
				{
					var result = sessionStrategy.Session.StartTime >= FromDate && sessionStrategy.Session.EndTime <= ToDate;

					var profitValue = converter.Convert(sessionStrategy.Statistics, typeof(string), "Net Profit", CultureInfo.CurrentCulture);
					if (profitValue != null)
					{
						var profit = profitValue.To<decimal>();
						result &= profit >= ProfitFrom && profit <= ProfitTo;
					}
					else
						result &= ShowEmptyProfit.IsChecked == true;

					return result;
				}

				return true;
			};

			Sessions.ItemsSource = source;
		}

		private IStudioEntityRegistry _registry;

		public IStudioEntityRegistry Registry
		{
			get { return _registry ?? (_registry = ConfigManager.GetService<IStudioEntityRegistry>()); }
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Sessions.Load(storage.GetValue<SettingsStorage>("Sessions"));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("Sessions", Sessions.Save());
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title
		{
			get { return LocalizedStrings.Sessions; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}

		private DateTime FromDate { get { return From.Value ?? DateTime.MinValue; } }
		private DateTime ToDate { get { return To.Value ?? DateTime.MaxValue; } }
		private decimal ProfitFrom { get { return MinProfit.Value.HasValue ? MinProfit.Value.Value.To<decimal>() : decimal.MinValue; } }
		private decimal ProfitTo { get { return MaxProfit.Value.HasValue ? MaxProfit.Value.Value.To<decimal>() : decimal.MaxValue; } }

		private void OnValueChanged(object sender, PropertyChangedRoutedEventArgs<DateTime?> e)
		{
			if (_view != null)
				_view.Refresh();
		}

		private void ProfitOnValueChanged(object sender, PropertyChangedRoutedEventArgs<double?> e)
		{
			if (_view != null)
				_view.Refresh();
		}

		private void ShowEmptyProfit_OnClick(object sender, RoutedEventArgs e)
		{
			if (_view != null)
				_view.Refresh();
		}
	}
}