#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: OrderLogPane.xaml.cs
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

		protected override Type DataType => typeof(ExecutionMessage);

		protected override object Arg => ExecutionTypes.OrderLog;

		public override string Title => LocalizedStrings.OrderLog + " " + SelectedSecurity;

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private IEnumerable<ExecutionMessage> GetOrderLog()
		{
			IEnumerable<ExecutionMessage> orderLog = StorageRegistry
				.GetOrderLogMessageStorage(SelectedSecurity, Drive, StorageFormat)
				.Load(From, To + TimeHelper.LessOneDay);

			if (IsNonSystem.IsChecked == false)
				orderLog = orderLog.Where(o => o.IsSystem != false);

			return orderLog;
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (!CheckSecurity())
				return;

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

			FindedOrderLog.Load(storage.GetValue<SettingsStorage>(nameof(FindedOrderLog)));
			IsNonSystem.IsChecked = storage.GetValue<bool>(nameof(IsNonSystem));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(FindedOrderLog), FindedOrderLog.Save());
			storage.SetValue(nameof(IsNonSystem), IsNonSystem.IsChecked == true);
		}
	}
}