#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: ExecutionsPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class TransactionsPane
	{
		public TransactionsPane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetTransactions);
		}

		protected override object Arg => ExecutionTypes.Transaction;

		protected override Type DataType => typeof(ExecutionMessage);

		public override string Title => LocalizedStrings.Transactions + " " + SelectedSecurity;

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private IEnumerable<ExecutionMessage> GetTransactions()
		{
			var executions = StorageRegistry
				.GetTransactionStorage(SelectedSecurity, Drive, StorageFormat)
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

			FindedTransactions.Messages.Clear();
			Progress.Load(GetTransactions(), FindedTransactions.Messages.AddRange, 10000);
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

			FindedTransactions.Load(storage.GetValue<SettingsStorage>(nameof(FindedTransactions)));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(FindedTransactions), FindedTransactions.Save());
		}
	}
}
