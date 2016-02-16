#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: OptionDeskPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;

	using ActiproSoftware.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Derivatives;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3265Key)]
	[DescriptionLoc(LocalizedStrings.Str3266Key)]
	[Icon("/StockSharp.Xaml;component/images/indicators_16x16.png", true)]
	public partial class OptionDeskPanel
	{
		private readonly CachedSynchronizedSet<Security> _options = new CachedSynchronizedSet<Security>();

		private readonly SyncObject _needRefreshLock = new SyncObject();
		private bool _needRefresh;

		private decimal _minAssetPrice;
		private decimal _maxAssetPrice;

		private DateTimeOffset? _expiryDate;
		private decimal? _minStrike;
		private decimal? _maxStrike;

		private readonly object _token;

		private bool _changeFromCode;
		private DateTime? _currentDate;

		private Security _currentSecurity;

		public OptionDeskPanel()
		{
			InitializeComponent();

			AssetPrice.ValueChanged += AssetPrice_OnValueChanged;
			CurrentDate.ValueChanged += CurrentDate_OnValueChanged;

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ResetedCommand>(this, true, cmd =>
			{
				Desk.Options = GetOptions();

				lock (_needRefreshLock)
				_needRefresh = true;
			});

			var connector = ConfigManager.GetService<IConnector>();
			connector.NewSecurities += OnNewSecurities;

			var studioConnector = ConfigManager.GetService<IConnector>();
			studioConnector.SecuritiesChanged += OnSecuritiesChanged;

			Desk.MarketDataProvider = studioConnector;
			Desk.SecurityProvider = ConfigManager.GetService<IEntityRegistry>().Securities;

			_token = GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(() =>
			{
				lock (_needRefreshLock)
				{
				if (!_needRefresh)
					return;

					_needRefresh = false;
				}

				Desk.RefreshOptions();

				if (AssetPriceReset.IsEnabled)
					return;

				_changeFromCode = true;

				try
				{
					AssetPrice.Value = LastTradePrice;
				}
				finally
				{
					_changeFromCode = false;
				}
			});
		}

		private decimal? LastTradePrice => (decimal?)Desk.MarketDataProvider.GetSecurityValue(_currentSecurity, Level1Fields.LastTradePrice);

		private IEnumerable<Security> GetOptions()
		{
			return UnderlyingAsset.SelectedSecurity != null
				? Filter(UnderlyingAsset.SelectedSecurity.GetDerivatives(Desk.SecurityProvider, _expiryDate))
				: Enumerable.Empty<Security>();
		}

		private IEnumerable<Security> Filter(IEnumerable<Security> options)
		{
			return options.Where(o =>
			{
				if (_expiryDate != null && o.ExpiryDate != _expiryDate)
					return false;

				if (_minStrike != null && o.Strike < _minStrike)
					return false;

				if (_maxStrike != null && o.Strike > _maxStrike)
					return false;

				return true;
			});
		}

		//private void Desk_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		//{
		//	new SelectCommand<OptionDeskRow>(Desk.SelectedRow, false).Process(this);
		//}

		private void Desk_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			new ControlChangedCommand(this).Process(this);
		}

		public override void Save(SettingsStorage storage)
		{
			if (UnderlyingAsset.SelectedSecurity != null)
				storage.SetValue("UnderlyingAsset", UnderlyingAsset.SelectedSecurity.Id);

			storage.SetValue("Desk", Desk.Save());

			var expDate = ExpiryDate.Value;
			if (expDate != null)
				storage.SetValue("ExpiryDate", expDate.Value.To<long>());

			storage.SetValue("MinStrike", MinStrike.Value);
			storage.SetValue("MaxStrike", MaxStrike.Value);

			var currDate = CurrentDate.Value;
			if (currDate != null)
				storage.SetValue("CurrentDate", currDate.Value.To<long>());

			storage.SetValue("AssetPrice", AssetPrice.Value);
		}

		public override void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("UnderlyingAsset"))
				UnderlyingAsset.SelectedSecurity = ConfigManager.GetService<IEntityRegistry>().Securities.ReadById(storage.GetValue<string>("UnderlyingAsset"));

			Desk.Load(storage.GetValue<SettingsStorage>("Desk"));

			if (storage.ContainsKey("ExpiryDate"))
				ExpiryDate.Value = storage.GetValue<DateTime>("ExpiryDate");

			MinStrike.Value = storage.GetValue<decimal?>("MinStrike");
			MaxStrike.Value = storage.GetValue<decimal?>("MaxStrike");

			UseBlackMode.IsChecked = Desk.UseBlackModel;

			if (storage.ContainsKey("CurrentDate"))
				CurrentDate.Value = storage.GetValue<DateTime>("CurrentDate");

			AssetPrice.Value = storage.GetValue<decimal?>("AssetPrice");
		}

		public override void Dispose()
		{
			GuiDispatcher.GlobalDispatcher.RemovePeriodicalAction(_token);

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<ResetedCommand>(this);

			var connector = ConfigManager.GetService<IConnector>();
			connector.NewSecurities -= OnNewSecurities;
			connector.SecuritiesChanged -= OnSecuritiesChanged;
		}

		private IEnumerable<Security> GetStrikes(IEnumerable<Security> securities)
		{
			return Filter(securities
				.Where(s => s.Type == SecurityTypes.Option &&
					s.UnderlyingSecurityId.CompareIgnoreCase(_currentSecurity.Id)));
		}

		private void OnNewSecurities(IEnumerable<Security> securities)
		{
			if (_currentSecurity == null)
				return;

			var newStrikes = GetStrikes(securities).ToArray();

			if (newStrikes.Length <= 0)
				return;

			_options.AddRange(newStrikes);

			SetDateLimits();

			Desk.Options = _options.Cache;

			lock (_needRefreshLock)
				_needRefresh = true;
		}

		private void OnSecuritiesChanged(IEnumerable<Security> securities)
		{
			if (_currentSecurity == null)
				return;

			var hasUnderlying = false;
			var hasStrikes = false;

			var newStrikes = GetStrikes(securities)
				.Where(s =>
				{
					var isOld = _options.Contains(s);

					if (!hasUnderlying)
						hasUnderlying = s == _currentSecurity;

					if (!hasStrikes)
						hasStrikes = isOld;

					return !isOld;
				})
				.ToArray();

			if (newStrikes.Length > 0)
			{
				_options.AddRange(newStrikes);

				SetDateLimits();

					Desk.Options = _options.Cache;

				lock (_needRefreshLock)
					_needRefresh = true;

				if (_minAssetPrice == 0 && _maxAssetPrice == 0 && newStrikes.Contains(_currentSecurity))
					SetPriceLimits();

				return;
			}

			if (!hasUnderlying && !hasStrikes)
				return;

				if (_minAssetPrice == 0 && _maxAssetPrice == 0)
					SetPriceLimits();

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
				foreach (var option in _options.Cache)
					new RefuseMarketDataCommand(option, MarketDataTypes.Level1).Process(this);

				new RefuseMarketDataCommand(_currentSecurity, MarketDataTypes.Level1).Process(this);

				_currentSecurity = null;

				_options.Clear();

				Desk.Options = Enumerable.Empty<Security>();
				Desk.RefreshOptions();

				_minAssetPrice = _maxAssetPrice = 0;
				//_minDate = _maxDate = default(DateTime);

				Process(() =>
				{
					CurrentDate.Minimum = null;
					CurrentDate.Maximum = null;

					AssetPriceModified.Value = 0;
					AssetPriceReset.IsEnabled = false;

					CurrentDateModified.Value = 0;
					CurrentDateReset.IsEnabled = false;
				});
			}

			_currentSecurity = security;

			if (_currentSecurity != null)
			{
				_options.AddRange(GetOptions());

				foreach (var option in _options.Cache)
					new RequestMarketDataCommand(option, MarketDataTypes.Level1).Process(this);

				new RequestMarketDataCommand(_currentSecurity, MarketDataTypes.Level1).Process(this);

				Desk.Options = _options.Cache;
				Desk.RefreshOptions();

					SetPriceLimits();
					SetDateLimits();
			}

			Title = LocalizedStrings.Str3265 + " " + _currentSecurity;
		}

		private void SetPriceLimits()
		{
			var minPrice = (decimal?)Desk.MarketDataProvider.GetSecurityValue(_currentSecurity, Level1Fields.MinPrice);
			var maxPrice = (decimal?)Desk.MarketDataProvider.GetSecurityValue(_currentSecurity, Level1Fields.MaxPrice);

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

			GuiDispatcher.GlobalDispatcher.AddAction(() => Process(() =>
			{
				AssetPriceModified.Minimum = (double)_minAssetPrice;
				AssetPriceModified.Maximum = (double)_maxAssetPrice;
				AssetPriceModified.Value = (double)(_minAssetPrice + _maxAssetPrice) / 2;
			}));
		}

		private void OnFilterChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			_expiryDate = ExpiryDate.Value;
			_minStrike = MinStrike.Value;
			_maxStrike = MaxStrike.Value;

			var options = GetOptions();

			_options.Clear();
			_options.AddRange(options);

			SetDateLimits();

			Desk.Options = _options.Cache;
			Desk.RefreshOptions();
		}

		private void ExpiryDate_ValueChanged(object sender, PropertyChangedRoutedEventArgs<DateTime?> e)
		{
			OnFilterChanged(sender, null);
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

			AssetPriceModified.Value = (double)newValue.Value;

			if (_changeFromCode)
				return;

			AssetPriceReset.IsEnabled = true;

			Desk.AssetPrice = newValue.Value;
			Desk.RefreshOptions();
		}

		private void AssetPriceReset_OnClick(object sender, RoutedEventArgs e)
		{
			AssetPriceReset.IsEnabled = false;

			Desk.AssetPrice = null;
			Desk.RefreshOptions();
		}

		private void CurrentDateModified_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var newValue = (double?)e.NewValue;

			if (CurrentDate.Minimum == null)
				return;

			var date = CurrentDate.Minimum.Value.AddDays(newValue.Value);

			if (_currentDate == date)
				return;

			CurrentDate.Value = date;
		}

		private void CurrentDate_OnValueChanged(object sender, PropertyChangedRoutedEventArgs<DateTime?> e)
		{
			var newValue = e.NewValue;
			var min = CurrentDate.Minimum;

			if (newValue == null)
			{
				CurrentDateModified.Value = _currentSecurity == null || min == null ? 0 : (TimeHelper.Now - min.Value).TotalDays;
				CurrentDateReset_OnClick(sender, e);
				return;
			}

			if (_currentDate == newValue.Value)
				return;

			_currentDate = newValue.Value;
			CurrentDateModified.Value = min == null ? 0 : (_currentDate.Value - min.Value).TotalDays;

			if (_changeFromCode)
				return;

			CurrentDateReset.IsEnabled = true;

			Desk.CurrentTime = _currentDate;
			Desk.RefreshOptions();
		}

		private void CurrentDateReset_OnClick(object sender, RoutedEventArgs e)
		{
			CurrentDateReset.IsEnabled = false;

			_currentDate = null;
			Desk.CurrentTime = null;
			Desk.RefreshOptions();

			Process(() => CurrentDate.Value = TimeHelper.Now);
		}

		private void UseBlackMode_OnClick(object sender, RoutedEventArgs e)
		{
			Desk.UseBlackModel = UseBlackMode.IsChecked == true;
			Desk.RefreshOptions();
		}

		private void SetDateLimits()
		{
			var options = _options.Cache;

			var maxExpSec = options.OrderByDescending(s => s.ExpiryDate).FirstOrDefault() ?? _currentSecurity;
			var minExpSec = options.OrderBy(s => s.ExpiryDate).FirstOrDefault() ?? _currentSecurity;

			var maxDate = (maxExpSec == null ? null : maxExpSec.ExpiryDate) ?? DateTime.Today + TimeSpan.FromDays(30);
			var minDate = (minExpSec == null ? null : minExpSec.ExpiryDate) ?? DateTime.Today.Subtract(TimeSpan.FromDays(30));

			var diff = (maxDate - minDate).TotalDays;

			if (Math.Abs(diff) < double.Epsilon)
				diff = 0;

			GuiDispatcher.GlobalDispatcher.AddAction(() => Process(() =>
			{
				CurrentDate.Minimum = minDate.LocalDateTime;
				CurrentDate.Maximum = maxDate.LocalDateTime;

				CurrentDateModified.Minimum = 0;
				CurrentDateModified.Maximum = diff;
				CurrentDateModified.Value = diff;
			}));
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