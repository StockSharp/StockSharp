namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
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

		protected override Type DataType
		{
			get { return typeof(ExecutionMessage); }
		}

		protected override object Arg
		{
			get { return ExecutionTypes.Tick; }
		}

		public override string Title
		{
			get { return LocalizedStrings.Str985 + " " + SelectedSecurity; }
		}

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private IEnumerableEx<ExecutionMessage> GetTrades()
		{
			switch (BuildFrom.SelectedIndex)
			{
				case 0:
				{
					var trades = StorageRegistry
						.GetExecutionStorage(SelectedSecurity, ExecutionTypes.Tick, Drive, StorageFormat)
						.Load(From, To + TimeHelper.LessOneDay);

					if (IsNonSystem.IsChecked == false)
						trades = trades.Where(t => t.IsSystem != false).ToEx(trades.Count);

					return trades;
				}

				case 1:
				{
					var orderLog = StorageRegistry
						.GetExecutionStorage(SelectedSecurity, ExecutionTypes.OrderLog, Drive, StorageFormat)
						.Load(From, To + TimeHelper.LessOneDay);

					if (IsNonSystem.IsChecked == false)
						orderLog = orderLog.Where(i => i.IsSystem != false).ToEx(orderLog.Count);

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

		protected override bool CanDirectBinExport
		{
			get { return base.CanDirectBinExport && BuildFrom.SelectedIndex == 0; }
		}

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

			FindedTrades.Load(storage.GetValue<SettingsStorage>("FindedTrades"));
			BuildFrom.SelectedIndex = storage.GetValue<int>("BuildFrom");
			IsNonSystem.IsChecked = storage.GetValue<bool>("IsNonSystem");
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("FindedTrades", FindedTrades.Save());
			storage.SetValue("BuildFrom", BuildFrom.SelectedIndex);
			storage.SetValue("IsNonSystem", IsNonSystem.IsChecked == true);
		}
	}
}