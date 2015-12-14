#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: UniversalGridPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using Ecng.Configuration;
	using Ecng.Xaml.Grids;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3280Key)]
	public partial class UniversalGridPanel
	{
		private StrategyContainer _strategy;

		public UniversalGridPanel()
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
					SetGrid(Grid);
				});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		public override void Dispose()
		{
			if (_strategy != null)
				SetGrid(null);

			ConfigManager
				.GetService<IStudioCommandService>()
				.UnRegister<BindStrategyCommand>(this);
		}

		private void SetGrid(UniversalGrid grid)
		{
			_strategy.Environment.SetValue("Grid", grid);
		}
	}
}
