#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: AnalyticsTab.xaml.cs
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

	using StockSharp.Algo;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;

	using RibbonButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.Button;
	using RibbonMenu = ActiproSoftware.Windows.Controls.Ribbon.Controls.Menu;
	using RibbonPopupButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.PopupButton;

	public partial class AnalyticsTab
	{
		public static readonly RoutedCommand AddAnalyticsInfoCommand = new RoutedCommand();
		public static readonly RoutedCommand OpenAnalyticsInfoCommand = new RoutedCommand();
		public static readonly RoutedCommand RemoveAnalyticsInfoCommand = new RoutedCommand();
		public static readonly RoutedCommand AddAnalyticsCommand = new RoutedCommand();
		public static readonly RoutedCommand OpenAnalyticsCommand = new RoutedCommand();
		public static readonly RoutedCommand RemoveAnalyticsCommand = new RoutedCommand();

		public static readonly RoutedCommand StartAnalyticsCommand = new RoutedCommand();
		public static readonly RoutedCommand StopAnalyticsCommand = new RoutedCommand();

		public static readonly DependencyProperty SelectedStrategyProperty = DependencyProperty.Register("SelectedStrategy", typeof(StrategyContainer), typeof(AnalyticsTab));

		public StrategyContainer SelectedStrategy
		{
			get { return (StrategyContainer)GetValue(SelectedStrategyProperty); }
			set { SetValue(SelectedStrategyProperty, value); }
		}

		public static readonly DependencyProperty SelectedStrategyInfoProperty = DependencyProperty.Register("SelectedStrategyInfo", typeof(StrategyInfo), typeof(AnalyticsTab),
			new PropertyMetadata(StrategyInfoChanged));

		private static void StrategyInfoChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (AnalyticsTab)sender;

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

		public AnalyticsTab()
		{
			InitializeComponent();

			_holder = new StrategyInfoHolder();
			_holder.StrategyInfosUpdated += () => GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				var registry = ConfigManager.TryGetService<IStudioEntityRegistry>();
				OpenAnalyticsInfoBtn.IsEnabled = registry != null && registry.Strategies.Any(s => s.IsAnalytics());
			});
			_holder.StrategiesUpdated += () => GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				OpenAnalyticsBtn.IsEnabled = SelectedStrategyInfo.IsAnalytics() && SelectedStrategyInfo.Strategies.Any();
			});

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;

			_holder.Set(ConfigManager.GetService<IStudioEntityRegistry>());
		}

		private void ExecutedAddAnalyticsInfo(object sender, ExecutedRoutedEventArgs e)
		{
			new AddStrategyInfoCommand(StrategyInfoTypes.Analytics).SyncProcess(this);
		}

		private void CanExecuteAddAnalyticsInfo(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedRemoveAnalyticsInfo(object sender, ExecutedRoutedEventArgs e)
		{
			new RemoveStrategyInfoCommand(SelectedStrategyInfo).SyncProcess(this);
		}

		private void CanExecuteRemoveAnalyticsInfo(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo.IsAnalytics();
		}

		private void ExecutedOpenAnalyticsInfoCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenStrategyInfoCommand((StrategyInfo)e.Parameter).SyncProcess(this);
		}

		private void CanExecuteOpenAnalyticsInfoCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedAddAnalytics(object sender, ExecutedRoutedEventArgs e)
		{
			if (SelectedStrategy != null)
				new CloneStrategyCommand(SelectedStrategy).SyncProcess(this);
			else
				new AddStrategyCommand(SelectedStrategyInfo, SessionType.Battle).SyncProcess(this);
		}

		private void CanExecuteAddAnalytics(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo.IsAnalytics();
		}

		private void ExecutedRemoveAnalytics(object sender, ExecutedRoutedEventArgs e)
		{
			new RemoveStrategyCommand(SelectedStrategy).SyncProcess(this);
		}

		private void CanExecuteRemoveAnalytics(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy.IsAnalytics() && SelectedStrategy.ProcessState == ProcessStates.Stopped;
		}

		private void ExecutedStartAnalyticsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedStrategy.Environment.SetValue("Drive", new StudioStorageRegistry { MarketDataSettings = SelectedStrategy.MarketDataSettings }.DefaultDrive);
			SelectedStrategy.Start();
		}

		private void CanExecuteStartAnalyticsCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy.IsAnalytics() && SelectedStrategy.ProcessState == ProcessStates.Stopped;
		}

		private void ExecutedStopAnalyticsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedStrategy.Stop();
		}

		private void CanExecuteStopAnalyticsCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy.IsAnalytics() && SelectedStrategy.ProcessState == ProcessStates.Started;
		}

		private void OpenAnalyticsInfo_OnPopupOpening(object sender, CancelRoutedEventArgs e)
		{
			var btn = sender as RibbonPopupButton;

			if (btn == null)
				return;

			var menu = btn.PopupContent as RibbonMenu;

			if (menu == null)
				return;

			menu.ItemsSource = ConfigManager
				.GetService<IStudioEntityRegistry>()
				.Strategies
				.ReadByType(StrategyInfoTypes.Analytics)
				.ToArray();
		}

		private void OpenAnalytics_OnPopupOpening(object sender, CancelRoutedEventArgs e)
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

		private void ExecutedOpenAnalyticsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenStrategyCommand((StrategyContainer)e.Parameter).SyncProcess(SelectedStrategyInfo);
		}

		private void CanExecuteOpenAnalyticsCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo != null;
		}
	}
}
