#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: TerminalTab.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using ActiproSoftware.Windows;

	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;

	using RibbonButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.Button;
	using RibbonMenu = ActiproSoftware.Windows.Controls.Ribbon.Controls.Menu;
	using RibbonPopupButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.PopupButton;

	public partial class TerminalTab
	{
		public static readonly RoutedCommand OpenStrategyInfoCommand = new RoutedCommand();

		public static readonly RoutedCommand AddStrategyCommand = new RoutedCommand();
		public static readonly RoutedCommand OpenStrategyCommand = new RoutedCommand();
		public static readonly RoutedCommand RemoveStrategyCommand = new RoutedCommand();

		public static readonly DependencyProperty SelectedStrategyProperty = DependencyProperty.Register("SelectedStrategy", typeof(StrategyContainer), typeof(TerminalTab));

		public StrategyContainer SelectedStrategy
		{
			get { return (StrategyContainer)GetValue(SelectedStrategyProperty); }
			set { SetValue(SelectedStrategyProperty, value); }
		}

		public static readonly DependencyProperty SelectedStrategyInfoProperty = DependencyProperty.Register("SelectedStrategyInfo", typeof(StrategyInfo), typeof(TerminalTab),
			new PropertyMetadata(StrategyInfoChanged));

		private static void StrategyInfoChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (TerminalTab)sender;

			var oldValue = (StrategyInfo)args.OldValue;
			var newValue = (StrategyInfo)args.OldValue;

			ctrl._holder.Set(oldValue, newValue);
		}

		public StrategyInfo SelectedStrategyInfo
		{
			get { return (StrategyInfo)GetValue(SelectedStrategyInfoProperty); }
			set { SetValue(SelectedStrategyInfoProperty, value); }
		}

		private readonly StrategyInfoHolder _holder;

		public TerminalTab()
		{
			InitializeComponent();

			_holder = new StrategyInfoHolder();
			_holder.StrategiesUpdated += () => GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				OpenStrategyBtn.IsEnabled = SelectedStrategyInfo.IsTerminal() && SelectedStrategyInfo.Strategies.Any();
			});
		}

		#region Add/remove strategy commands

		private void ExecutedAddStrategy(object sender, ExecutedRoutedEventArgs e)
		{
			if (SelectedStrategy != null)
				new CloneStrategyCommand(SelectedStrategy).SyncProcess(this);
			else
				new AddStrategyCommand(SelectedStrategyInfo, SessionType.Battle).SyncProcess(this);
		}

		private void CanExecuteAddStrategy(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo.IsTerminal();
		}

		private void ExecutedRemoveStrategy(object sender, ExecutedRoutedEventArgs e)
		{
			new StopStrategyCommand(SelectedStrategy).SyncProcess(SelectedStrategy);
			new RemoveStrategyCommand(SelectedStrategy).SyncProcess(this);
		}

		private void CanExecuteRemoveStrategy(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy.IsTerminal();
		}

		private void ExecutedOpenStrategy(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenStrategyCommand((StrategyContainer)e.Parameter).SyncProcess(SelectedStrategyInfo);
		}

		private void CanExecuteOpenStrategy(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo != null;
		}

		#endregion

		private void ExecutedOpenStrategyInfoCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var info = ConfigManager
				.GetService<IStudioEntityRegistry>()
				.Strategies
				.ReadByType(StrategyInfoTypes.Terminal)
				.FirstOrDefault();

			if (info == null)
				return;

			new OpenStrategyInfoCommand(info).SyncProcess(this);
		}

		private void Strategy_OnPopupOpening(object sender, CancelRoutedEventArgs e)
		{
			var btn = sender as RibbonPopupButton;

			if (btn == null)
				return;

			var menu = btn.PopupContent as RibbonMenu;

			if (menu == null)
				return;

			menu.ItemsSource = SelectedStrategyInfo
				.Strategies
				.ToArray();
		}
	}
}
