#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: StrategySecuritiesPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.SecuritiesKey)]
	[DescriptionLoc(LocalizedStrings.Str3274Key)]
	[Icon("images/security_32x32.png")]
	public partial class StrategySecuritiesPanel
	{
		public static readonly RoutedCommand OpenMarketDepthCommand = new RoutedCommand();

		private readonly SynchronizedSet<string> _securityIds = new SynchronizedSet<string>(StringComparer.InvariantCultureIgnoreCase);

		private readonly string _defaultToolTipText;
		private readonly ToolTip _newSecuritiesTooltip;
		private readonly Brush _defaultStorageBrush;

		private bool _isTooltipVisible;

		private Security[] Securities
		{
			get
			{
				var entityRegistry = ConfigManager.GetService<IStudioEntityRegistry>();

				return _securityIds
					.Select(id => entityRegistry.Securities.ReadById(id))
					.Where(s => s != null)
					.ToArray();
			}
		}

		public StrategySecuritiesPanel()
		{
			InitializeComponent();

			SecurityPicker.GridChanged += RaiseChangedCommand;
			AlertBtn.SchemaChanged += RaiseChangedCommand;

			GotFocus += (s, e) => RaiseSelectedCommand();

			_newSecuritiesTooltip = (ToolTip)AddSecurity.ToolTip;
			_defaultStorageBrush = ((TextBlock)_newSecuritiesTooltip.Content).Foreground;
			_defaultToolTipText = ((TextBlock)_newSecuritiesTooltip.Content).Text;

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ResetedCommand>(this, false, cmd =>
			{
				var selectedSecurities = Securities;

				selectedSecurities.ForEach(RaiseRefuseMarketData);
				selectedSecurities.ForEach(RaiseRequestMarketData);
			});
			cmdSvc.Register<NewSecuritiesCommand>(this, false, cmd =>
			{
				if (_isTooltipVisible)
					return;

				_isTooltipVisible = true;

				GuiDispatcher.GlobalDispatcher.AddAction(() =>
				{
					((TextBlock)_newSecuritiesTooltip.Content).Text = LocalizedStrings.Str3276;
					((TextBlock)_newSecuritiesTooltip.Content).Foreground = Brushes.Red;
					_newSecuritiesTooltip.Placement = PlacementMode.Bottom;
					_newSecuritiesTooltip.PlacementTarget = AddSecurity;
					_newSecuritiesTooltip.IsOpen = true;
				});
			});
			cmdSvc.Register<BindStrategyCommand>(this, false, cmd =>
			{
				if (!cmd.CheckControl(this))
					return;

				if (_securityIds.Count == 0)
				{
					_securityIds.Add(cmd.Source.Security.Id);

					AddDefaultSecurities("RI");
					AddDefaultSecurities("Si");
					AddDefaultSecurities("GZ");
				}

				var selectedSecurities = Securities;

				SecurityPicker.Securities.AddRange(selectedSecurities);
				selectedSecurities.ForEach(RaiseRequestMarketData);

				GuiDispatcher.GlobalDispatcher.AddAction(() =>
				{
					SecurityPicker.AddContextMenuItem(new Separator());
					SecurityPicker.AddContextMenuItem(new MenuItem { Header = LocalizedStrings.Str3277, Command = OpenMarketDepthCommand, CommandTarget = this });
				});
			});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		private void AddDefaultSecurities(string baseCode)
		{
			baseCode
				.GetFortsJumps(DateTime.Today.AddMonths(-3), DateTime.Today.AddMonths(6), code => new Security
				{
					Id = code + "@" + ExchangeBoard.Forts.Code,
					Code = code,
					Board = ExchangeBoard.Forts,
				})
				.Take(3)
				.ForEach(s => _securityIds.Add(s.Id));
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		private void RaiseSelectedCommand()
		{
			new SelectCommand<Security>(SecurityPicker.SelectedSecurity, false).Process(this);
		}

		private void RaiseRequestMarketData(Security security)
		{
			new RequestMarketDataCommand(security, MarketDataTypes.Level1).Process(this);
		}

		private void RaiseRefuseMarketData(Security security)
		{
			new RefuseMarketDataCommand(security, MarketDataTypes.Level1).Process(this);
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<ResetedCommand>(this);
			cmdSvc.UnRegister<NewSecuritiesCommand>(this);
			cmdSvc.UnRegister<BindStrategyCommand>(this);

			AlertBtn.Dispose();
		}

		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("SecurityPicker", SecurityPicker.Save());
			storage.SetValue("AlertSettings", AlertBtn.Save());
			storage.SetValue("Securities", _securityIds.ToArray());
		}

		public override void Load(SettingsStorage storage)
		{
			var gridSettings = storage.GetValue<SettingsStorage>("SecurityPicker");
			if (gridSettings != null)
				SecurityPicker.Load(gridSettings);

			var alertSettings = storage.GetValue<SettingsStorage>("AlertSettings");
			if (alertSettings != null)
				AlertBtn.Load(alertSettings);

			_securityIds.SyncDo(list =>
			{
				list.Clear();
				list.AddRange(storage.GetValue("Securities", ArrayHelper.Empty<string>()));
			});

			SecurityPicker.SecurityProvider = ConfigManager.GetService<ISecurityProvider>();
		}

		private void SecurityPicker_SecuritySelected(Security security)
		{
			RemoveSecurity.IsEnabled = security != null;
			RaiseSelectedCommand();
		}

		private void SecurityPicker_SecurityDoubleClick(Security security)
		{
			new EditSecurityCommand(security).Process(this);
		}

		private void RemoveSecurity_Click(object sender, RoutedEventArgs e)
		{
			SecurityPicker
				.SelectedSecurities
				.ForEach(ProcessRemoveSecurity);
		}

		private void AddSecurity_Click(object sender, RoutedEventArgs e)
		{
			_isTooltipVisible = false;

			((TextBlock)_newSecuritiesTooltip.Content).Foreground = _defaultStorageBrush;
			((TextBlock)_newSecuritiesTooltip.Content).Text = _defaultToolTipText;
			_newSecuritiesTooltip.IsOpen = false;

			var window = new SecuritiesWindowEx
			{
				SecurityProvider = ConfigManager.GetService<ISecurityProvider>()
			};

			var selectedSecurities = Securities;

			window.SelectSecurities(selectedSecurities);

			if (!window.ShowModal(this))
				return;

			var toRemove = selectedSecurities.Except(window.SelectedSecurities).ToArray();
			var toAdd = window.SelectedSecurities.Except(selectedSecurities).ToArray();

			toRemove.ForEach(ProcessRemoveSecurity);
			toAdd.ForEach(ProcessAddSecurity);

			new ControlChangedCommand(this).Process(this);
		}

		private void ProcessAddSecurity(Security security)
		{
			_securityIds.Add(security.Id);

			SecurityPicker.Securities.Add(security);
			RaiseRequestMarketData(security);
		}

		private void ProcessRemoveSecurity(Security security)
		{
			_securityIds.Remove(security.Id);

			SecurityPicker.Securities.Remove(security);
			RaiseRefuseMarketData(security);
		}

		private void ExecutedOpenMarketDepthCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenMarketDepthCommand(SecurityPicker.SelectedSecurity).SyncProcess(this);
		}

		private void CanExecuteOpenMarketDepthCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SecurityPicker != null && SecurityPicker.SelectedSecurity != null;
		}
	}
}
