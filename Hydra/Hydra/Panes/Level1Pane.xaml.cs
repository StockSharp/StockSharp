namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class Level1Pane
	{
		public Level1Pane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetMessages);

			Level1FieldsCtrl.SelectedFields = Enumerator.GetValues<Level1Fields>();
		}

		protected override Type DataType
		{
			get { return typeof(Level1ChangeMessage); }
		}

		public override string Title
		{
			get { return LocalizedStrings.Str2934 + " " + SelectedSecurity; }
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

		private IEnumerableEx<Level1ChangeMessage> GetMessages()
		{
			var types = new HashSet<Level1Fields>(Level1FieldsCtrl.SelectedFields);

			var messages = StorageRegistry
				.GetLevel1MessageStorage(SelectedSecurity, Drive, StorageFormat)
				.Load(From, To + TimeHelper.LessOneDay);

			return messages
				.Where(m => types.IsSupersetOf(m.Changes.Keys))
				.ToEx(messages.Count);
		}

		private bool CheckExportTypes()
		{
			if (Level1FieldsCtrl.SelectedFields.Any() || ExportBtn.ExportType == ExportTypes.Bin)
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

			FindedChanges.Load(storage.GetValue<SettingsStorage>("FindedChanges"));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("SelectedLevel1Fields", Level1FieldsCtrl.SelectedFields.Select(f => f.ToString()).Join(","));
			storage.SetValue("FindedChanges", FindedChanges.Save());
		}
	}
}