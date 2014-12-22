namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	[DisplayName("Level2")]
	[DescriptionLoc(LocalizedStrings.Str3236Key)]
	[Icon("images/level2_16x16.png")]
	public partial class Level2Panel : IStudioControl
	{
		private readonly string[] _askDefaultColumns = { "Board", "BestAsk.Price", "BestAsk.Volume" };
		private readonly string[] _bidDefaultColumns = { "Board", "BestBid.Price", "BestBid.Volume" };

		private Security[] _securities;

		private static FilterableSecurityProvider SecurityProvider { get { return ConfigManager.GetService<FilterableSecurityProvider>(); } }

		private BuySellSettings Settings
		{
			get { return BuySellPanel.Settings; }
		}

		public Level2Panel()
		{
			InitializeComponent();

			SecurityAsksGrid.PropertyChanged += SecurityGridPropertyChanged;
			SecurityAsksGrid.SelectionChanged += SecuritiesCtrlOnSelectionChanged;

			SecurityBidsGrid.PropertyChanged += SecurityGridPropertyChanged;
			SecurityBidsGrid.SelectionChanged += SecuritiesCtrlOnSelectionChanged;

			SecurityPicker.SecuritySelected += SecurityPickerSecuritySelected;
			SecurityPicker.SecurityProvider = SecurityProvider;

			SetVisibleColumns(SecurityAsksGrid, _askDefaultColumns);
			SetVisibleColumns(SecurityBidsGrid, _bidDefaultColumns);
		}

		private static void SetVisibleColumns(DataGrid grid, IEnumerable<string> columns)
		{
			foreach (var column in grid.Columns)
			{
				column.Visibility = columns.Contains(column.SortMemberPath) ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		private void SecurityPickerSecuritySelected()
		{
			var security = SecurityPicker.SelectedSecurity;

			var securities = SecurityProvider.LookupByCode(security.Code).ToArray();

			if (_securities != null)
				_securities.ForEach(s => new RefuseMarketDataCommand(s, MarketDataTypes.Level1).Process(this));

			SecurityAsksGrid.Securities.Clear();
			SecurityBidsGrid.Securities.Clear();

			_securities = securities;

			if (_securities == null)
				return;

			_securities.ForEach(s => new RequestMarketDataCommand(s, MarketDataTypes.Level1).Process(this));

			SecurityAsksGrid.Securities.AddRange(_securities);
			SecurityBidsGrid.Securities.AddRange(_securities);
		}

		private void SecurityGridPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (sender == Settings && e.PropertyName == "Security")
				return;

			RaiseChangedCommand();
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		private void SecuritiesCtrlOnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Settings.Security = ((SecurityGrid)sender).SelectedSecurity;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			var securityId = storage.GetValue<string>("SecurityId");
			if (securityId != null)
				SecurityPicker.SelectedSecurity = SecurityProvider.LookupById(securityId);

			var asksGridSettings = storage.GetValue<SettingsStorage>("SecurityAsksGrid");
			if (asksGridSettings != null)
				SecurityAsksGrid.Load(asksGridSettings);

			var bidsGridSettings = storage.GetValue<SettingsStorage>("SecurityBidsGrid");
			if (bidsGridSettings != null)
				SecurityBidsGrid.Load(bidsGridSettings);

			var buySellSettings = storage.GetValue<SettingsStorage>("BuySellSettings");
			if (buySellSettings != null)
				BuySellPanel.Load(buySellSettings);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("SecurityId", SecurityPicker.SelectedSecurity != null ? SecurityPicker.SelectedSecurity.Id : null);

			storage.SetValue("SecurityAsksGrid", SecurityAsksGrid.Save());
			storage.SetValue("SecurityBidsGrid", SecurityBidsGrid.Save());

			storage.SetValue("BuySellSettings", BuySellPanel.Save());
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title
		{
			get { return "Level2"; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}
	}
}
