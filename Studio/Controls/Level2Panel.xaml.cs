#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: Level2Panel.xaml.cs
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
	using System.Windows.Controls;

	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

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
		private readonly HashSet<string> _askDefaultColumns = new HashSet<string> { "Board", "BestAsk.Price", "BestAsk.Volume" };
		private readonly HashSet<string> _bidDefaultColumns = new HashSet<string> { "Board", "BestBid.Price", "BestBid.Volume" };

		private Security[] _securities;

		private static ISecurityProvider SecurityProvider => ConfigManager.GetService<ISecurityProvider>();

		private BuySellSettings Settings => BuySellPanel.Settings;

		public Level2Panel()
		{
			InitializeComponent();

			SecurityAsksGrid.PropertyChanged += SecurityGridPropertyChanged;
			SecurityAsksGrid.SelectionChanged += SecuritiesCtrlOnSelectionChanged;

			SecurityBidsGrid.PropertyChanged += SecurityGridPropertyChanged;
			SecurityBidsGrid.SelectionChanged += SecuritiesCtrlOnSelectionChanged;

			SecurityPicker.SecuritySelected += SecurityPickerSecuritySelected;

			SecurityPicker.SecurityProvider = SecurityProvider;

			SecurityAsksGrid.MarketDataProvider = SecurityBidsGrid.MarketDataProvider = ConfigManager.GetService<IMarketDataProvider>();

			SetVisibleColumns(SecurityAsksGrid, _askDefaultColumns);
			SetVisibleColumns(SecurityBidsGrid, _bidDefaultColumns);
		}

		private static void SetVisibleColumns(DataGrid grid, HashSet<string> columns)
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
			storage.SetValue("SecurityId", SecurityPicker.SelectedSecurity?.Id);

			storage.SetValue("SecurityAsksGrid", SecurityAsksGrid.Save());
			storage.SetValue("SecurityBidsGrid", SecurityBidsGrid.Save());

			storage.SetValue("BuySellSettings", BuySellPanel.Save());
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title => "Level2";

		Uri IStudioControl.Icon => null;
	}
}
