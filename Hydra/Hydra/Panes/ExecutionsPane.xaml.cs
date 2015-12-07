namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class ExecutionsPane
	{
		public ExecutionsPane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetExecutions);
		}

		protected override object Arg => ExecutionTypes.Order;

		protected override Type DataType => typeof(ExecutionMessage);

		public override string Title => LocalizedStrings.Transactions + " " + SelectedSecurity;

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private IEnumerableEx<ExecutionMessage> GetExecutions()
		{
			var executions = StorageRegistry
				.GetExecutionStorage(SelectedSecurity, ExecutionTypes.Order, Drive, StorageFormat)
				.Load(From, To + TimeHelper.LessOneDay);

			return executions;
		}

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (!CheckSecurity())
				return;

			FindedExecutions.Messages.Clear();
			Progress.Load(GetExecutions(), FindedExecutions.Messages.AddRange, 10000);
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

			FindedExecutions.Load(storage.GetValue<SettingsStorage>("FindedExecutions"));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("FindedExecutions", FindedExecutions.Save());
		}
	}
}
