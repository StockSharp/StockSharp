#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: Level1Pane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.Primitives;

	public partial class Level1Pane
	{
		private readonly Dictionary<Level1Fields, DataGridColumn> _columns = new Dictionary<Level1Fields, DataGridColumn>();

		public Level1Pane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetMessages);

			Level1FieldsCtrl.SelectedFields = Enumerator.GetValues<Level1Fields>();

			foreach (var column in FindedChanges.Columns)
			{
				Level1Fields field;

				if (!Enum.TryParse(column.SortMemberPath, out field))
					continue;

				_columns.Add(field, column);
			}
		}

		protected override Type DataType => typeof(Level1ChangeMessage);

		public override string Title => LocalizedStrings.Level1 + " " + SelectedSecurity;

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private IEnumerable<Level1ChangeMessage> GetMessages()
		{
			var excludedTypes = Enumerator
				.GetValues<Level1Fields>()
				.Except(Level1FieldsCtrl.SelectedFields)
				.ToArray();

			var messages = StorageRegistry
				.GetLevel1MessageStorage(SelectedSecurity, Drive, StorageFormat)
				.Load(From, To + TimeHelper.LessOneDay);

			return messages
				.Select(m =>
				{
					excludedTypes.ForEach(t => m.Changes.Remove(t));
					return m;
				})
				.Where(m => m.Changes.Any());
		}

		private bool CheckExportTypes()
		{
			if (Level1FieldsCtrl.SelectedFields.Any() || ExportBtn.ExportType == ExportTypes.StockSharp)
				return true;

			new MessageBoxBuilder()
				.Error()
				.Owner(this)
				.Text(LocalizedStrings.Str2935)
				.Show();

			return false;
		}

		protected override void ExportBtnOnExportStarted()
		{
			if (!CheckExportTypes())
				return;

			base.ExportBtnOnExportStarted();
		}

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (!CheckSecurity())
				return;

			if (!CheckExportTypes())
				return;

			var messages = new ObservableCollection<Level1ChangeMessage>();

			FindedChanges.ItemsSource = messages;

			Progress.Load(GetMessages(), messages.AddRange, 10000);
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

		private void Level1FieldsCtrl_OnItemSelectionChanged(object sender, ItemSelectionChangedEventArgs e)
		{
			var pair = (KeyValuePair<Level1Fields, string>)e.Item;

			var column = _columns.TryGetValue(pair.Key);

			if (column == null)
				return;

			column.Visibility = e.IsSelected ? Visibility.Visible : Visibility.Collapsed;
		}

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			if (storage.ContainsKey("SelectedLevel1Fields"))
			{
				Level1FieldsCtrl.SelectedFields = storage
					.GetValue<string>("SelectedLevel1Fields")
					.Split(",")
					.Select(s => s.To<Level1Fields>())
					.ToArray();
			}

			FindedChanges.Load(storage.GetValue<SettingsStorage>(nameof(FindedChanges)));

			var selectedFields = Level1FieldsCtrl.SelectedFields.ToArray();

			foreach (var pair in _columns)
			{
				pair.Value.Visibility = selectedFields.Contains(pair.Key) ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("SelectedLevel1Fields", Level1FieldsCtrl.SelectedFields.Select(f => f.ToString()).Join(","));
			storage.SetValue(nameof(FindedChanges), FindedChanges.Save());
		}
	}
}