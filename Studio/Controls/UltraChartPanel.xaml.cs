#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: UltraChartPanel.xaml.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using Ecng.Xaml.Charting.Visuals;

	using Ecng.Configuration;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3200Key)]
	public partial class UltraChartPanel
	{
		private StrategyContainer _strategy;

		public UltraChartPanel()
		{
			InitializeComponent();

			ConfigManager
				.GetService<IStudioCommandService>()
				.Register<BindStrategyCommand>(this, true, cmd =>
				{
					if (!cmd.CheckControl(this))
						return;

					if (_strategy == cmd.Source)
						return;

					_strategy = cmd.Source;
					SetSurface(UltrachartSurface);
				});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		public override void Dispose()
		{
			if (_strategy != null)
				SetSurface(null);

			ConfigManager
				.GetService<IStudioCommandService>()
				.UnRegister<BindStrategyCommand>(this);
		}

		private void SetSurface(UltrachartSurface surface)
		{
			_strategy.Environment.SetValue("Chart", surface);
		}
	}
}
