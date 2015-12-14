#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: StatisticsPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using Ecng.Configuration;
	using Ecng.ComponentModel;

	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str436Key)]
	[DescriptionLoc(LocalizedStrings.Str3259Key)]
	[Icon("images/statistics_32x32.png")]
	public partial class StatisticsPanel
	{
		public StatisticsPanel()
		{
			InitializeComponent();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<BindStrategyCommand>(this, true, cmd =>
			{
				if (!cmd.CheckControl(this))
					return;

				StatisticsGrid.StatisticManager = cmd.Source.StatisticManager;
			});
			cmdSvc.Register<ResetedCommand>(this, true, cmd => StatisticsGrid.Reset());

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<BindStrategyCommand>(this);
			cmdSvc.UnRegister<ResetedCommand>(this);
		}
	}
}