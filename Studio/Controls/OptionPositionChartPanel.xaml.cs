namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using ActiproSoftware.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3239Key)]
	[DescriptionLoc(LocalizedStrings.Str3240Key)]
	[Icon("/StockSharp.Xaml;component/images/indicators_16x16.png", true)]
	public partial class OptionPositionChartPanel
	{
		private readonly IStudioConnector _studioConnector;
		private Security _currentSecurity;

		private decimal _minAssetPrice;
		private decimal _maxAssetPrice;
		private DateTime _minDate;
		private DateTime _maxDate;

		private decimal _lastTradePrice;
		private DateTime _currentDate;

		private bool _changeFromCode;

		private readonly SyncObject _needRefreshLock = new SyncObject();
		private bool _needRefresh;

		private readonly object _token;

		private decimal? LastTradePrice
		{
			get { return (decimal?)PosChart.MarketDataProvider.GetSecurityValue(_currentSecurity, Level1Fields.LastTradePrice); }
		}

		public OptionPositionChartPanel()
		{
			InitializeComponent();

			AssetPrice.ValueChanged += AssetPrice_OnValueChanged;
			CurrentDate.ValueChanged += CurrentDate_OnValueChanged;

			_studioConnector = ConfigManager.GetService<IStudioConnector>();
			_studioConnector.ValuesChanged += StudioConnectorValuesChanged;
			_studioConnector.NewPositions += StudioConnectorNewPositions;

			PosChart.MarketDataProvider = _studioConnector;
			PosChart.SecurityProvider = ConfigManager.GetService<IEntityRegistry>().Securities;

			_token = GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(() =>
			{
				lock (_needRefreshLock)
				{
					if (!_needRefresh)
						return;

					_needRefresh = false;
				}

				if (!AssetPriceReset.IsEnabled)
					Process(() => AssetPrice.Value = LastTradePrice);

				RefreshChart();
			});
		}

		public override void Dispose()
		{
			GuiDispatcher.GlobalDispatcher.RemovePeriodicalAction(_token);

			_studioConnector.ValuesChanged -= StudioConnectorValuesChanged;
			_studioConnector.NewPositions -= StudioConnectorNewPositions;
		}

		public override void Save(SettingsStorage storage)
		{
			if (UnderlyingAsset.SelectedSecurity != null)
				storage.SetValue("UnderlyingAsset", UnderlyingAsset.SelectedSecurity.Id);

			var currDate = CurrentDate.Value;
			if (currDate != null)
				storage.SetValue("CurrentDate", currDate.Value.To<long>());

			storage.SetValue("AssetPrice", AssetPrice.Value);
		}

		public override void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("UnderlyingAsset"))
				UnderlyingAsset.SelectedSecurity = ConfigManager.GetService<IEntityRegistry>().Securities.ReadById(storage.GetValue<string>("UnderlyingAsset"));

			if (storage.ContainsKey("CurrentDate"))
				CurrentDate.Value = storage.GetValue<DateTime>("CurrentDate");

			AssetPrice.Value = storage.GetValue<decimal?>("AssetPrice");
		}

		private void StudioConnectorNewPositions(IEnumerable<Position> positions)
		{
			if (_currentSecurity == null)
				return;

			var assetPos = _studioConnector.Positions.FirstOrDefault(p => p.Security == _currentSecurity);
			var newPos = _studioConnector.Positions.Where(p => p.Security.UnderlyingSecurityId == _currentSecurity.Id).ToArray();

			if (assetPos == null && newPos.Length == 0)
				return;

			if (assetPos != null && PosChart.AssetPosition == null)
				PosChart.AssetPosition = assetPos;

			if (newPos.Length > 0)
				PosChart.Positions.AddRange(newPos);

			lock (_needRefreshLock)
				_needRefresh = true;
		}

		private void StudioConnectorValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> values, DateTimeOffset serverTime, DateTime localTime)
		{
			if (_currentSecurity == null || security != _currentSecurity)
				return;

			if (_minAssetPrice == 0 && _maxAssetPrice == 0)
				SetPriceLimits();

			if (_minDate.IsDefault() && _maxDate.IsDefault())
				SetDateLimits();

			lock (_needRefreshLock)
				_needRefresh = true;
		}

		private void UnderlyingAsset_OnSecuritySelected()
		{
			var security = UnderlyingAsset.SelectedSecurity;

			if (_currentSecurity == security)
				return;

			if (_currentSecurity != null)
			{
				PosChart.AssetPosition = null;
				PosChart.Positions.Clear();

				_minAssetPrice = _maxAssetPrice = 0;
				_minDate = _maxDate = default(DateTime);

				Process(() =>
				{
					AssetPriceModified.Value = 0;
					AssetPriceReset.IsEnabled = false;

					CurrentDateModified.Value = 0;
					CurrentDateReset.IsEnabled = false;
				});
			}

			_currentSecurity = security;

			if (_currentSecurity != null)
			{
				var assetPos = _studioConnector.Positions.FirstOrDefault(p => p.Security == _currentSecurity);
				var newPos = _studioConnector.Positions.Where(p => p.Security.UnderlyingSecurityId == _currentSecurity.Id).ToArray();

				if (assetPos == null && newPos.Length == 0)
					return;

				if (assetPos != null)
					PosChart.AssetPosition = assetPos;

				if (newPos.Length > 0)
					PosChart.Positions.AddRange(newPos);

				Process(() =>
				{
					SetPriceLimits();
					SetDateLimits();
				});

				RefreshChart();
			}

			Title = LocalizedStrings.Str3239 + " " + _currentSecurity;
		}

		private void AssetPriceReset_OnClick(object sender, RoutedEventArgs e)
		{
			AssetPriceReset.IsEnabled = false;

			RefreshChart();
		}

		private void AssetPrice_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			var newValue = (decimal?)e.NewValue;

			if (newValue == null)
			{
				var price = _currentSecurity == null ? null : LastTradePrice;
				AssetPriceModified.Value = (double)(price ?? 0);
				AssetPriceReset_OnClick(sender, e);
				return;
			}

			_lastTradePrice = newValue.Value;
			AssetPriceModified.Value = (double)newValue.Value;

			if (_changeFromCode)
				return;

			AssetPriceReset.IsEnabled = true;

			RefreshChart();
		}

		private void CurrentDateModified_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var newValue = (double?)e.NewValue;

			if (_minDate.IsDefault())
				return;

			var date = _minDate.AddDays(newValue.Value);

			if (_currentDate == date)
				return;

			CurrentDate.Value = date;
		}

		private void CurrentDate_OnValueChanged(object sender, PropertyChangedRoutedEventArgs<DateTime?> e)
		{
			var newValue = e.NewValue;
			
			if (newValue == null)
			{
				CurrentDateModified.Value = _currentSecurity == null ? 0 : (TimeHelper.Now.Date - _minDate).TotalDays;
				CurrentDateReset_OnClick(sender, e);
				return;
			}

			if (_currentDate == newValue.Value)
				return;

			_currentDate = newValue.Value.Date;
			CurrentDateModified.Value = (_currentDate - _minDate).TotalDays;

			if (_changeFromCode)
				return;

			CurrentDateReset.IsEnabled = true;

			RefreshChart();
		}

		private void CurrentDateReset_OnClick(object sender, RoutedEventArgs e)
		{
			CurrentDateReset.IsEnabled = false;
			Process(() => CurrentDate.Value = _maxDate);
			RefreshChart();
		}

		private void RefreshChart()
		{
			var asset = _currentSecurity;

			if (_lastTradePrice != 0)
				PosChart.Refresh(_lastTradePrice, asset.PriceStep ?? 1m, _currentDate, (asset.ExpiryDate ?? DateTimeOffset.Now).LocalDateTime + TimeSpan.FromDays(1));
		}

		private void SetPriceLimits()
		{
			var minPrice = (decimal?)PosChart.MarketDataProvider.GetSecurityValue(_currentSecurity, Level1Fields.MinPrice);
			var maxPrice = (decimal?)PosChart.MarketDataProvider.GetSecurityValue(_currentSecurity, Level1Fields.MaxPrice);

			if (minPrice != null && maxPrice != null)
			{
				_minAssetPrice = minPrice.Value;
				_maxAssetPrice = maxPrice.Value;
			}
			else
			{
				var price = LastTradePrice;

				if (price == null)
					return;

				_minAssetPrice = _currentSecurity.ShrinkPrice(((decimal)(price - 20.Percents())));
				_maxAssetPrice = _currentSecurity.ShrinkPrice(((decimal)(price + 20.Percents())));
			}

			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				AssetPriceModified.Minimum = (double)_minAssetPrice;
				AssetPriceModified.Maximum = (double)_maxAssetPrice;
				AssetPriceModified.Value = (double)(LastTradePrice ?? (_minAssetPrice + _maxAssetPrice) / 2);
			});
		}

		private void SetDateLimits()
		{
			_maxDate = (_currentSecurity.ExpiryDate ?? default(DateTimeOffset)).LocalDateTime;
			_minDate = !_maxDate.IsDefault() ? _maxDate.Subtract(TimeSpan.FromDays(30)) : default(DateTime);

			var diff = (_maxDate - _minDate).TotalDays;

			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				CurrentDateModified.Minimum = 0;
				CurrentDateModified.Maximum = diff;
				CurrentDateModified.Value = Math.Abs(diff) < double.Epsilon ? 0 : (_maxDate - TimeHelper.Now.Date).TotalDays;
			});
		}

		private void Process(Action action)
		{
			_changeFromCode = true;

			try
			{
				action();
			}
			finally
			{
				_changeFromCode = false;
			}
		}
	}
}
