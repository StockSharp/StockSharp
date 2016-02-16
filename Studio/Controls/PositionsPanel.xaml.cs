#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: PositionsPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using Ecng.Collections;

namespace StockSharp.Studio.Controls
{
	using System.Windows.Input;

	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str972Key)]
	[DescriptionLoc(LocalizedStrings.Str3254Key)]
	[Icon("images/position_24x24.png")]
	public partial class PositionsPanel
	{
		public static RoutedCommand ClosePositionCommand = new RoutedCommand();
		public static RoutedCommand RevertPositionCommand = new RoutedCommand();

		public PositionsPanel()
		{
			InitializeComponent();

			PortfolioGrid.PropertyChanged += (s, e) => new ControlChangedCommand(this).Process(this);
			PortfolioGrid.SelectionChanged += (s, e) => new SelectCommand<Position>(SelectedBasePosition, CanEditPosition).Process(this);
			GotFocus += (s, e) => new SelectCommand<Position>(SelectedBasePosition, CanEditPosition).Process(this);

			PortfolioGrid.MouseDoubleClick += (sender, e) =>
			{
				new PositionEditCommand(SelectedBasePosition).Process(this);
				e.Handled = true;
			};

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<PortfolioCommand>(this, false, cmd =>
			{
				PortfolioGrid.Positions.RemoveWhere(p =>
				{
					var port = p as Portfolio;
					return port != null && port.Name == cmd.Portfolio.Name;
				});

				PortfolioGrid.Positions.Add(cmd.Portfolio);
			});
			cmdSvc.Register<PositionCommand>(this, false, cmd =>
			{
				PortfolioGrid.Positions.RemoveWhere(p =>
				{
					var pos = p as Position;
					return pos != null && pos.Security.Id == cmd.Position.Security.Id;
				});

				PortfolioGrid.Positions.Add(cmd.Position);
			});
			cmdSvc.Register<ResetedCommand>(this, false, cmd => PortfolioGrid.Positions.Clear());

			WhenLoaded(() =>
			{
				new RequestPortfoliosCommand().Process(this);
				new RequestPositionsCommand().Process(this);
			});
		}

		private bool CanEditPosition { get; set; }

		private BasePosition SelectedBasePosition => PortfolioGrid == null ? null : PortfolioGrid.SelectedPosition;

		private Position SelectedPosition => SelectedBasePosition as Position;

		public override void Save(SettingsStorage settings)
		{
			settings.SetValue("PortfolioGrid", PortfolioGrid.Save());
		}

		public override void Load(SettingsStorage settings)
		{
			PortfolioGrid.Load(settings.GetValue<SettingsStorage>("PortfolioGrid"));
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<PortfolioCommand>(this);
			cmdSvc.UnRegister<PositionCommand>(this);
		}

		private void ExecutedClosePositionCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new ClosePositionCommand(SelectedPosition).Process(this);
		}

		private void CanExecuteClosePositionCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedPosition != null && SelectedPosition.CurrentValue != 0;
		}

		private void ExecutedRevertPositionCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new RevertPositionCommand(SelectedPosition).Process(this);
		}

		private void CanExecuteRevertPositionCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedPosition != null && SelectedPosition.CurrentValue != 0;
		}
	}
}