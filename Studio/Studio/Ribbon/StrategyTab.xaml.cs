namespace StockSharp.Studio.Ribbon
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using ActiproSoftware.Windows;

	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Studio.Services;

	using RibbonButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.Button;
	using RibbonMenu = ActiproSoftware.Windows.Controls.Ribbon.Controls.Menu;
	using RibbonPopupButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.PopupButton;

	public partial class StrategyTab
	{
		public readonly static RoutedCommand AddStrategyInfoCommand = new RoutedCommand();
		public readonly static RoutedCommand OpenStrategyInfoCommand = new RoutedCommand();
		public readonly static RoutedCommand RemoveStrategyInfoCommand = new RoutedCommand();
		public readonly static RoutedCommand AddStrategyCommand = new RoutedCommand();
		public readonly static RoutedCommand OpenStrategyCommand = new RoutedCommand();
		//public readonly static RoutedCommand CopyStrategyCommand = new RoutedCommand();
		public readonly static RoutedCommand RemoveStrategyCommand = new RoutedCommand();

		public static readonly RoutedCommand StartStrategyCommand = new RoutedCommand();
		public static readonly RoutedCommand StopStrategyCommand = new RoutedCommand();

		public readonly static RoutedCommand AddEmulationCommand = new RoutedCommand();
		public readonly static RoutedCommand AddOptimizationCommand = new RoutedCommand();
		public readonly static RoutedCommand RemoveEmulationCommand = new RoutedCommand();

		public static readonly DependencyProperty SelectedStrategyProperty = DependencyProperty.Register("SelectedStrategy", typeof(StrategyContainer), typeof(StrategyTab),
			new PropertyMetadata(StrategyChanged));

		private static void StrategyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var oldValue = (StrategyContainer)args.OldValue;
			if (oldValue != null)
				oldValue.ProcessStateChanged -= OnStrategyProcessStateChanged;

			var newValue = (StrategyContainer)args.NewValue;
			if (newValue != null)
				newValue.ProcessStateChanged += OnStrategyProcessStateChanged;
		}

		static void OnStrategyProcessStateChanged(Strategy strategy)
		{
			// при изменении состояния стратегии не всегда обновляется состояние кнопок.
			GuiDispatcher.GlobalDispatcher.AddAction(CommandManager.InvalidateRequerySuggested);
		}

		public StrategyContainer SelectedStrategy
		{
			get { return (StrategyContainer)GetValue(SelectedStrategyProperty); }
			set { SetValue(SelectedStrategyProperty, value); }
		}

		public static readonly DependencyProperty SelectedStrategiesProperty = DependencyProperty.Register("SelectedStrategies", typeof(IEnumerable<StrategyContainer>), typeof(StrategyTab));

		public IEnumerable<StrategyContainer> SelectedStrategies
		{
			get { return (IEnumerable<StrategyContainer>)GetValue(SelectedStrategiesProperty); }
			set { SetValue(SelectedStrategiesProperty, value); }
		}

		public static readonly DependencyProperty SelectedStrategyInfoProperty = DependencyProperty.Register("SelectedStrategyInfo", typeof(StrategyInfo), typeof(StrategyTab),
			new PropertyMetadata(StrategyInfoChanged));

		private static void StrategyInfoChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (StrategyTab)sender;

			var oldValue = (StrategyInfo)args.OldValue;
			var newValue = (StrategyInfo)args.OldValue;

			ctrl._holder.Set(oldValue, newValue);
		}

		public StrategyInfo SelectedStrategyInfo
		{
			get { return (StrategyInfo)GetValue(SelectedStrategyInfoProperty); }
			set { SetValue(SelectedStrategyInfoProperty, value); }
		}

		public static readonly DependencyProperty SelectedEmulationServiceProperty = DependencyProperty.Register("SelectedEmulationService", typeof(EmulationService), typeof(StrategyTab));

		public EmulationService SelectedEmulationService
		{
			get { return (EmulationService)GetValue(SelectedEmulationServiceProperty); }
			set { SetValue(SelectedEmulationServiceProperty, value); }
		}

		private readonly StrategyInfoHolder _holder;

		public StrategyTab()
		{
			InitializeComponent();

			_holder = new StrategyInfoHolder();
			_holder.StrategyInfosUpdated += () => GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				var registry = ConfigManager.TryGetService<IStudioEntityRegistry>();
				OpenStrategyInfoBtn.IsEnabled = registry != null && registry.Strategies.Any(s => s.IsStrategy());
			});
			_holder.StrategiesUpdated += () => GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				OpenStrategyBtn.IsEnabled = SelectedStrategyInfo.IsStrategy() && SelectedStrategyInfo.Strategies.Any();
			});

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;

			_holder.Set(ConfigManager.GetService<IStudioEntityRegistry>());
		}

		#region Add/remove strategy info commands

		private void ExecutedAddStrategyInfo(object sender, ExecutedRoutedEventArgs e)
		{
			new AddStrategyInfoCommand(StrategyInfoTypes.Diagram, StrategyInfoTypes.SourceCode, StrategyInfoTypes.Assembly).SyncProcess(this);
		}

		private void CanExecuteAddStrategyInfo(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedOpenStrategyInfoCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenStrategyInfoCommand((StrategyInfo)e.Parameter).SyncProcess(this);
		}

		private void CanExecuteOpenStrategyInfoCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedRemoveStrategyInfo(object sender, ExecutedRoutedEventArgs e)
		{
			new RemoveStrategyInfoCommand(SelectedStrategyInfo).SyncProcess(this);
		}

		private void CanExecuteRemoveStrategyInfo(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo.IsStrategy();
		}

		#endregion

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
			e.CanExecute = SelectedStrategyInfo.IsStrategy();
		}

		private void ExecutedOpenStrategyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenStrategyCommand((StrategyContainer)e.Parameter).SyncProcess(SelectedStrategyInfo);
		}

		private void CanExecuteOpenStrategyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo != null;
		}

		private void ExecutedRemoveStrategy(object sender, ExecutedRoutedEventArgs e)
		{
			new RemoveStrategyCommand(SelectedStrategy).SyncProcess(this);
		}

		private void CanExecuteRemoveStrategy(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy.IsStrategy() && SelectedStrategy.SessionType == SessionType.Battle && SelectedStrategy.ProcessState == ProcessStates.Stopped;
		}

		//private void ExecutedCopyStrategy(object sender, ExecutedRoutedEventArgs e)
		//{
		//	new CloneStrategyCommand(SelectedStrategy).SyncProcess(this);
		//}

		//private void CanExecuteCopyStrategy(object sender, CanExecuteRoutedEventArgs e)
		//{
		//	e.CanExecute = SelectedStrategy.IsStrategy() && SelectedStrategy.SessionType == SessionType.Battle;
		//}

		#endregion

		#region Strategy content commands

		private void ExecutedStartStrategyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new ResetStrategyCommand(SelectedStrategy).Process(this);
			new StartStrategyCommand(SelectedStrategy).Process(SelectedStrategy);
		}

		private void CanExecuteStartStrategyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			if (!SelectedStrategy.IsStrategy())
			{
				e.CanExecute = false;
				return;
			}

			e.CanExecute = SelectedStrategy.SessionType != SessionType.Optimization 
				? SelectedStrategy.ProcessState == ProcessStates.Stopped 
				: new StartStrategyCommand(SelectedStrategy).CanProcess(SelectedStrategy);
		}

		private void ExecutedStopStrategyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new StopStrategyCommand(SelectedStrategy).Process(SelectedStrategy);
		}

		private void CanExecuteStopStrategyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			if (!SelectedStrategy.IsStrategy())
			{
				e.CanExecute = false;
				return;
			}

			e.CanExecute = SelectedStrategy.SessionType != SessionType.Optimization
				? SelectedStrategy.ProcessState == ProcessStates.Started
				: new StopStrategyCommand(SelectedStrategy).CanProcess(SelectedStrategy);
		}

		#endregion

		#region Add/remove emulation

		private void ExecutedAddEmulation(object sender, ExecutedRoutedEventArgs e)
		{
			new AddStrategyCommand(SelectedStrategyInfo, SessionType.Emulation).SyncProcess(this);
		}

		private void CanExecuteAddEmulation(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo.IsStrategy() && !SelectedStrategyInfo.GetIsNoEmulation();
		}

		private void ExecutedAddOptimization(object sender, ExecutedRoutedEventArgs e)
		{
			new AddStrategyCommand(SelectedStrategyInfo, SessionType.Optimization).SyncProcess(this);
		}

		private void CanExecuteAddOptimization(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo.IsStrategy();
		}

		private void ExecutedRemoveEmulation(object sender, ExecutedRoutedEventArgs e)
		{
			new RemoveStrategyCommand(SelectedStrategy).SyncProcess(this);
		}

		private void CanExecuteRemoveEmulation(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy.IsStrategy() && SelectedStrategy.SessionType != SessionType.Battle;
		}

		#endregion

		private void StrategyInfo_OnPopupOpening(object sender, CancelRoutedEventArgs e)
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
				.Where(s => s.IsStrategy())
				.ToList();
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
				.ToList();
		}
	}
}
