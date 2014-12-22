namespace StockSharp.Studio.Controls
{
	using Ecng.Configuration;

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