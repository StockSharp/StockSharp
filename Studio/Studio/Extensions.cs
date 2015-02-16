namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Reflection;
	using System.Resources;
	using System.Windows;

	using ActiproSoftware.Windows.Controls.Docking;
	using ActiproSoftware.Windows.Controls.Ribbon.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Community;
	using StockSharp.Fix;
	using StockSharp.Licensing;
	using StockSharp.Logging;
	using StockSharp.Studio.Controls;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Studio.Services;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Code;
	using StockSharp.Xaml.Diagram;
	using StockSharp.Localization;
	using License = StockSharp.Licensing.License;

	internal static class Extensions
	{
		public static void CheckExchange(this Security security)
		{
			// TODO Временный метод для проверки ошибки с нулевой биржей.

			if (security == null)
				throw new ArgumentNullException("security");

			if (security.Board == null)
				throw new ArgumentException(LocalizedStrings.Str3602Params.Put(security), "security");
		}

        public static StrategyContainer CreateStrategy(this StrategyInfo info, SessionType sessionType)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			var registry = ConfigManager.GetService<IStudioEntityRegistry>();
			var security = "RI".GetFortsJumps(DateTime.Today, DateTime.Today.AddMonths(3), code => registry.Securities.LookupById(code + "@" + ExchangeBoard.Forts.Code)).Last();

	        var container = new StrategyContainer
	        {
		        StrategyInfo = info,
				Portfolio = GetDefaultPortfolio(sessionType),
				Security = security,
		        MarketDataSettings = ConfigManager.GetService<MarketDataSettingsCache>().Settings.First(s => s.Id != Guid.Empty),
				SessionType = sessionType
	        };

	        container.InitStrategy();

			return container;
		}

		private static Portfolio GetDefaultPortfolio(SessionType sessionType)
		{
			var registry = ConfigManager.GetService<IStudioEntityRegistry>();
			var credentails = ConfigManager.GetService<IPersistableService>().GetCredentials();

			if (credentails == null || credentails.Login.IsEmpty() || sessionType != SessionType.Battle)
				return registry.Portfolios.First(p => p.Board == ExchangeBoard.Test);

			var portfolio = registry.Portfolios.ReadById(credentails.Login);

			if (portfolio != null)
				return portfolio;

			portfolio = new Portfolio
			{
				Name = credentails.Login,
				Board = ExchangeBoard.Forts
			};

			registry.Portfolios.Save(portfolio);

			return portfolio;
		}

		public static void UpdateName(this StrategyContainer strategy)
		{
			var info = strategy.StrategyInfo;
			var index = info.Strategies.Count(s => s.SessionType == strategy.SessionType) + 1;

			switch (strategy.SessionType)
			{
				case SessionType.Battle:
					switch (info.Type)
					{
						case StrategyInfoTypes.SourceCode:
						case StrategyInfoTypes.Diagram:
						case StrategyInfoTypes.Assembly:
							strategy.Name = LocalizedStrings.Str3599 + " " + index;
							break;

						case StrategyInfoTypes.Analytics:
							strategy.Name = LocalizedStrings.Str3604 + index;
							break;

						case StrategyInfoTypes.Terminal:
							strategy.Name = LocalizedStrings.Str3605 + index;
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
					break;

				case SessionType.Emulation:
					strategy.Name = LocalizedStrings.Str3606 + index;
					break;

				case SessionType.Optimization:
					strategy.Name = LocalizedStrings.Str3177 + " " + index;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static void UpdateStrategies(this StrategyInfo info)
		{
			foreach (var strategy in info.Strategies)
			{
				if (strategy.ProcessState != ProcessStates.Stopped)
                    strategy.NeedRestart = true;
				else
					strategy.InitStrategy();
			}
		}

		private static void SubscribePropertyChanged(this StrategyInfo info, string propName)
		{
			info.PropertyChanged += (arg1, arg2) =>
			{
				if (!arg2.PropertyName.CompareIgnoreCase(propName))
					return;

				info.UpdateStrategies();
			};
		}

		private static void SubscribeAssemblyChanged(this StrategyInfo info)
		{
			var watcher = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(info.Path),
				Filter = Path.GetFileName(info.Path),
				NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size
			};
			watcher.Changed += (s, e) => info.ReloadAssemblyType();
			watcher.EnableRaisingEvents = true;
		}

		private static Assembly LoadAssembly(this StrategyInfo info)
		{
			//var assemblyBytes = info.Assembly;

			//var savedHash = assemblyBytes.GetMd5Hash();
			//var fileHash = File.ReadAllBytes(info.Path).GetMd5Hash();

			//if (savedHash != fileHash)
			//	throw new InvalidOperationException("Не совпадают хэш файла '{0}' и сохраненной сборки для '{1}'.".Put(info.Path, info.Name));

			return Assembly.Load(info.Assembly);
		}

		public static Tuple<byte[], Assembly> LoadAssembly(this string path)
		{
			var bytes = File.ReadAllBytes(path);

			try
			{
				return Tuple.Create(bytes, Assembly.Load(bytes));
			}
			catch
			{
				return null;
			}
		}

		private static void ReloadAssemblyType(this StrategyInfo info)
		{
			if (info.Path.IsEmpty())
				return;

			var tuple = info.Path.LoadAssembly();

			if (tuple == null)
				throw new InvalidOperationException(LocalizedStrings.Str3607Params.Put(info.Path));

			if (info.StrategyType != null && info.Assembly != null && tuple.Item1.SequenceEqual(info.Assembly))
				return;

			info.Assembly = tuple.Item1;
			info.StrategyType = tuple.Item2.GetType(info.StrategyTypeName.Split(',')[0]);
		}

		private static void ReloadSourceCodeType(this StrategyInfo info)
		{
			if (info.Path.IsEmpty())
				return;

			info.StrategyType = info.LoadAssembly().GetTypes().FirstOrDefault(t => !t.IsAbstract && t.IsSubclassOf(typeof(Strategy)));
		}

		public static void InitStrategyType(this StrategyInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			switch (info.Type)
			{
				case StrategyInfoTypes.SourceCode:
				case StrategyInfoTypes.Analytics:
					info.ReloadSourceCodeType();
					info.SubscribePropertyChanged("StrategyType");
					break;

				case StrategyInfoTypes.Diagram:
					info.StrategyType = typeof(DiagramStrategy);
					info.SubscribePropertyChanged("Body");
					break;

				case StrategyInfoTypes.Assembly:
					info.ReloadAssemblyType();
					info.SubscribeAssemblyChanged();
					info.SubscribePropertyChanged("StrategyType");
					break;

				case StrategyInfoTypes.Terminal:
					info.StrategyType = typeof(TerminalStrategy);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static CompilationResult CompileStrategy(this StrategyInfo info, IEnumerable<CodeReference> references)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (references == null)
				throw new ArgumentNullException("references");

			var result = CompilationLanguages.CSharp.CompileCode(info.Body, info.Name, references, UserConfig.Instance.StrategiesAssemblyPath, UserConfig.Instance.StrategiesTempPath);

			if (result.HasErrors())
				return result;

			var type = result.Assembly.GetTypes().FirstOrDefault(t => !t.IsAbstract && t.IsSubclassOf(typeof(Strategy)));

			if (type == null)
				throw new InvalidOperationException(LocalizedStrings.Str3608);

			info.Assembly = File.ReadAllBytes(result.Assembly.Location);
			info.Path = result.Assembly.Location;
			info.StrategyType = type;

			return result;
		}

		public static void CreateDefaultStrategies(this IStudioEntityRegistry registry)
		{
			if (registry == null)
				throw new ArgumentNullException("registry");

			if (registry.Strategies.Count != 0)
				return;

			new AddStrategyInfoCommand(new StrategyInfo
			{
				Name = LocalizedStrings.Str3291,
				Description = LocalizedStrings.Str3609,
				Path = string.Empty,
				StrategyType = typeof(DiagramStrategy),
				Body = Properties.Resources.SmaDiagramStrategy,
				Type = StrategyInfoTypes.Diagram
			}).Process(registry);

			new AddStrategyInfoCommand(new StrategyInfo
			{
				Name = LocalizedStrings.Str3610,
				Description = LocalizedStrings.Str3611,
				Path = string.Empty,
				StrategyType = typeof(DiagramStrategy),
				Body = Properties.Resources.ArbitrageStrategy,
				Type = StrategyInfoTypes.Diagram
			}).Process(registry);

			new AddStrategyInfoCommand(new StrategyInfo
			{
				Name = LocalizedStrings.Str3183,
				Description = LocalizedStrings.Str3612,
				Path = string.Empty,
				StrategyType = typeof(TerminalStrategy),
				Body = string.Empty,
				Type = StrategyInfoTypes.Terminal
			}).Process(registry);
		}

		public static void LoadState(this Strategy strategy, SessionStrategy sessionStrategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (sessionStrategy == null)
				throw new ArgumentNullException("sessionStrategy");

			var storage = new SettingsStorage
			{
				{ "Settings", sessionStrategy.Settings },
				{ "Statistics", sessionStrategy.Statistics },
				{ "Positions", sessionStrategy.Positions.Select(p => p.Position).ToList() },
			};

			strategy.SafeLoadState(storage);
		}

		public static IEnumerable<string> LoadCompositions(this ResourceManager manager)
		{
#if !DEBUG
			var compositions = manager
				.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true)
				.Cast<System.Collections.DictionaryEntry>()
				.Where(i => i.Key.ToString().EndsWith("DiagramElement"));

			return compositions.Select(pair => (string)pair.Value).ToList();
#else
			return Enumerable.Empty<string>();
#endif
		}

		public static void SaveUISettings(this SettingsStorage storage, DockSite dockSite, PairSet<Tuple<string, Type>, IContentWindow> contents)
		{
			storage.SetValue("Content", contents.Select(p =>
			{
				var ctrlStorage = new SettingsStorage();
				
				ctrlStorage.SetValue("Id", p.Key.Item1);
				ctrlStorage.SetValue("Type", p.Value.Control.GetType().GetTypeName(false));
				ctrlStorage.SetValue("IsToolWindow", p.Value is ContentToolWindow);
				ctrlStorage.SetValue("DockingWindowName", ((DockingWindow)p.Value).Name);
				ctrlStorage.SetValue("Settings", p.Value.Control.Save());
				ctrlStorage.SetValue("TagType", p.Value.Tag == null ? null : p.Value.Tag.GetType().GetTypeName(false));

				return ctrlStorage;
			}).ToArray());

			storage.SetValue("Layout", dockSite.SaveLayout());

			var window = dockSite.ActiveWindow;
			if (window != null)
				storage.SetValue("ActiveWindow", window.UniqueId.To<string>());
		}

		public static void LoadUISettings(this SettingsStorage storage, DockSite dockSite, PairSet<Tuple<string, Type>, IContentWindow> contents)
		{
			var controlsSettings = storage.GetValue<SettingsStorage[]>("Content");

			foreach (var ctrlSettings in controlsSettings)
			{
				try
				{
					var id = ctrlSettings.GetValue<string>("Id");
					var ctrlType = ctrlSettings.GetValue<string>("Type").To<Type>();
					var isToolWindow = ctrlSettings.GetValue<bool>("IsToolWindow");
					var dockingWindowName = ctrlSettings.GetValue<string>("DockingWindowName");
					var controlSettings = ctrlSettings.GetValue<SettingsStorage>("Settings");
					var tagType = ctrlSettings.GetValue<string>("TagType").To<Type>();

					var canClose = true;
					object tag = null;

					if (tagType == typeof(StrategyInfo))
					{
						tag = ConfigManager.GetService<IStudioEntityRegistry>().Strategies.ReadById(id.To<long>());

						if (tag == null)
							continue;
					}
					else if (tagType != null && tagType.IsSubclassOf(typeof(Strategy)))
					{
						var sessionStrategy = ConfigManager.GetService<IStudioEntityRegistry>().ReadSessionStrategyById(id.To<Guid>());

						if (sessionStrategy != null)
							tag = sessionStrategy.Strategy;

						if (tag == null)
							continue;
					}
					else if (tagType == typeof(CompositionDiagramElement))
					{
						tag = ConfigManager.GetService<CompositionRegistry>().DiagramElements.FirstOrDefault(c => c.TypeId == id.To<Guid>());

						if (tag == null)
							continue;
					}
					else if (tagType == typeof(ExpressionIndexSecurity) || tagType == typeof(ContinuousSecurity))
					{
						tag = ConfigManager.GetService<IStudioEntityRegistry>().Securities.ReadById(id);

						if (tag == null)
							continue;
					}

					var control = ctrlType.CreateInstance<IStudioControl>();

					var info = tag as StrategyInfo;
					if (info != null)
					{
						control.DoIf<IStudioControl, StrategyInfoCodeContent>(c =>
						{
							canClose = false;
							c.StrategyInfo = info;
						});
						control.DoIf<IStudioControl, DiagramPanel>(c =>
						{
							canClose = false;
							c.StrategyInfo = info;
						});
						control.DoIf<IStudioControl, StrategyInfoContent>(c => c.StrategyInfo = info);

						ConfigManager
								.GetService<IStudioCommandService>()
								.Bind(control.GetKey(), (IStudioCommandScope)control);
					}

					var strategy = tag as StrategyContainer;
					if (strategy != null)
					{
						control.DoIf<IStudioControl, StrategyContent>(c => c.SetStrategy(strategy));
						control.DoIf<IStudioControl, DiagramDebuggerPanel>(c => c.Strategy = strategy);
						control.DoIf<IStudioControl, OptimizatorContent>(c => c.SetStrategy(strategy));
					}

					var composition = tag as CompositionDiagramElement;
					if (composition != null)
						((DiagramPanel)control).Composition = composition;

					var security = tag as Security;
					if (security != null)
						((CompositeSecurityPanel)control).Security = security;

					var window = isToolWindow
						? (IContentWindow)new ContentToolWindow { Id = id, Tag = tag, Control = control, Name = dockingWindowName, CanClose = canClose }
						: new ContentDocumentWindow { Id = id, Tag = tag, Control = control, Name = dockingWindowName, CanClose = canClose };

					if (isToolWindow)
						dockSite.ToolWindows.Add((ToolWindow)window);
					else
						dockSite.DocumentWindows.Add((DocumentWindow)window);

					((DockingWindow)window).Activate();

					if (controlSettings != null)
						control.Load(controlSettings);

					contents.Add(Tuple.Create(id, ctrlType), window);
				}
				catch (Exception e)
				{
					e.LogError();
				}
			}

			var layout = storage.GetValue<string>("Layout");
			
			if (layout != null)
			{
				try
				{
					dockSite.LoadLayout(layout);
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}

			var activeWindow = storage.GetValue<string>("ActiveWindow");
			if (activeWindow != null)
			{
				var id = activeWindow.To<Guid>();

				var window = dockSite
					.ToolWindows
					.OfType<DockingWindow>()
					.Concat(dockSite.DocumentWindows)
					.FirstOrDefault(w => w.UniqueId == id);

				if (window != null)
					window.Activate();
			}
		}

		public static SessionStrategy ReadSessionStrategyById(this IStudioEntityRegistry registry, Guid id)
		{
			var battleSession = registry.Sessions.Battle;
			var sessionStrategy = battleSession.Strategies.ReadByStrategyId(id);

			if (sessionStrategy != null)
				return sessionStrategy;

			return registry
				.Sessions
				.Where(s => s != battleSession)
				.Select(s => s.Strategies.ReadByStrategyId(id))
				.FirstOrDefault(s => s != null);
		}

		public static void SetStrategy(this StrategyContent control, StrategyContainer strategy)
		{
			strategy.BindStrategyToScope(control);

			control.Strategy = strategy;

			if (strategy.SessionType == SessionType.Emulation)
				control.EmulationService = new EmulationService(strategy);

			control.ChildsLoaded += () =>
			{
				//после загрузки всех дочерних контролов можно запустить инициализацию по истории
				if (strategy.Security == null || strategy.Portfolio == null)
					return;

				new ResetStrategyCommand(strategy).Process(control);

				if (strategy.SessionType == SessionType.Battle && strategy.GetIsAutoStart())
					new StartStrategyCommand(strategy).Process(strategy);
			};
		}

		public static void SetStrategy(this OptimizatorContent control, StrategyContainer strategy)
		{
			strategy.BindStrategyToScope(control);

			control.Strategy = strategy;
		}

        private static void BindStrategyToScope(this StrategyContainer strategy, IStudioCommandScope control)
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

            strategy.StrategyRemoved += cmdSvc.UnBind;
            strategy.StrategyAssigned += newStrategy => cmdSvc.Bind(newStrategy, control);

            if (strategy.Strategy != null)
                cmdSvc.Bind(strategy.Strategy, control);
		}

		public static List<ControlType> GetControlTypes(this AppConfig config)
		{
			var types = config.StrategyControls;

			return types
				.Select(type =>
				{
					var iconAttr = type.GetAttribute<IconAttribute>();

					return new ControlType(type,
						type.GetDisplayName(),
						type.GetDescription(),
						iconAttr == null ? null : iconAttr.GetResourceUrl(type));
				})
				.ToList();
		}

		public static Tuple<StrategyInfo, StrategyInfoTypes> GetKey(this StrategyInfo info)
		{
			return Tuple.Create(info, info.Type);
		}

		public static object GetKey(this IStudioControl control)
		{
			var infoContent = control as StrategyInfoContent;
			if (infoContent != null)
				return infoContent.StrategyInfo;

			var codeContent = control as StrategyInfoCodeContent;
			if (codeContent != null)
				return codeContent.StrategyInfo.GetKey();

			var diagramContent = control as DiagramPanel;
			if (diagramContent != null)
				return diagramContent.StrategyInfo == null ? (object)diagramContent.Composition : diagramContent.StrategyInfo.GetKey();

			var optimizatorContent = control as OptimizatorContent;
			if (optimizatorContent != null)
				return optimizatorContent.Strategy;

			var strategyContent = control as StrategyContent;
			if (strategyContent != null)
				return strategyContent.Strategy;

			return null;
		}

		public static bool IsExpired(this IEnumerable<License> licenses)
		{
			return licenses.IsEmpty() || licenses.All(license => license.GetEstimatedTime() < LicenseHelper.RenewOffset);
		}

		public static void AddStockSharpFixConnection(this StudioConnector connector, string serverAddress = "localhost:5001")
		{
			if (connector.BasketSessionHolder.InnerSessions.Count > 1)
				return;

			var client = ConfigManager.GetService<AuthenticationClient>();

			var login = client.Credentials.Login;
			var pass = client.Credentials.Password;

			var fixSessionholder = new FixSessionHolder(connector.TransactionIdGenerator)
			{
				MarketDataSession =
				{
					Login = login,
					Password = pass,
					Address = serverAddress.To<EndPoint>(),
					TargetCompId = "StockSharpMD",
					SenderCompId = login,
					MarketData = FixMarketData.MarketData,
					ExchangeBoard = ExchangeBoard.Forts,
					Version = FixVersions.Fix44
				},
				TransactionSession =
				{
					Login = login,
					Password = pass,
					Address = serverAddress.To<EndPoint>(),
					TargetCompId = "StockSharpTS",
					SenderCompId = login,
					MarketData = FixMarketData.None,
					ExchangeBoard = ExchangeBoard.Forts,
					Version = FixVersions.Fix44,
					RequestAllPortfolios = true
				},
				IsMarketDataEnabled = true,
				IsTransactionEnabled = true,
			};

			connector.BasketSessionHolder.InnerSessions.Add(fixSessionholder, 0);
		}

		public static bool IsEmulation(this StrategyContainer container)
		{
			return container.StrategyInfo.Session == null || container.StrategyInfo.Session.Type == SessionType.Emulation;
		}

		public static bool IsAnalytics(this StrategyInfo info)
		{
			return info != null && info.Type == StrategyInfoTypes.Analytics;
		}

		public static bool IsAnalytics(this StrategyContainer container)
		{
			return container != null && container.StrategyInfo.IsAnalytics();
		}

		public static bool IsStrategy(this StrategyInfo info)
		{
			return info != null && (info.Type == StrategyInfoTypes.Assembly || info.Type == StrategyInfoTypes.Diagram || info.Type == StrategyInfoTypes.SourceCode);
		}

		public static bool IsStrategy(this StrategyContainer container)
		{
			return container != null && container.StrategyInfo.IsStrategy();
		}

		public static bool IsTerminal(this StrategyInfo info)
		{
			return info != null && info.Type == StrategyInfoTypes.Terminal;
		}

		public static bool IsTerminal(this StrategyContainer container)
		{
			return container != null && container.StrategyInfo.IsTerminal();
		}

		public static string CheckCanStart(this StrategyContainer strategy, bool throwError = true)
		{
			string error = null;

			if (strategy.Security == null)
				error = LocalizedStrings.Str3613Params.Put(strategy.Name);

			if (strategy.Portfolio == null)
				error = LocalizedStrings.Str3614Params.Put(strategy.Name);

			var diagramStrategy = strategy.Strategy as DiagramStrategy;
			if (diagramStrategy != null)
			{
				if (diagramStrategy.Composition == null)
					error = LocalizedStrings.Str3615Params.Put(strategy.Name);
				else if (diagramStrategy.Composition.HasErrors)
					error = LocalizedStrings.Str3616Params.Put(strategy.Name);
			}

			if (throwError && error != null)
				throw new InvalidOperationException(error);

			return error;
		}

		public static void SetVisibility<T>(this Group group, IStudioControl ctrl)
		{
			group.Visibility = ctrl is T ? Visibility.Visible : Visibility.Collapsed;
		}

		public static IStorageRegistry GetExecutionStorage(this SessionStrategy sessionStrategy)
		{
			var path = Path.Combine(UserConfig.Instance.ExecutionStoragePath, "{0}_{1}".Put(sessionStrategy.Session.Type, sessionStrategy.Session.RowId), sessionStrategy.StrategyId);
			
			return new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(path) };
		}

		public static void OpenFile(this string file)
		{
			if (file.IsEmpty())
				throw new ArgumentNullException("file");

			try
			{
				Process.Start(file);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}
	}
}