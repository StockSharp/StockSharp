#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: PositionChartPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System.Collections.Generic;
	using System.Windows.Media;

	using Ecng.Configuration;
	using Ecng.ComponentModel;

	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3244Key)]
	[DescriptionLoc(LocalizedStrings.Str3245Key)]
	[Icon("images/position_24x24.png")]
	public partial class PositionChartPanel
	{
		private readonly ICollection<EquityData> _positionCurve;

		public PositionChartPanel()
		{
			InitializeComponent();

			_positionCurve = EquityChart.CreateCurve(LocalizedStrings.Str862, Colors.SteelBlue);

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ResetedCommand>(this, false, cmd => _positionCurve.Clear());
			cmdSvc.Register<PositionCommand>(this, false, cmd =>
				_positionCurve.Add(new EquityData { Time = cmd.Time, Value = cmd.Position.CurrentValue }));
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<ResetedCommand>(this);
			cmdSvc.UnRegister<PositionCommand>(this);
		}
	}
}