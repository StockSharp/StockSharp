#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: TradesPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class TradesPane
	{
		public TradesPane()
		{
			InitializeComponent();

			FindedTrades.HideColumns(ExecutionTypes.Tick);
			Init(ExportBtn, MainGrid, GetTrades);
		}

		protected override Type DataType => typeof(ExecutionMessage);

		protected override object Arg => ExecutionTypes.Tick;

		public override string Title => LocalizedStrings.Str985 + " " + SelectedSecurity;

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private IEnumerable<ExecutionMessage> GetTrades()
		{
			switch (BuildFrom.SelectedIndex)
			{
				case 0:
				{
					IEnumerable<ExecutionMessage> trades = StorageRegistry
						.GetTickMessageStorage(SelectedSecurity, Drive, StorageFormat)
						.Load(From, To + TimeHelper.LessOneDay);

					if (IsNonSystem.IsChecked == false)
						trades = trades.Where(t => t.IsSystem != false);

					return trades;
				}

				case 1:
				{
					IEnumerable<ExecutionMessage> orderLog = StorageRegistry
						.GetOrderLogMessageStorage(SelectedSecurity, Drive, StorageFormat)
						.Load(From, To + TimeHelper.LessOneDay);

					if (IsNonSystem.IsChecked == false)
						orderLog = orderLog.Where(i => i.IsSystem != false);

					return orderLog.ToTicks();
				}

				case 2:
				{
					var level1 = StorageRegistry
						.GetLevel1MessageStorage(SelectedSecurity, Drive, StorageFormat)
						.Load(From, To + TimeHelper.LessOneDay);

					return level1.ToTicks();
				}

				default:
					throw new InvalidOperationException();
			}
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (!CheckSecurity())
				return;

			FindedTrades.Messages.Clear();
			Progress.Load(GetTrades(), FindedTrades.Messages.AddRange, 10000);
		}

		protected override bool CanDirectExport => BuildFrom.SelectedIndex == 0;

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		private void SelectSecurityBtn_SecuritySelected()
		{
			if (SelectedSecurity == null)
			{
				ExportBtn.IsEnabled = false;
			}
			else
			{
				ExportBtn.IsEnabled = true;
				UpdateTitle();
			}
		}

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			FindedTrades.Load(storage.GetValue<SettingsStorage>(nameof(FindedTrades)));
			BuildFrom.SelectedIndex = storage.GetValue<int>(nameof(BuildFrom));
			IsNonSystem.IsChecked = storage.GetValue<bool>(nameof(IsNonSystem));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(FindedTrades), FindedTrades.Save());
			storage.SetValue(nameof(BuildFrom), BuildFrom.SelectedIndex);
			storage.SetValue(nameof(IsNonSystem), IsNonSystem.IsChecked == true);
		}
	}
}