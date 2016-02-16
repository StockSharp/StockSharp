#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Linq;

	using ActiproSoftware.Windows.Controls.Docking;

	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Collections;
	using Ecng.Interop;
	using Ecng.Xaml;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.History.Hydra;
	using StockSharp.Licensing;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Community;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Studio.Controls;
	using StockSharp.Studio.Controls.Commands;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Studio.Database;
	using StockSharp.Studio.Ribbon;
	using StockSharp.Studio.Services;
	using StockSharp.Alerts;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Diagram;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Studio.Core.Services;
	using StockSharp.Xaml.Code;

	using IStrategyService = StockSharp.Studio.Core.Services.IStrategyService;
	using RibbonButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.Button;
	using RibbonPopupButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.PopupButton;
	using RibbonSeparator = ActiproSoftware.Windows.Controls.Ribbon.Controls.Separator;

	public partial class MainWindow
	{
		public static readonly RoutedCommand StockSharpConnectCommand = new RoutedCommand();
		public static readonly RoutedCommand ConnectCommand = new RoutedCommand();
		public static readonly RoutedCommand ConnectionSettingsCommand = new RoutedCommand();
		public static readonly RoutedCommand PortfolioSettingsCommand = new RoutedCommand();
		public static readonly RoutedCommand ExitCommand = new RoutedCommand();
		public static readonly RoutedCommand ConnectionsWindowCommand = new RoutedCommand();
		public static readonly RoutedCommand CheckForUpdatesCommand = new RoutedCommand();
		public static readonly RoutedCommand AboutCommand = new RoutedCommand();
		
		public static readonly RoutedCommand DataDirectoryCommand = new RoutedCommand();
		public static readonly RoutedCommand TargetPlatformCommand = new RoutedCommand();

		public static readonly RoutedCommand NewPortfolioCommand = new RoutedCommand();
		public static readonly RoutedCommand NewSecurityCommand = new RoutedCommand();
		public static readonly RoutedCommand LookupSecurityCommand = new RoutedCommand();

		public static readonly RoutedCommand DocumentationCommand = new RoutedCommand();
		public static readonly RoutedCommand EduCommand = new RoutedCommand();
		public static readonly RoutedCommand ForumCommand = new RoutedCommand();
		public static readonly RoutedCommand ChatCommand = new RoutedCommand();

		#region DependencyProperty

		public static readonly DependencyProperty SelectedStrategyProperty = DependencyProperty.Register("SelectedStrategy", typeof(StrategyContainer), typeof(MainWindow));

		public StrategyContainer SelectedStrategy
		{
			get { return (StrategyContainer)GetValue(SelectedStrategyProperty); }
			set { SetValue(SelectedStrategyProperty, value); }
		}

		public static readonly DependencyProperty SelectedStrategiesProperty = DependencyProperty.Register("SelectedStrategies", typeof(IEnumerable<StrategyContainer>), typeof(MainWindow));

		public IEnumerable<StrategyContainer> SelectedStrategies
		{
			get { return (IEnumerable<StrategyContainer>)GetValue(SelectedStrategiesProperty); }
			set { SetValue(SelectedStrategiesProperty, value); }
		}

		public static readonly DependencyProperty SelectedStrategyInfoProperty = DependencyProperty.Register("SelectedStrategyInfo", typeof(StrategyInfo), typeof(MainWindow));

		public StrategyInfo SelectedStrategyInfo
		{
			get { return (StrategyInfo)GetValue(SelectedStrategyInfoProperty); }
			set { SetValue(SelectedStrategyInfoProperty, value); }
		}

		public static readonly DependencyProperty SelectedEmulationServiceProperty = DependencyProperty.Register("SelectedEmulationService", typeof(EmulationService), typeof(MainWindow));

		public EmulationService SelectedEmulationService
		{
			get { return (EmulationService)GetValue(SelectedEmulationServiceProperty); }
			set { SetValue(SelectedEmulationServiceProperty, value); }
		}

		#endregion

		private StudioConnector _connector;
		private StudioEntityRegistry _entityRegistry;
		private bool _showConnectionErrors;
		private bool _isInitialized;
		private readonly IPersistableService _persistableService;

		private readonly PairSet<Tuple<string, Type>, IContentWindow> _contents = new PairSet<Tuple<string, Type>, IContentWindow>();

		public string ProductTitle => TypeHelper.ApplicationNameWithVersion.Replace("S#.Studio", "StockSharp");

		public MainWindow()
		{
			UserConfig.Instance.SuspendChangesMonitor();
			UserConfig.Instance.SaveLayout = () => GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
			{
				var settings = new SettingsStorage();
				settings.SaveUISettings(DockSite, _contents);
				return settings;
			});

			_persistableService = UserConfig.Instance;

			ConfigManager.RegisterService<IStudioCommandService>(new StudioCommandService());

			InitializeComponent();

			if (AutomaticUpdater.ClosingForInstall)
			{
				Application.Current.Shutdown();
				return;
			}
			
			ConfigManager.RegisterService<Window>(this);
		}

		private void RegisterCommandHandlers()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			var notConnectedBrush = StockSharpConnectBtn.Background;

			#region StrategyInfo commands

			cmdSvc.Register<AddStrategyInfoCommand>(this, false, cmd =>
			{
				var references = _persistableService.GetReferences();

				GuiDispatcher.GlobalDispatcher.AddAction(() =>
				{
					var info = cmd.Info;

					if (info == null)
					{
						var wnd = new NewStrategyWindow(cmd.Types);

						if (!wnd.ShowModal(Application.Current.GetActiveOrMainWindow()))
							return;

						info = wnd.SelectedInfo;
					}

					_entityRegistry.Strategies.Add(info);
					_entityRegistry.Strategies.DelayAction.WaitFlush();

					switch (info.Type)
					{
						case StrategyInfoTypes.SourceCode:
						case StrategyInfoTypes.Analytics:
							{
								new CompileStrategyInfoCommand(info, references).SyncProcess(this);
								break;
							}
					}

					info.InitStrategyType();

					new AddStrategyCommand(info, SessionType.Battle).Process(this);

					if (!info.GetIsNoEmulation())
						new AddStrategyCommand(info, SessionType.Emulation).Process(this);
				});
			});

			cmdSvc.Register<CompileStrategyInfoCommand>(this, true, cmd =>
			{
				var res = cmd.Info.CompileStrategy(cmd.References);

				ConfigManager.GetService<IStudioEntityRegistry>().Strategies.Save(cmd.Info);

				if (res.HasErrors())
				{
					//сначала открываем вкладку типа стратегии, а далее код/дизайнер внутри нее
					new OpenStrategyInfoCommand(cmd.Info).SyncProcess(this);
					new OpenStrategyInfoCommand(cmd.Info).SyncProcess(cmd.Info.GetKey());
				}

				new CompileStrategyInfoResultCommand(res).Process(cmd.Info.GetKey());
			});

			cmdSvc.Register<RemoveStrategyInfoCommand>(this, true, cmd =>
			{
				var info = cmd.Info;

				if (!CheckState(info))
					return;

				info.Strategies.ToArray().ForEach(RemoveStrategy);

				_entityRegistry.Strategies.Remove(info);
			});

			#endregion

			#region Strategy commands

			cmdSvc.Register<AddStrategyCommand>(this, true, cmd =>
			{
				var info = cmd.Info;

				if (info.StrategyType == null)
				{
					new MessageBoxBuilder()
						.Owner(Application.Current.GetActiveOrMainWindow())
						.Text(LocalizedStrings.Str3642Params.Put(info.Name))
						.Error()
						.Show();

					return;
				}

				var strategy = cmd.Strategy ?? info.CreateStrategy(cmd.SessionType);

				strategy.UpdateName();

				info.Strategies.Add(strategy);

				new SelectCommand<Strategy>(strategy, true).Process(this);
			});

			cmdSvc.Register<RemoveStrategyCommand>(this, true, cmd => RemoveStrategy(cmd.Strategy));

			cmdSvc.Register<CloneStrategyCommand>(this, true, cmd =>
			{
				var clone = (StrategyContainer)cmd.Strategy.Clone();
				clone.UpdateName();
				cmd.Strategy.StrategyInfo.Strategies.Add(clone);
			});

			#endregion

			#region Documents commands

			cmdSvc.Register<OpenStrategyInfoCommand>(this, true, cmd => OpenWindow(cmd.Info));
			cmdSvc.Register<OpenCompositionCommand>(this, true, cmd => OpenWindow(cmd.Composition));
			cmdSvc.Register<OpenMarketDataSettingsCommand>(this, true, cmd => OpenWindow(cmd.Settings));
			cmdSvc.Register<OpenWindowCommand>(this, true, cmd => OpenWindow(cmd.Id, cmd.CtrlType, cmd.IsToolWindow, cmd.Context, () => cmd.CtrlType.CreateInstance<IStudioControl>()));
			cmdSvc.Register<OpenIndexSecurityPanelCommand>(this, true, cmd => OpenWindow<IndexSecurityPanel>(cmd.Security));
			cmdSvc.Register<OpenContinuousSecurityPanelCommand>(this, true, cmd => OpenWindow<ContinuousSecurityPanel>(cmd.Security));

			cmdSvc.Register<CloseWindowCommand>(this, true, cmd => CloseWindow(cmd.Id, cmd.CtrlType));
			cmdSvc.Register<ControlOpenedCommand>(this, true, cmd => WindowOpened(cmd.Control, cmd.IsMainWindow));

			#endregion

			#region StockSharp commands

			cmdSvc.Register<RenewLicenseCommand>(this, true, cmd =>
			{
				if (!ConfigManager.GetService<AuthenticationClient>().IsLoggedIn)
				{
					var res = new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3643)
						.Warning()
						.YesNo()
						.Show();

					if (res != MessageBoxResult.Yes)
						return;

					new LogInCommand().Process(this, true);

					if (!ConfigManager.GetService<AuthenticationClient>().IsLoggedIn)
						return;
				}

				try
				{
					using (var client = new LicenseClient())
					{
						(cmd.License == null ? client.GetFullLicense() : client.RenewLicense(cmd.License)).Save();
					}
				}
				catch (Exception excp)
				{
					excp.LogError();

					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str3644 + excp.Message)
						.Error()
						.Owner(this)
						.Show();
				}
			});

			cmdSvc.Register<LicenseChangedCommand>(this, true, cmd => UpdateLicenseToolbar());
			cmdSvc.Register<RemoveLicenseCommand>(this, false, cmd => cmd.License.Remove());
			cmdSvc.Register<RequestLicenseCommand>(this, true, cmd =>
			{
				try
				{
					using (var client = new LicenseClient())
					{
						client.RequestLicense(cmd.BrokerId, cmd.Account);
					}

					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str3645)
						.Info()
						.Owner(this)
						.Show();
				}
				catch (Exception excp)
				{
					excp.LogError();

					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str3646 + excp.Message)
						.Error()
						.Owner(this)
						.Show();
				}
			});

			cmdSvc.Register<LoggedInCommand>(this, true, cmd =>
			{
				StockSharpConnectBtn.Background = Brushes.Salmon;
				StockSharpConnectBtn.Label = LocalizedStrings.Disconnect;

				UpdateLicenseToolbar();

				if (LicenseHelper.Licenses.IsExpired())
					new RenewLicenseCommand().Process(this);

				new RefreshSecurities(MarketDataSettings.StockSharpSettings,
					new[] { SecurityTypes.Stock, SecurityTypes.Future, SecurityTypes.Currency, SecurityTypes.Index },
					() => false,
					p => { },
					c => ConfigManager.GetService<LogManager>().Application.AddInfoLog(LocalizedStrings.Str3264Params, c)).Process(this);

				if (_isInitialized)
					return;

				_isInitialized = true;
				InitializeControls();
			});

			cmdSvc.Register<LoggedOutCommand>(this, true, cmd =>
			{
				StockSharpConnectBtn.Background = notConnectedBrush;
				StockSharpConnectBtn.Label = LocalizedStrings.Connect;
			});

			cmdSvc.Register<LogInCommand>(this, true, cmd =>
			{
				var credentials = _persistableService.GetCredentials();
				var client = ConfigManager.GetService<AuthenticationClient>();

				if (client.IsLoggedIn)
				{
					client.Logout();
					new LoggedOutCommand().Process(this);
					new LogInCommand(false).Process(this);

					return;
				}

				if (credentials == null || !credentials.AutoLogon || !cmd.CanAutoLogon)
				{
					var wnd = new CredentialsWindow { IsLoggedIn = false, AutoLogon = true };

					if (credentials != null)
					{
						wnd.Login = credentials.Login;
						wnd.AutoLogon = credentials.AutoLogon;
					}

					if (!wnd.ShowModal(Application.Current.GetActiveOrMainWindow()))
					{
						ExitCommand.Execute(null, this);
						//new LogInCommand(false).Process(this);
						return;
					}

					credentials = new ServerCredentials
					{
						Login = wnd.Login,
						Password = wnd.Password,
						AutoLogon = wnd.AutoLogon
					};
				}

				try
				{
					client.Credentials.Login = credentials.Login;
					client.Credentials.Password = credentials.Password;
					client.Login();

					_persistableService.SetCredentials(credentials.Clone());
					new LoggedInCommand().Process(this);
				}
				catch (Exception excp)
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str3647 + excp.Message)
						.Error()
						.Owner(this)
						.Show();

					new LogInCommand(false).Process(this);
				}
			});

			cmdSvc.Register<RefreshSecurities>(this, false, cmd => ThreadingHelper
				.Thread(() =>
				{
					var entityRegistry = ConfigManager.GetService<IEntityRegistry>();
					var count = 0;
					var progress = 0;

					try
					{
						using (var client = new RemoteStorageClient(new Uri(cmd.Settings.Path)))
						{
							var credentials = _persistableService.GetCredentials();

							client.Credentials.Login = credentials.Login;
							client.Credentials.Password = credentials.Password;

							foreach (var secType in cmd.Types.TakeWhile(secType => !cmd.IsCancelled()))
							{
								if (secType == SecurityTypes.Future)
								{
									var from = DateTime.Today.AddMonths(-4);
									var to = DateTime.Today.AddMonths(4);
									var expiryDates = from.GetExpiryDates(to);

									foreach (var expiryDate in expiryDates.TakeWhile(d => !cmd.IsCancelled()))
									{
										client.Refresh(entityRegistry.Securities, new Security { Type = secType, ExpiryDate = expiryDate }, s =>
										{
											entityRegistry.Securities.Save(s);
											count++;
										}, cmd.IsCancelled);
									}
								}
								else
								{
									// для акций передаем фиктивное значение ExpiryDate, чтобы получить инструменты без даты экспирации
									var expiryDate = secType == SecurityTypes.Stock ? DateTime.Today : (DateTime?)null;

									client.Refresh(entityRegistry.Securities, new Security { Type = secType, ExpiryDate = expiryDate }, s =>
									{
										entityRegistry.Securities.Save(s);
										count++;
									}, cmd.IsCancelled);
								}

								cmd.ProgressChanged(++progress);
							}
						}
					}
					catch (Exception ex)
					{
						ex.LogError();
					}

					if (cmd.IsCancelled())
						return;

					try
					{
						cmd.WhenFinished(count);
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				})
				.Launch());

			#endregion

			#region Security commands

			cmdSvc.Register<EditSecurityCommand>(this, true, cmd =>
			{
				ISecurityWindow wnd;

				if (cmd.Security is ContinuousSecurity)
					wnd = new ContinuousSecurityWindow { SecurityProvider = _entityRegistry.Securities };
				else if (cmd.Security is ExpressionIndexSecurity)
					wnd = CreateIndexSecurityWindow();
				else
					wnd = new SecurityCreateWindow();

				wnd.Security = cmd.Security.Clone();

				if (((Window)wnd).ShowModal(Application.Current.GetActiveOrMainWindow()))
				{
					wnd.Security.CopyTo(cmd.Security);
					_entityRegistry.Securities.Save(cmd.Security);
				}
			});
			cmdSvc.Register<CreateSecurityCommand>(this, true, cmd =>
			{
				ISecurityWindow wnd;

				if (cmd.SecurityType == typeof(Security))
					wnd = new SecurityCreateWindow();
				else if (cmd.SecurityType == typeof(ContinuousSecurity))
					wnd = new ContinuousSecurityWindow { SecurityProvider = _entityRegistry.Securities };
				else if (cmd.SecurityType == typeof(IndexSecurity))
					wnd = CreateIndexSecurityWindow();
				else
					throw new InvalidOperationException(LocalizedStrings.Str2140Params.Put(cmd.SecurityType));

				wnd.ValidateId = id =>
				{
					if (_entityRegistry.Securities.ReadById(id) != null)
					{
						return LocalizedStrings.Str2927Params.Put(id);
					}

					return null;
				};

				if (((Window)wnd).ShowModal(Application.Current.GetActiveOrMainWindow()))
				{
					//wnd.Security.Connector = ConfigManager.GetService<IConnector>();
					_entityRegistry.Securities.Save(wnd.Security);
					cmd.Security = wnd.Security;
				}
			});

			#endregion

			#region Position commands

			cmdSvc.Register<PositionEditCommand>(this, true, cmd =>
			{
				var isPortfolio = cmd.Position is Portfolio;

				var copy = isPortfolio
					? (BasePosition)((Portfolio)cmd.Position).Clone()
					: ((Position)cmd.Position).Clone();

				var wnd = new PositionEditWindow
				{
					Position = copy
				};

				if (!wnd.ShowModal(Application.Current.GetActiveOrMainWindow()))
					return;

				if (isPortfolio)
				{
					var pf = (Portfolio)cmd.Position;

					((Portfolio)copy).CopyTo(pf);

					_entityRegistry.Portfolios.Save(pf);
					_connector.SendToEmulator(new[] { pf.ToChangeMessage() });
					new PortfolioCommand(pf, wnd.IsNew).Process(this);
				}
				else
				{
					var pos = (Position)cmd.Position;

					((Position)copy).CopyTo(pos);

					_entityRegistry.Positions.Save(pos);
					_connector.SendToEmulator(new[] { pos.ToChangeMessage() });
					//new PositionCommand(DateTime.Now, pos, wnd.IsNew).Process(this);
				}
			});

			#endregion

			cmdSvc.Register<AddLogListenerCommand>(this, false, cmd => ConfigManager.GetService<LogManager>().Listeners.Add(cmd.Listener));
			cmdSvc.Register<RemoveLogListenerCommand>(this, false, cmd => ConfigManager.GetService<LogManager>().Listeners.Remove(cmd.Listener));

			cmdSvc.Register<RequestBindSource>(this, true, cmd => new BindConnectorCommand(ConfigManager.GetService<IConnector>(), cmd.Control).SyncProcess(this));

			cmdSvc.Register<ControlChangedCommand>(this, false, cmd => SaveLayout());
		}

		private void InitializeCompositions()
		{
			var compositionSerializer = new CompositionRegistry();

			ConfigManager.RegisterService(compositionSerializer);

			compositionSerializer.DiagramElements.AddRange(StockSharp.Configuration.Extensions.GetDiagramElements());
			
			Properties
				.Resources
				.ResourceManager
				.LoadCompositions()
				.ForEach(s => compositionSerializer.Load(s.LoadSettingsStorage(), true));
			
			UserConfig
				.Instance
				.LoadCompositions()
				.ForEach(s => compositionSerializer.Load(s.LoadSettingsStorage()));
			
			compositionSerializer.SaveComposition += UserConfig.Instance.SaveComposition;
			compositionSerializer.RemoveComposition += element =>
			{
				UserConfig.Instance.RemoveComposition(element);
				new CloseWindowCommand(element.TypeId.To<string>(), typeof(DiagramPanel)).Process(this);
			};
		}

		private void InitializeToolBar()
		{
			TargetPlatformMenuItem.Visibility = Environment.Is64BitOperatingSystem ? Visibility.Visible : Visibility.Hidden;

			// контролы из конфига
			AppConfig.Instance.ToolControls.ForEach(AddToolControl);

			// контролы для инструментов
			CustomItemToolBar.Items.Add(new RibbonSeparator());
			AddToolControl(typeof(SecuritiesPanel));

			var item = CreateRibbonPopupButton(typeof(IndexSecurityPanel),
				typeof(ExpressionIndexSecurity),
				_entityRegistry.GetIndexSecurities,
				s => new OpenIndexSecurityPanelCommand((ExpressionIndexSecurity)s).SyncProcess(this));

			CustomItemToolBar.Items.Add(item);

			item = CreateRibbonPopupButton(typeof(ContinuousSecurityPanel),
				typeof(ContinuousSecurity),
				_entityRegistry.GetContinuousSecurities,
				s => new OpenContinuousSecurityPanelCommand((ContinuousSecurity)s).SyncProcess(this));

			CustomItemToolBar.Items.Add(item);
		}

		//private void InitializeChat()
		//{
		//	var chatClient = new ChatClient();
		//	ConfigManager.RegisterService(chatClient);
		//}

		private void AddToolControl(Type type)
		{
			var id = type.GUID.ToString();

			var mi = CreateRibbonButton(type);
			mi.Click += (sender, args) => new OpenWindowCommand(id, type, false).Process(this);

			CustomItemToolBar.Items.Add(mi);
		}

		private static RibbonButton CreateRibbonButton(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			var iconUrl = type.GetIconUrl();

			return new RibbonButton
			{
				Label = type.GetDisplayName(),
				ToolTip = type.GetDescription(),
				ImageSourceLarge = iconUrl == null ? null : new BitmapImage(iconUrl)
			};
		}

		private static RibbonPopupButton CreateRibbonPopupButton(Type controlType, Type securityType, Func<IEnumerable<Security>> getSecurities, Action<Security> open)
		{
			if (controlType == null)
				throw new ArgumentNullException(nameof(controlType));

			var iconUrl = controlType.GetIconUrl();

			return new SecurityPopupButton(securityType, getSecurities, open)
			{
				Label = controlType.GetDisplayName(),
				ToolTip = controlType.GetDescription(),
				ImageSourceLarge = iconUrl == null ? null : new BitmapImage(iconUrl)
			};
		}

		private static IndexSecurityWindow CreateIndexSecurityWindow()
		{
			var wnd = new IndexSecurityWindow();
			wnd.Securities.AddRange(ConfigManager.GetService<IStudioEntityRegistry>().Securities);
			return wnd;
		}

		private void InitializeAutomaticUpdater()
		{
			AutomaticUpdater.Translate();
		}

		private IStudioControl OpenWindow(StrategyInfo info)
		{
			return OpenWindow(info.Id.To<string>(), typeof(StrategyInfoContent), false, info, () =>
			{
				var ctrl = new StrategyInfoContent { StrategyInfo = info };

				ctrl.OpenDefaultPanes();

				ConfigManager
					.GetService<IStudioCommandService>()
					.Bind(info, ctrl);

				return ctrl;
			});
		}

		private IStudioControl OpenWindow(CompositionDiagramElement composition)
		{
			return OpenWindow(composition.TypeId.To<string>(), typeof(DiagramPanel), false, composition, () =>
			{
				var ctrl = new DiagramPanel { Composition = composition };

				ConfigManager
					.GetService<IStudioCommandService>()
					.Bind(composition, ctrl);

				return ctrl;
			});
		}

		private IStudioControl OpenWindow(MarketDataSettings settings)
		{
			var type = typeof(MarketDataPanel);
			var ctrl = OpenWindow(type.GUID.To<string>(), type, false, null, () => type.CreateInstance<IStudioControl>());

			((MarketDataPanel)ctrl).SelectedSettings = settings;

			return ctrl;
		}

		private IStudioControl OpenWindow<T>(Security security)
			where T : CompositeSecurityPanel
		{
			var type = typeof(T);

			string id;

			if (security.Id.IsEmpty())
			{
				id = Guid.NewGuid().ToString();

				// после сохранения инструмента меняется его Id, необходимо запомнить его, что сохранить настройки
				((INotifyPropertyChanged)security).PropertyChanged += (s, a) =>
				{
					if (!a.PropertyName.CompareIgnoreCase("Id"))
						return;

					var oldKey = Tuple.Create(id, type);
					var content = _contents.TryGetValue(oldKey);

					if (content == null)
						return;

					_contents.Remove(oldKey);
					_contents.Add(Tuple.Create(security.Id, type), content);
				};
			}
			else
				id = security.Id;

			return OpenWindow(id, type, false, security, () =>
			{
				var control = (CompositeSecurityPanel)type.CreateInstance<IStudioControl>();
				control.Security = security;
				return control;
			});
		}

		private IStudioControl OpenWindow(string id, Type ctrlType, bool isToolWindow, object context, Func<IStudioControl> getControl)
		{
			if (ctrlType == null)
				throw new ArgumentNullException(nameof(ctrlType));

			if (getControl == null)
				throw new ArgumentNullException(nameof(getControl));

			id = id.ToLowerInvariant();
			var name = "ToolWindow" + id.Replace("-", "").Replace("@", "") + ctrlType.Name;

			var wnd = _contents.SafeAdd(Tuple.Create(id, ctrlType), key =>
			{
				if (isToolWindow)
				{
					var toolWnd = new ContentToolWindow { Tag = context, Control = getControl(), Name = name };
					DockSite.ToolWindows.Add(toolWnd);
					return toolWnd;
				}
				else
				{
					var docWnd = new ContentDocumentWindow { Tag = context, Control = getControl(), Name = name };
					DockSite.DocumentWindows.Add(docWnd);
					return docWnd;
				}
			});

			((DockingWindow)wnd).Activate();
			return wnd.Control;
		}

		private void WindowOpened(IStudioControl control, bool isMainWindow)
		{
			var type = control.GetType();

			if (type != typeof(LogManagerPanel))
			{
				RibbonGroupPortfolio.SetVisibility<PortfoliosPanel>(control);
				RibbonGroupSecurity.SetVisibility<SecuritiesPanel>(control);
				RibbonGroupIndexSecurity.SetVisibility<CompositeSecurityPanel>(control);
				RibbonTabCommon.DiagramPanelGroup.SetVisibility<DiagramPanel>(control);
			}

			if (type == typeof(StrategyInfoContent))
			{
				var ctrl = (StrategyInfoContent)control;
				var info = ctrl.StrategyInfo;

				switch (info.Type)
				{
					case StrategyInfoTypes.SourceCode:
					case StrategyInfoTypes.Diagram:
					case StrategyInfoTypes.Assembly:
						Ribbon.SelectedTab = RibbonTabCommon;
						break;

					case StrategyInfoTypes.Analytics:
						Ribbon.SelectedTab = RibbonTabAnalytics;
						break;

					case StrategyInfoTypes.Terminal:
						Ribbon.SelectedTab = RibbonTabTerminal;
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				SelectedStrategyInfo = info;
				SelectedStrategy = null;

				if (ctrl.SelectedPane != null)
					WindowOpened(ctrl.SelectedPane.Control, false);
			}
			else if (type == typeof(StrategyContent))
			{
				var ctrl = (StrategyContent)control;

				SelectedStrategy = ctrl.Strategy;
				SelectedEmulationService = ctrl.EmulationService;
			}
			else if (type == typeof(OptimizatorContent))
			{
				var ctrl = (OptimizatorContent)control;

				SelectedStrategy = ctrl.Strategy;
				SelectedStrategies = ctrl.SelectedStrategies;
				SelectedEmulationService = ctrl.EmulationService;
			}
			else if (type == typeof(StrategyInfoCodeContent))
			{
				SelectedStrategy = null;
				SelectedEmulationService = null;
			}
			else if (type == typeof(DiagramPanel))
			{
				var diagramPanel = (DiagramPanel)control;

				// для вкладки составного блока нет информации по типу стратегии
				if (diagramPanel.StrategyInfo == null)
					SelectedStrategyInfo = null;

				SelectedStrategy = null;
				SelectedEmulationService = null;

				RibbonTabCommon.DiagramPanelGroup.DiagramPanel = diagramPanel;
				Ribbon.SelectedTab = RibbonTabCommon;
			}
			else if (isMainWindow && (AppConfig.Instance.ToolControls.Any(t => t == type) || type == typeof(SecuritiesPanel)))
			{
				Ribbon.SelectedTab = RibbonTabAdditional;

				SelectedEmulationService = null;
				SelectedStrategy = null;
				SelectedStrategyInfo = null;
			}
			else if (control is CompositeSecurityPanel)
			{
				Ribbon.SelectedTab = RibbonTabAdditional;
				RibbonGroupIndexSecurity.DataContext = control;

				SelectedEmulationService = null;
				SelectedStrategy = null;
				SelectedStrategyInfo = null;
			}
		}

		private void CloseWindow(string id, Type ctrlType)
		{
			if (ctrlType == null)
				throw new ArgumentNullException(nameof(ctrlType));

			var key = Tuple.Create(id, ctrlType);

			var content = _contents.TryGetValue(key);

			if (content == null)
				return;

			_contents.Remove(key);
			((DockingWindow)content).Destroy();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_entityRegistry = new StudioEntityRegistry();
			_entityRegistry.Strategies.Added += info => new OpenStrategyInfoCommand(info).Process(this);
			_entityRegistry.Strategies.Removed += info => new CloseWindowCommand(info.Id.To<string>(), typeof(StrategyInfoContent)).Process(this);

			ConfigManager.RegisterService<IEntityRegistry>(_entityRegistry);
			ConfigManager.RegisterService<IStudioEntityRegistry>(_entityRegistry);
			ConfigManager.RegisterService(AuthenticationClient.Instance);
			ConfigManager.RegisterService<IAlertService>(new AlertService(UserConfig.Instance.MainFolder));
			ConfigManager.RegisterService<IExchangeInfoProvider>(new ExchangeInfoProvider(_entityRegistry));
			ConfigManager.RegisterService(CreateMarketDataSettingsCache());

			_connector = new StudioConnector();
			ConfigManager.GetService<LogManager>().Sources.Add(_connector);
			var strategyService = new StrategyService();

			ConfigManager.RegisterService<IConnector>(_connector);
			ConfigManager.RegisterService<ISecurityProvider>(new FilterableSecurityProvider(_connector));
			_connector.Connect();

			ConfigManager.RegisterService<IConnector>(_connector);
			ConfigManager.RegisterService<IStrategyService>(strategyService);

			InitializeAutomaticUpdater();
			//InitializeChat();

			RegisterCommandHandlers();

			InitializeCompositions();
			InitializeToolBar();

			LicenseHelper.LicenseChanged += () => new LicenseChangedCommand().Process(this);
		}

		private void DockSite_OnLoaded(object sender, RoutedEventArgs e)
		{
			UserConfig.Instance.ResumeChangesMonitor();

			new LogInCommand().Process(this);
		}

		private void DockSite_OnWindowClosing(object sender, DockingWindowEventArgs e)
		{
			//окна могут закрываться при загрузке разметки
			if (UserConfig.Instance.IsChangesSuspended)
				return;

			var contentWindow = e.Window as IContentWindow;
			if (contentWindow == null)
				return;

			var window = contentWindow as ContentDocumentWindow;
			if (window != null)
			{
				var strategyInfo = contentWindow.Tag as StrategyInfo;
				if (strategyInfo != null && strategyInfo.State != StrategyInfoStates.Stopped && strategyInfo.Type != StrategyInfoTypes.Terminal)
				{
					e.Cancel = true;

					new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3648Params.Put(window.Title))
						.Warning()
						.Show();

					return;
				}

				if (contentWindow.Tag != null)
				{
					var key = contentWindow.Control.GetKey();
					if (key != null)
						ConfigManager.GetService<IStudioCommandService>().UnBind(key);
				}

				//закрываем только вкладки, общие окна (портфели, инструменты) не удаляем
				contentWindow.Control.Dispose();
				_contents.RemoveByValue(window);
			}
			
			SaveLayout();
		}

		private void DockSite_OnWindowClosed(object sender, DockingWindowEventArgs e)
		{
			if (DockSite.ActiveWindow == null)
			{
				SelectedEmulationService = null;
				SelectedStrategy = null;
				SelectedStrategyInfo = null;

				RibbonGroupPortfolio.Visibility = Visibility.Collapsed;
				RibbonGroupSecurity.Visibility = Visibility.Collapsed;
				RibbonGroupIndexSecurity.Visibility = Visibility.Collapsed;
				RibbonTabCommon.DiagramPanelGroup.Visibility = Visibility.Collapsed;
			}
			else
			{
				var ctrl = DockSite.ActiveWindow.Content as IStudioControl;

				if (ctrl != null)
					WindowOpened(ctrl, false);
			}
		}

		private void DockSite_OnWindowOpened(object sender, DockingWindowEventArgs e)
		{
			SaveLayout();
		}

		private void DockSite_OnWindowStateChanged(object sender, DockingWindowStateChangedEventArgs e)
		{
			SaveLayout();
		}

		private void DockSite_OnWindowDragged(object sender, RoutedEventArgs e)
		{
			SaveLayout();
		}

		private void DockSite_WindowActivated(object sender, DockingWindowEventArgs e)
		{
			var window = e.Window as IContentWindow;

			if (window == null)
				return;

			new ControlOpenedCommand(window.Control, Equals(e.OriginalSource, DockSite)).SyncProcess(this);
		}

		private void InitializeControls()
		{
			UserConfig.Instance.SuspendChangesMonitor();

			InitializeConnector();

			var layout = _persistableService.GetValue<SettingsStorage>("MainWindow") ??
				Properties.Resources.DefaultMainWindow.LoadSettingsStorage();

			//сначала загружаем разметку, чтобы когда есть только разметка поумолчанию она не переписывала разметку новых стратегий
			layout.LoadUISettings(DockSite, _contents);

			_entityRegistry.CreateDefaultStrategies();

			UserConfig.Instance.ResumeChangesMonitor();

			if (_persistableService.GetAutoConnect())
				Connect();
		}

		private void InitializeConnector()
		{
			try
			{
				var sessionSettings = _persistableService.GetStudioSession();

				if (sessionSettings != null)
				{
					_connector.TransactionAdapter.Load(sessionSettings.GetValue<SettingsStorage>("TransactionAdapter"));
					_connector.MarketDataAdapter.Load(sessionSettings.GetValue<SettingsStorage>("MarketDataAdapter"));
				}
				else
					_connector.AddStockSharpFixConnection(AppConfig.Instance.FixServerAddresss);
			}
			catch (Exception ex)
			{
				ex.LogError();

				new MessageBoxBuilder()
					.Owner(this)
					.Description(LocalizedStrings.Str3649)
					.Text(ex.ToString())
					.Error()
					.Show();
			}

			_connector.Connected += Trader_Connected;
			_connector.Disconnected += Trader_Disconnected;
			_connector.ConnectionError += Trader_ConnectionError;
		}

		private MarketDataSettingsCache CreateMarketDataSettingsCache()
		{
			var marketDataSettingsCache = new MarketDataSettingsCache();

			var cache = _persistableService.GetValue<SettingsStorage>("MarketDataSettingsCache");

			if (cache == null)
				marketDataSettingsCache.Settings.Add(MarketDataSettings.StockSharpSettings);
			else
				marketDataSettingsCache.Load(cache);

			marketDataSettingsCache.Changed += () => _persistableService.SetValue("MarketDataSettingsCache", ((IPersistable)marketDataSettingsCache).Save());

			return marketDataSettingsCache;
		}

		private void Connect()
		{
			try
			{
				var innerAdapters = ((BasketMessageAdapter)_connector.MarketDataAdapter).InnerAdapters;

				if (innerAdapters.IsEmpty())
				{
					new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3650)
						.Warning()
						.Show();

					if (!ProcessConnectionSettings())
						return;
				}

				var mdAdapters = innerAdapters.SortedAdapters.ToArray();

				if (mdAdapters.IsEmpty())
				{
					new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3651)
						.Warning()
						.Show();

					if (!ProcessConnectionSettings())
						return;
				}

				//if (mdSessions.Count() > 1)
				//{
				//	var msg = "Multi".ValidateLicense();
				//	if (msg != null)
				//	{
				//		new MessageBoxBuilder()
				//		   .Owner(this)
				//		   .Text(msg)
				//		   .Error()
				//		   .Show();
						
				//		return;
				//	}
				//}

				ConnectBtn.IsEnabled = false;

				_showConnectionErrors = true;
				_connector.Connect();
			}
			catch (Exception ex)
			{
				_connector.AddErrorLog(ex);

				if (_connector.ConnectionState != ConnectionStates.Disconnected)
					_connector.Disconnect();
			}
		}

		private void Disconnect()
		{
			try
			{
				ConnectBtn.IsEnabled = false;

				_connector.Disconnect();
			}
			catch (Exception ex)
			{
				_connector.AddErrorLog(ex);

				if (_connector.ConnectionState != ConnectionStates.Disconnected)
					_connector.Disconnect();
			}
		}

		private static void SaveLayout()
		{
			if (UserConfig.Instance.IsChangesSuspended)
				return;

			UserConfig.Instance.MarkLayoutChanged();
		}

		private void UpdateLicenseToolbar()
		{
			try
			{
				LicenseGroup.Licenses = LicenseHelper.Licenses;

				using (var client = new LicenseClient())
				{
					LicenseGroup.Brokers = client.Brokers;
					LicenseGroup.Features = client.Features;
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			// если что-то еще не "сбросилось" в БД
			_entityRegistry.DelayAction.WaitFlush();

			//ConfigManager.GetService<ChatClient>().Dispose();
			ConfigManager.GetService<AuthenticationClient>().Dispose();

			_connector.Connected -= Trader_Connected;
			_connector.Disconnected -= Trader_Disconnected;
			_connector.ConnectionError -= Trader_ConnectionError;

			// временное решение для сохранения аннотаций на графике,
			// т.к. нет способа определить изменение положения или параметров аннотаций.
			SaveLayout();

			//Вызывать UserConfig.Dispose необходимо перед Trader.Dispose
			//т.к. при вызове UserConfig.Dispose выполняется сохранение конфигурации,
			//а вызов Trader.Dispose сбрасывает приоритеты для трейдеров.

			UserConfig.Instance.Dispose();
			_connector.Dispose();

			base.OnClosing(e);
		}

		private void ExecutedConnect(object sender, ExecutedRoutedEventArgs e)
		{
			if (_connector.ConnectionState == ConnectionStates.Disconnected || _connector.ConnectionState == ConnectionStates.Failed)
			{
				Connect();
			}
			else
			{
				Disconnect();
			}
		}

		private void ExecutedExit(object sender, ExecutedRoutedEventArgs e)
		{
			Application.Current.Shutdown(110);
		}

		private bool CheckState(StrategyInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (info.State == StrategyInfoStates.Runned)
			{
				new MessageBoxBuilder()
					.Owner(this)
					.Text(LocalizedStrings.Str3652Params.Put(info.Name))
					.Error()
					.Show();

				return false;
			}

			return true;
		}

		private void Trader_Connected()
		{
			ChangeConnectionControls();

			_showConnectionErrors = true;
		}

		private void Trader_Disconnected()
		{
			ChangeConnectionControls();
			_showConnectionErrors = false;
		}

		private void Trader_ConnectionError(Exception error)
		{
			ChangeConnectionControls();

			if (!_showConnectionErrors)
				return;

			_showConnectionErrors = false;

			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				var msg = LocalizedStrings.Str625Params.Put("Studio", error.Message);

				if (error.InnerException != null)
					msg = msg + Environment.NewLine + LocalizedStrings.Str3654 + error.InnerException.Message;

				new MessageBoxBuilder()
					.Owner(this)
					.Text(msg)
					.Error()
					.Show();
			});
		}

		private void ChangeConnectionControls()
		{
			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				switch (_connector.ConnectionState)
				{
					case ConnectionStates.Connecting:
					case ConnectionStates.Disconnecting:
						ConnectBtn.IsEnabled = false;
						break;

					case ConnectionStates.Connected:
						ConnectBtn.IsEnabled = true;
						ConnectBtn.ToolTip = ConnectBtn.Label = LocalizedStrings.Disconnect;
						ConnectBtn.ImageSourceLarge = new BitmapImage(new Uri("pack://application:,,,/Studio;component/Images/disconnect.png"));
						break;

					case ConnectionStates.Disconnected:
					case ConnectionStates.Failed:
						ConnectBtn.IsEnabled = true;
						ConnectBtn.ToolTip = ConnectBtn.Label = LocalizedStrings.Connect;
						ConnectBtn.ImageSourceLarge = new BitmapImage(new Uri("pack://application:,,,/Studio;component/Images/connect.png"));
						break;
				}
				
				//bmp.InvalidateMeasure();
				//bmp.InvalidateVisual();
			});
		}

		private static void RemoveStrategy(StrategyContainer strategy)
		{
			strategy.StrategyInfo.Strategies.Remove(strategy);
			strategy.Dispose();
		}

		private void ExecutedCheckForUpdatesCommand(object sender, ExecutedRoutedEventArgs e)
		{
			AutomaticUpdater.ForceCheckForUpdate(true);
		}

		private void ExecutedAboutCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new AboutWindow(this).ShowModal(this);
		}

		private bool ProcessConnectionSettings()
		{
			var autoConnect = _persistableService.GetAutoConnect();

			if (!_connector.Adapter.Configure(this, ref autoConnect))
				return false;

			var settings = new SettingsStorage
			{
				{ "TransactionAdapter", _connector.TransactionAdapter.Save() },
				{ "MarketDataAdapter", _connector.MarketDataAdapter.Save() }
			};
			_persistableService.SetStudioSession(settings);
			_persistableService.SetAutoConnect(autoConnect);

			return true;
		}

		private void ExecutedStockSharpConnect(object sender, ExecutedRoutedEventArgs e)
		{
			new LogInCommand().Process(this);
		}

		private void ExecutedConnectionSettings(object sender, ExecutedRoutedEventArgs e)
		{
			ProcessConnectionSettings();
		}

		private void DataDirectoryCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Process.Start(UserConfig.Instance.MainFolder);
		}

		private void TargetPlatformCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var language = LocalizedStrings.ActiveLanguage;
			var platform = Environment.Is64BitProcess ? Platforms.x64 : Platforms.x86;

			var window = new TargetPlatformWindow();

			if (!window.ShowModal(this))
				return;

			if (window.SelectedLanguage == language && window.SelectedPlatform == platform)
				return;

			// temporarily set prev lang for display the followed message
			// and leave all text as is if user will not restart the app
			LocalizedStrings.ActiveLanguage = language;

			var result = new MessageBoxBuilder()
				.Text(LocalizedStrings.Str2952Params.Put(TypeHelper.ApplicationName))
				.Owner(this)
				.Info()
				.YesNo()
				.Show();

			if (result == MessageBoxResult.Yes)
				Application.Current.Restart();
		}

		private void ExecutedPortfolioSettings(object sender, ExecutedRoutedEventArgs e)
		{
			var adapter = _connector.TransactionAdapter;

			new PortfolioMessageAdaptersWindow { Adapter = (BasketMessageAdapter)adapter }.ShowModal(this);

			_persistableService.SetStudioSession(adapter.Save());
		}

		private void ExecutedNewPortfolioCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var type = (Type)e.Parameter;
			var basePosition = type.CreateInstance<BasePosition>();

			new PositionEditCommand(basePosition).Process(this);
		}

		private void ExecutedNewSecurityCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new CreateSecurityCommand((Type)e.Parameter).Process(this, true);
		}

		private void ExecutedLookupSecurityCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new SecuritiesWindowEx { Title = LocalizedStrings.Str3657, IsLookup = true }.ShowModal(this);
		}

		private static void Open(string url)
		{
			url.To<Uri>().OpenLinkInBrowser();
		}

		private void ExecutedDocumentationCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Open("http://stocksharp.com/forum/yaf_postst5267_Dokumientatsiia-S--Studio.aspx");
		}

		private void ExecutedEduCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Open("http://stocksharp.com/edu/");
		}

		private void ExecutedForumCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Open("http://stocksharp.com/forum/yaf_topics26_S--Studio.aspx");
		}

		private void ExecutedChatCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Open("http://stocksharp.com/chat/");
		}
	}
}