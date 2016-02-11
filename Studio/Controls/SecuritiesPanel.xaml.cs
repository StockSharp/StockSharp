#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: SecuritiesPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.SecuritiesKey)]
	[DescriptionLoc(LocalizedStrings.Str3274Key)]
	[Icon("images/security_32x32.png")]
	public partial class SecuritiesPanel
	{
		public static RoutedCommand CreateSecurityCommand = new RoutedCommand();
		public static RoutedCommand SaveSecurityCommand = new RoutedCommand();
		public static RoutedCommand CancelSecurityCommand = new RoutedCommand();

		private static readonly string[] _defaultColumns =
		{
			"PriceStep", "StepPrice", "VolumeStep", "Multiplier", "Decimals",
			"UnderlyingSecurityId", "Strike", "OptionType",
			"Currency", "ExpiryDate", "ExternalId.Isin", "ExternalId.Ric"
		};

		private readonly SynchronizedSet<string> _securityIds = new SynchronizedSet<string>(StringComparer.InvariantCultureIgnoreCase);
		private bool _isNew;
		private bool _changed;

		//private Security[] Securities
		//{
		//	get
		//	{
		//		var entityRegistry = ConfigManager.GetService<IStudioEntityRegistry>();

		//		return _securityIds
		//			.Select(id => entityRegistry.Securities.ReadById(id))
		//			.Where(s => s != null)
		//			.ToArray();
		//	}
		//}

		public SecuritiesPanel()
		{
			InitializeComponent();

			_defaultColumns.ForEach(c => SecurityPicker.SetColumnVisibility(c, Visibility.Visible));

			SecurityPicker.GridChanged += RaiseChangedCommand;

			GotFocus += (s, e) => RaiseSelectedCommand();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			//cmdSvc.Register<ResetedCommand>(this, false, cmd =>
			//{
			//	var selectedSecurities = Securities;

			//	selectedSecurities.ForEach(RaiseRefuseMarketData);
			//	selectedSecurities.ForEach(RaiseRequestMarketData);
			//});
			cmdSvc.Register<BindConnectorCommand>(this, true, cmd =>
			{
				if (!cmd.CheckControl(this))
					return;

				SecurityPicker.SecurityProvider = ConfigManager.GetService<ISecurityProvider>();
			});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		private void RaiseSelectedCommand()
		{
			new SelectCommand<Security>(SecurityPicker.SelectedSecurity, false).Process(this);
		}

		//private void RaiseRequestMarketData(Security security)
		//{
		//	new RequestMarketDataCommand(security, MarketDataTypes.Level1).Process(this);
		//}

		//private void RaiseRefuseMarketData(Security security)
		//{
		//	new RefuseMarketDataCommand(security, MarketDataTypes.Level1).Process(this);
		//}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			//cmdSvc.UnRegister<ResetedCommand>(this);
			cmdSvc.UnRegister<BindConnectorCommand>(this);
		}

		//TODO: дописать логику сохранения состояния для DockSite
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("SecurityPicker", SecurityPicker.Save());
			//storage.SetValue("Layout", DockSite.SaveLayout());
			storage.SetValue("Securities", _securityIds.ToArray());
		}

		//TODO: дописать логику загрузки состояния для DockSite
		public override void Load(SettingsStorage storage)
		{
			var gridSettings = storage.GetValue<SettingsStorage>("SecurityPicker");

			if (gridSettings != null)
				SecurityPicker.Load(gridSettings);

			var layout = storage.GetValue<string>("Layout");

			//if (layout != null)
			//	DockSite.LoadLayout(layout);

			_securityIds.SyncDo(list =>
			{
				list.Clear();
				list.AddRange(storage.GetValue("Securities", ArrayHelper.Empty<string>()));
			});

			SecurityPicker.SecurityProvider = ConfigManager.GetService<ISecurityProvider>();
		}

		private void SecurityPicker_SecuritySelected(Security security)
		{
			var oldSecurity = (Security)PropertyGrid.SelectedObject;
			if (oldSecurity != null)
				((INotifyPropertyChanged)oldSecurity).PropertyChanged -= SecurityPropertyChanged;

			var newSecurity = security == null ? null : security.Clone();

			if (newSecurity != null)
				((INotifyPropertyChanged)newSecurity).PropertyChanged += SecurityPropertyChanged;

			_changed = false;
			PropertyGrid.SelectedObject = newSecurity;
		}

		private void SecurityPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			_changed = true;
		}

		private void ExecutedCreateSecurityCommand(object sender, ExecutedRoutedEventArgs e)
		{
			_isNew = true;
			PropertyGrid.SelectedObject = new Security
			{
				ExtensionInfo = new Dictionary<object, object>()
			};
		}

		private void CanExecuteCreateSecurityCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedSaveSecurityCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var entityRegistry = ConfigManager.GetService<IStudioEntityRegistry>();
			var security = (Security)PropertyGrid.SelectedObject;

			if (_isNew)
			{
				var mbBuilder = new MessageBoxBuilder()
					.Owner(this)
					.Error();

				if (security.Code.IsEmpty())
				{
					mbBuilder.Text(LocalizedStrings.Str2923).Show();
					return;
				}

				if (security.Board == null)
				{
					mbBuilder.Text(LocalizedStrings.Str2926).Show();
					return;
				}

				if (security.PriceStep == null || security.PriceStep == 0)
				{
					mbBuilder.Text(LocalizedStrings.Str2925).Show();
					return;
				}

				if (security.VolumeStep == null || security.VolumeStep == 0)
				{
					mbBuilder.Text(LocalizedStrings.Str2924).Show();
					return;
				}

				var id = new SecurityIdGenerator().GenerateId(security.Code, security.Board);

				if (entityRegistry.Securities.ReadById(id) != null)
				{
					mbBuilder.Text(LocalizedStrings.Str2927Params.Put(id)).Show();
					return;
				}

				security.Id = id;
			}
			else
			{
				security.CopyTo(SecurityPicker.SelectedSecurity);
				security = SecurityPicker.SelectedSecurity;
			}

			entityRegistry.Securities.Save(security);

			_isNew = false;
			_changed = false;
		}

		private void CanExecuteSaveSecurityCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _isNew || _changed;
		}

		private void ExecutedCancelSecurityCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var security = (Security)PropertyGrid.SelectedObject;

			if (_isNew)
				PropertyGrid.SelectedObject = null;
			else
				SecurityPicker.SelectedSecurity.CopyTo(security);

			_isNew = false;
			_changed = false;
		}

		private void CanExecuteCancelSecurityCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _isNew || _changed;
		}
	}
}