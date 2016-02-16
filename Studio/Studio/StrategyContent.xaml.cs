#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: StrategyContent.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using ActiproSoftware.Windows.Controls.Docking;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Strategies;
	using StockSharp.Logging;
	using StockSharp.Studio.Controls;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Studio.Services;
	using StockSharp.Localization;
	using StockSharp.Messages;

	public partial class StrategyContent : IStudioControl, IStudioCommandScope
	{
		private sealed class ControlList : BaseList<IContentWindow>
		{
			private readonly Dictionary<FrameworkElement, bool> _controlsLoaded;
			private readonly Dictionary<string, IContentWindow> _contentWindows;

			private bool _initialized;

			public bool Initialized => _initialized;

			public event Action Loaded;
			public event Action SizeChanged;

			public ControlList()
			{
				_controlsLoaded = new Dictionary<FrameworkElement, bool>();
				_contentWindows = new Dictionary<string, IContentWindow>();
			}

			public IContentWindow TryGetWindow(string id)
			{
				return _contentWindows.TryGetValue(id);
			}

			protected override void OnAdded(IContentWindow item)
			{
				item.Control.DoIf<IStudioControl, FrameworkElement>(e =>
				{
					_controlsLoaded.Add(e, false);
					e.Loaded += ElementLoaded;
					e.SizeChanged += ElementSizeChanged;
				});

				_contentWindows.Add(item.Id, item);

				base.OnAdded(item);
			}

			protected override bool OnRemoving(IContentWindow item)
			{
				RemoveItem(item);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				foreach (var ctrl in this)
				{
					RemoveItem(ctrl);
				}

				return base.OnClearing();
			}

			private void RemoveItem(IContentWindow item)
			{
				item.Control.DoIf<IStudioControl, FrameworkElement>(e =>
				{
					_controlsLoaded.Remove(e);
					e.Loaded -= ElementLoaded;
					e.SizeChanged -= ElementSizeChanged;
				});

				_contentWindows.Remove(item.Id);
			}

			private void ElementLoaded(object sender, RoutedEventArgs args)
			{
				sender.DoIf<object, FrameworkElement>(e =>
				{
					_controlsLoaded[e] = true;
					e.Loaded -= ElementLoaded;
				});

				if (!_initialized && _controlsLoaded.Values.All(i => i))
				{
					_initialized = true;
					Loaded.SafeInvoke();
				}
			}

			private void ElementSizeChanged(object sender, SizeChangedEventArgs e)
			{
				SizeChanged.SafeInvoke();
			}
		}

		private sealed class StrategyCommandAdapter
		{
			private Strategy _strategy;
			private DateTimeOffset _lastPnlTime;

			public StrategyCommandAdapter(StrategyContainer strategy)
			{
				if (strategy == null)
					throw new ArgumentNullException(nameof(strategy));

				var strategyContainer = strategy;

				Subscribe(strategyContainer.Strategy);

				strategyContainer.StrategyAssigned += Subscribe;
				strategyContainer.StrategyRemoved += UnSubscribe;
			}

			private void Subscribe(Strategy strategy)
			{
				if (strategy == null)
					return;

				_strategy = strategy;

				_strategy.OrderRegistering += RaiseOrderRegisteringCommand;
				_strategy.OrderRegistered += RaiseOrderRegisteredCommand;
				_strategy.OrderChanged += RaiseOrderChangedCommand;
				_strategy.OrderReRegistering += RaiseOrderReRegisteringCommand;
				_strategy.OrderCanceling += RaiseOrderCancelingCommand;
				_strategy.OrderRegisterFailed += RaiseOrderRegisterFailedCommand;
				_strategy.OrderCancelFailed += RaiseOrderCancelFailedCommand;

				_strategy.StopOrderRegistering += RaiseOrderRegisteringCommand;
				_strategy.StopOrderRegistered += RaiseOrderRegisteredCommand;
				_strategy.StopOrderChanged += RaiseOrderChangedCommand;
				_strategy.StopOrderReRegistering += RaiseOrderReRegisteringCommand;
				_strategy.StopOrderCanceling += RaiseOrderCancelingCommand;
				_strategy.StopOrderRegisterFailed += RaiseOrderRegisterFailedCommand;
				_strategy.StopOrderCancelFailed += RaiseOrderCancelFailedCommand;

				_strategy.NewMyTrades += RaiseNewMyTradeCommand;

				_strategy.PositionManager.NewPosition += RaiseNewPositionCommand;
				_strategy.PositionManager.PositionChanged += RaisePositionChangedCommand;
				_strategy.PositionManager.Positions.ForEach(RaiseNewPositionCommand);

				_strategy.PnLChanged += RaisePnLChangedCommand;

				_strategy.Reseted += RaiseResetedCommand;
			}

			private void UnSubscribe(Strategy strategy)
			{
				//if (_strategyContainer != null)
				//	_strategyContainer.StrategyChanged -= StrategyChanged;

				if (_strategy == null)
					return;

				_strategy.OrderRegistering -= RaiseOrderRegisteringCommand;
				_strategy.OrderRegistered -= RaiseOrderRegisteredCommand;
				_strategy.OrderChanged -= RaiseOrderChangedCommand;
				_strategy.OrderReRegistering -= RaiseOrderReRegisteringCommand;
				_strategy.OrderCanceling -= RaiseOrderCancelingCommand;
				_strategy.OrderRegisterFailed -= RaiseOrderRegisterFailedCommand;
				_strategy.OrderCancelFailed -= RaiseOrderCancelFailedCommand;

				_strategy.StopOrderRegistering -= RaiseOrderRegisteringCommand;
				_strategy.StopOrderRegistered -= RaiseOrderRegisteredCommand;
				_strategy.StopOrderChanged -= RaiseOrderChangedCommand;
				_strategy.StopOrderReRegistering -= RaiseOrderReRegisteringCommand;
				_strategy.StopOrderCanceling -= RaiseOrderCancelingCommand;
				_strategy.StopOrderRegisterFailed -= RaiseOrderRegisterFailedCommand;
				_strategy.StopOrderCancelFailed -= RaiseOrderCancelFailedCommand;

				_strategy.NewMyTrades -= RaiseNewMyTradeCommand;

				_strategy.PositionManager.NewPosition -= RaiseNewPositionCommand;
				_strategy.PositionManager.PositionChanged -= RaisePositionChangedCommand;

				_strategy.PnLChanged -= RaisePnLChangedCommand;

				_strategy.Reseted -= RaiseResetedCommand;
			}

			public void UnSubscribe()
			{
				UnSubscribe(_strategy);
			}

			private void RaiseOrderCancelFailedCommand(OrderFail fail)
			{
				new OrderFailCommand(fail, OrderActions.Canceling).Process(_strategy);
			}

			private void RaiseOrderRegisterFailedCommand(OrderFail fail)
			{
				new OrderFailCommand(fail, OrderActions.Registering).Process(_strategy);
			}

			private void RaiseOrderCancelingCommand(Order order)
			{
				RaiseOrderCommand(order, OrderActions.Canceling);
			}

			private void RaiseOrderCommand(Order order, OrderActions action)
			{
				new OrderCommand(order, action).Process(_strategy);
			}

			private void RaiseOrderReRegisteringCommand(Order oldOrder, Order newOrder)
			{
				new ReRegisterOrderCommand(oldOrder, newOrder).Process(_strategy);
			}

			private void RaiseOrderRegisteringCommand(Order order)
			{
				RaiseOrderCommand(order, OrderActions.Registering);
			}

			private void RaiseOrderRegisteredCommand(Order order)
			{
				RaiseOrderCommand(order, OrderActions.Registered);
			}

			private void RaiseOrderChangedCommand(Order order)
			{
				RaiseOrderCommand(order, OrderActions.Changed);
			}

			private void RaiseNewMyTradeCommand(IEnumerable<MyTrade> trades)
			{
				new NewMyTradesCommand(trades).Process(_strategy);
			}

			private void RaiseNewPositionCommand(KeyValuePair<Tuple<SecurityId, string>, decimal> position)
			{
				// todo fix
				//new PositionCommand(_strategy.CurrentTime, position, true).Process(_strategy);
			}

			private void RaisePositionChangedCommand(KeyValuePair<Tuple<SecurityId, string>, decimal> position)
			{
				// todo fix
				// new PositionCommand(_strategy.CurrentTime, position, false).Process(_strategy);
			}

			private void RaisePnLChangedCommand()
			{
				var time = _strategy.CurrentTime;

				if (time < _lastPnlTime)
					return; // TODO нужен перевод стратегий на месседжи
					//throw new InvalidOperationException("Новое значение даты для PnL {0} меньше ранее добавленного {1}.".Put(time, _lastPnlTime));

				_lastPnlTime = time;

				new PnLChangedCommand(time, _strategy.PnL - (_strategy.Commission ?? 0), _strategy.PnLManager.UnrealizedPnL, _strategy.Commission).Process(_strategy);
			}

			private void RaiseResetedCommand()
			{
				_lastPnlTime = DateTimeOffset.MinValue;
				new ResetedCommand().Process(_strategy);
			}
		}

		#region DependencyProperty

		public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register("Strategy", typeof(StrategyContainer), typeof(StrategyContent),
			new FrameworkPropertyMetadata(null, OnStrategyPropertyChanged));

		public StrategyContainer Strategy
		{
			get { return (StrategyContainer)GetValue(StrategyProperty); }
			set { SetValue(StrategyProperty, value); }
		}

		private static void OnStrategyPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			((StrategyContent)source).OnStrategyChanged((StrategyContainer)e.OldValue, (StrategyContainer)e.NewValue);
		}

		#endregion

		private readonly LogManager _logManager = new LogManager();
		private readonly ControlList _controls;

		private volatile bool _suspendChangedEvent;

		private StrategyCommandAdapter _commandAdapter;
		private bool _needRaiseChangedOnLoaded;

		public EmulationService EmulationService { get; set; }

		public event Action ChildsLoaded;

		public StrategyContent()
		{
			InitializeComponent();

			_controls = new ControlList();
			_controls.Loaded += () =>
			{
				//чтобы команда привязки стратегии отработала корректно,
				//ее необходимо вызвать когда проинициализированы все Scope
				//для контролов, что будет только после их загрузки.
				if (Strategy != null)
				{
					RaiseBindStrategy();
					//RaiseSelectStrategy();
				}

				ChildsLoaded.SafeInvoke();

				if (!_needRaiseChangedOnLoaded)
					return;

				//первая загрузка шаблона происходит до открытия вкладки
				//поэтому после того как она будет открыта необходимо сохранить настройки
				_needRaiseChangedOnLoaded = false;
				RaiseChangedCommand();
			};
			_controls.SizeChanged += RaiseChangedCommand;

			DockSite.WindowClosing += (sender, args) =>
			{
				//окна могут открываться и закрываться в момент загрузки разметки
				if (!_suspendChangedEvent)
					((IStudioControl)args.Window.Content).Dispose();

				var contentWindow = args.Window as IContentWindow;
				if (contentWindow != null)
					_controls.Remove(contentWindow);

				RaiseChangedCommand();
			};
			DockSite.WindowOpened += (sender, args) =>
			{
				var contentWindow = args.Window as IContentWindow;
				if (contentWindow != null)
					_controls.Add(contentWindow);

				RaiseChangedCommand();
			};
			DockSite.WindowStateChanged += (sender, args) => RaiseChangedCommand();
			DockSite.WindowDragged += (sender, args) => RaiseChangedCommand();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ControlChangedCommand>(this, false, cmd => RaiseChangedCommand());
			cmdSvc.Register<OpenWindowCommand>(this, true, cmd => OpenControl(cmd.Id, cmd.CtrlType, null, ctrl => { }));
			cmdSvc.Register<OpenMarketDepthCommand>(this, true, cmd => OpenControl(cmd.Security.Id, typeof(ScalpingMarketDepthControl), cmd.Security, ctrl =>
			{
				((ScalpingMarketDepthControl)ctrl).Settings.Security = cmd.Security;
			}));
			cmdSvc.Register<AddLogListenerCommand>(this, false, cmd => _logManager.Listeners.Add(cmd.Listener));
			cmdSvc.Register<RemoveLogListenerCommand>(this, false, cmd => _logManager.Listeners.Remove(cmd.Listener));
			cmdSvc.Register<RequestBindSource>(this, true, cmd => RaiseBindStrategy(cmd.Control));
			cmdSvc.Register<LoadLayoutCommand>(this, true, cmd => LoadTemplate(cmd.Layout, true));
			cmdSvc.Register<SaveLayoutCommand>(this, true, cmd => cmd.Layout = this.Save().SaveSettingsStorage());

			cmdSvc.Register<StartStrategyCommand>(this, true, cmd =>
			{
				if (EmulationService != null)
				{
					var error = Strategy.CheckCanStart(false);
					if (error != null)
					{
						new MessageBoxBuilder()
							.Owner(this)
							.Caption(LocalizedStrings.Str3598)
							.Text(error)
							.Warning()
							.Show();

						return;
					}

					EmulationService.StartEmulation();
				}
				else
					new StartStrategyCommand(cmd.Strategy).Process(this);
			});
			cmdSvc.Register<StopStrategyCommand>(this, true, cmd =>
			{
				if (EmulationService != null)
					EmulationService.StopEmulation();
				else
					new StopStrategyCommand(cmd.Strategy).Process(this);
			});
		}

		private void OpenControl(string id, Type ctrlType, object tag, Action<IStudioControl> init)
		{
			var window = (ToolWindow)_controls.TryGetWindow(id);

			if (window == null)
			{
				var control = ctrlType.CreateInstance<IStudioControl>();

				init(control);

				window = new ContentToolWindow
				{
					Tag = tag,
					Control = control,
					Title = control.Title,
					Name = "ToolWindow" + id.Replace("-", "").Replace("@", "") + ctrlType.Name,
					Id = id
				};

				DockSite.ToolWindows.Add(window);
			}

			window.Activate();
		}

		public void LoadTemplate(string template, bool useControlSettings)
		{
			var newSettings = template.LoadSettingsStorage();

			if (useControlSettings)
			{
				var oldSettings = this.Save();

				var oldControlsSettings = oldSettings.GetValue<SettingsStorage[]>("Content");
				var newControlsSettings = newSettings.GetValue<SettingsStorage[]>("Content");

				foreach (var oldCtrlSetting in oldControlsSettings)
				{
					var type = oldCtrlSetting.GetValue<string>("Type").To<Type>();
					var settings = oldCtrlSetting.GetValue<SettingsStorage>("Settings");

					foreach (var newCtrlSettings in newControlsSettings)
					{
						if (newCtrlSettings.GetValue<string>("Type").To<Type>() == type)
						{
							newCtrlSettings.SetValue("Settings", settings);
						}
					}
				}
			}

			((IPersistable)this).Load(newSettings);

			if (_controls.Initialized)
				RaiseChangedCommand();
			else
				_needRaiseChangedOnLoaded = true;
		}

		private void OnStrategyChanged(StrategyContainer oldStrategy, StrategyContainer newStrategy)
		{
			if (oldStrategy != null)
			{
				_commandAdapter.UnSubscribe();
				_logManager.Sources.Remove(oldStrategy);
			}

			if (newStrategy == null)
				return;

			_commandAdapter = new StrategyCommandAdapter(newStrategy);
			_logManager.Sources.Add(newStrategy);

			RaiseBindStrategy();
			//RaiseSelectStrategy();
		}

		private void RemoveControls()
		{
			DockSite
				.ToolWindows
				.ToArray()
				.ForEach(w =>
				{
					w.Close();
					DockSite.ToolWindows.Remove(w);
				});
		}

		private void RaiseChangedCommand()
		{
			if (!_suspendChangedEvent)
				new ControlChangedCommand(this).Process(this);
		}

		#region IStudioControl

		void IDisposable.Dispose()
		{
			RemoveControls();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<ControlChangedCommand>(this);
			cmdSvc.UnRegister<OpenWindowCommand>(this);
			cmdSvc.UnRegister<OpenMarketDepthCommand>(this);
			cmdSvc.UnRegister<AddLogListenerCommand>(this);
			cmdSvc.UnRegister<RemoveLogListenerCommand>(this);
			cmdSvc.UnRegister<RequestBindSource>(this);
			cmdSvc.UnRegister<LoadLayoutCommand>(this);
			cmdSvc.UnRegister<SaveLayoutCommand>(this);
			cmdSvc.UnRegister<StartStrategyCommand>(this);
			cmdSvc.UnRegister<StopStrategyCommand>(this);
		}

		public string Title
		{
			get
			{
				if (Strategy == null)
					return LocalizedStrings.Str3599;

				switch (Strategy.SessionType)
				{
					case SessionType.Battle:
						return LocalizedStrings.Str3599;

					case SessionType.Emulation:
						return LocalizedStrings.Str3600;

					case SessionType.Optimization:
						return LocalizedStrings.Str3601;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		Uri IStudioControl.Icon => null;

		void IPersistable.Load(SettingsStorage settings)
		{
			_suspendChangedEvent = true;

			RemoveControls();

			settings.LoadUISettings(DockSite, new PairSet<Tuple<string, Type>, IContentWindow>());

			var emulationService = settings.GetValue<SettingsStorage>("EmulationService");

			if (emulationService != null && EmulationService != null)
				EmulationService.Load(emulationService);

			_suspendChangedEvent = false;
		}

		void IPersistable.Save(SettingsStorage settings)
		{
			var ps = new PairSet<Tuple<string, Type>, IContentWindow>();

			foreach (var control in _controls)
			{
				ps.Add(Tuple.Create(control.Id, control.Control.GetType()), control);
			}

			settings.SaveUISettings(DockSite, ps);

			if (EmulationService != null)
				settings.SetValue("EmulationService", EmulationService.Save());
		}

		#endregion

		private void RaiseBindStrategy(IStudioControl control = null)
		{
			new BindStrategyCommand(Strategy, control).SyncProcess(Strategy);
		}
	}
}