namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class OrderLogPane
	{
		public OrderLogPane()
		{
			InitializeComponent();

			FindedOrderLog.HideColumns(ExecutionTypes.OrderLog);
			Init(ExportBtn, MainGrid, GetOrderLog);
		}

		protected override Type DataType
		{
			get { return typeof(ExecutionMessage); }
		}

		protected override object Arg
		{
			get { return ExecutionTypes.OrderLog; }
		}

		public override string Title
		{
			get { return LocalizedStrings.Str2908 + SelectedSecurity; }
		}

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		public override bool InProcess
		{
			get { return Progress.IsStarted; }
		}

		private IEnumerableEx<ExecutionMessage> GetOrderLog()
		{
			var orderLog = StorageRegistry
				.GetExecutionStorage(SelectedSecurity, ExecutionTypes.OrderLog, Drive, StorageFormat)
				.Load(From, To + TimeHelper.LessOneDay);

			if (IsNonSystem.IsChecked == false)
				orderLog = orderLog.Where(o => o.IsSystem).ToEx(orderLog.Count);

			return orderLog;
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (SelectedSecurity == null)
			{
				new MessageBoxBuilder()
					.Caption(Title)
					.Text(LocalizedStrings.Str2875)
					.Info()
					.Owner(this)
					.Show();

				return;
			}

			FindedOrderLog.Messages.Clear();
			Progress.Load(GetOrderLog(), FindedOrderLog.Messages.AddRange, 10000);
		}

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		//protected override void OnClosed(EventArgs e)
		//{
		//    Progress.Stop();
		//    base.OnClosed(e);
		//}

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

			FindedOrderLog.Load(storage.GetValue<SettingsStorage>("FindedOrderLog"));
			IsNonSystem.IsChecked = storage.GetValue<bool>("IsNonSystem");
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("FindedOrderLog", FindedOrderLog.Save());
			storage.SetValue("IsNonSystem", IsNonSystem.IsChecked == true);
		}
	}
}