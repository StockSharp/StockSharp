#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: PortfoliosPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;
	using StockSharp.Messages;

	[DisplayNameLoc(LocalizedStrings.PortfoliosKey)]
	[DescriptionLoc(LocalizedStrings.Str3269Key)]
	[Icon("images/portfolio_32x32.png")]
	public partial class PortfoliosPanel
	{
		public static readonly DependencyProperty ShowToolBarProperty = DependencyProperty.Register("ShowToolBar", typeof(bool), typeof(PortfoliosPanel), new PropertyMetadata(true));

		public bool ShowToolBar
		{
			get { return (bool)GetValue(ShowToolBarProperty); }
			set { SetValue(ShowToolBarProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="PortfoliosPanel"/>.
		/// </summary>
		public PortfoliosPanel()
		{
			InitializeComponent();

			AlertBtn.SchemaChanged += RaiseChangedCommand;

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<PositionCommand>(this, true, cmd => AlertBtn.Process(new PositionChangeMessage
			{
				SecurityId = cmd.Position.Security.ToSecurityId(),
				PortfolioName = cmd.Position.Portfolio.Name,
				ServerTime = TimeHelper.NowWithOffset,
			}.Add(PositionChangeTypes.CurrentValue, cmd.Position.CurrentValue)));
			cmdSvc.Register<BindConnectorCommand>(this, true, cmd =>
			{
				if (!cmd.CheckControl(this))
					return;

				ShowToolBar = false;
			});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("PositionsPanel", PositionsPanel.Save());
			storage.SetValue("AlertSettings", AlertBtn.Save());
		}

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			var panelSettings = storage.GetValue<SettingsStorage>("PositionsPanel");
			if (panelSettings != null)
				((IPersistable)PositionsPanel).Load(panelSettings);

			var alertSettings = storage.GetValue<SettingsStorage>("AlertSettings");
			if (alertSettings != null)
				AlertBtn.Load(alertSettings);
		}

		private void PositionTypeCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			new PositionEditCommand(PositionTypeCtrl.SelectedIndex == 0 ? (BasePosition)new Portfolio() : new Position()).Process(this);
		}

		public override void Dispose()
		{
			ConfigManager.GetService<IStudioCommandService>().UnRegister<PositionCommand>(this);
			PositionsPanel.Dispose();
			AlertBtn.Dispose();
		}
	}
}