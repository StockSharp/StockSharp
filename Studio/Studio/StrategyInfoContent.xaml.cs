#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: StrategyInfoContent.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;

	using ActiproSoftware.Windows.Controls.Docking;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Studio.Controls;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	public partial class StrategyInfoContent : IStudioControl, IStudioCommandScope
	{
		private readonly PairSet<Tuple<string, Type>, IContentWindow> _contents = new PairSet<Tuple<string, Type>, IContentWindow>();
		private StrategyInfo _strategyInfo;
		private bool _suspendChangedEvent;

		public static readonly DependencyProperty SelectedPaneProperty = DependencyProperty.Register("SelectedPane", typeof(IContentWindow), typeof(StrategyInfoContent));

		public IContentWindow SelectedPane
		{
			get { return (IContentWindow)GetValue(SelectedPaneProperty); }
			set { SetValue(SelectedPaneProperty, value); }
		}

		public StrategyInfo StrategyInfo
		{
			get { return _strategyInfo; }
			set
			{
				if (_strategyInfo != null)
				{
					_strategyInfo.Strategies.Added -= StrategyAdded;
					_strategyInfo.Strategies.Removed -= StrategyRemoved;
				}

				_strategyInfo = value;

				if (_strategyInfo == null)
					return;

				_strategyInfo.Strategies.Added += StrategyAdded;
				_strategyInfo.Strategies.Removed += StrategyRemoved;
			}
		}

		public StrategyContainer SelectedStrategy
		{
			get
			{
				if (DockSite == null || DockSite.ActiveWindow == null)
					return null;

				var studioPane = (IContentWindow)DockSite.ActiveWindow;

				var strategyContent = studioPane.Control as StrategyContent;

				if (strategyContent == null)
					return null;

				return strategyContent.Strategy;
			}
		}

		public StrategyInfoContent()
		{
			InitializeComponent();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<OpenStrategyCommand>(this, true, cmd => OpenControl(cmd.Strategy, cmd.ContentTemplate));
			cmdSvc.Register<OpenStrategyInfoCommand>(this, true, cmd => OpenControl(cmd.Info));
			cmdSvc.Register<OpenWindowCommand>(this, true, cmd => OpenControl(cmd.Id, cmd.CtrlType, cmd.Context, () => cmd.CtrlType.CreateInstance<IStudioControl>()));
			cmdSvc.Register<CloseWindowCommand>(this, true, cmd => CloseWindow(cmd.Id, cmd.CtrlType));

			BindingOperations.SetBinding(this, SelectedPaneProperty, new Binding("ActiveWindow")
			{
				Source = DockSite,
				Mode = BindingMode.OneWay,
			});
		}

		public void OpenDefaultPanes()
		{
			OpenControl(StrategyInfo);
			StrategyInfo.Strategies.ForEach(s => OpenControl(s));
		}

		private void StrategyAdded(StrategyContainer strategy)
		{
			OpenControl(strategy);
		}

		private void StrategyRemoved(StrategyContainer strategy)
		{
			CloseWindow(strategy.Strategy.Id.To<string>(), strategy.SessionType == SessionType.Optimization ? typeof(OptimizatorContent) : typeof(StrategyContent));
		}

		#region IStudioControl

		private void RemoveControls()
		{
			_contents
				.Keys
				.ToArray()
				.ForEach(t => CloseWindow(t.Item1, t.Item2));
		}

		public void Load(SettingsStorage storage)
		{
			_suspendChangedEvent = true;

			RemoveControls();
			storage.LoadUISettings(DockSite, _contents);

			_suspendChangedEvent = false;
		}

		public void Save(SettingsStorage storage)
		{
			storage.SaveUISettings(DockSite, _contents);
		}

		void IDisposable.Dispose()
		{
			RemoveControls();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<OpenStrategyCommand>(this);
			cmdSvc.UnRegister<OpenStrategyInfoCommand>(this);
			cmdSvc.UnRegister<OpenWindowCommand>(this);
			cmdSvc.UnRegister<CloseWindowCommand>(this);

			StrategyInfo = null;
		}

		string IStudioControl.Title
		{
			get { return StrategyInfo == null ? string.Empty : StrategyInfo.Name; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}

		#endregion

		private IStudioControl OpenControl(StrategyContainer strategy, string contentTemplate = null)
		{
			switch (strategy.SessionType)
			{
				case SessionType.Battle:
				case SessionType.Emulation:
					return OpenStrategyControl(strategy, contentTemplate);

				case SessionType.Optimization:
					return OpenOprimizationControl(strategy);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private IStudioControl OpenStrategyControl(StrategyContainer strategy, string contentTemplate)
		{
			return OpenControl(strategy.Strategy.Id.To<string>(), typeof(StrategyContent), strategy, () =>
			{
				var ctrl = new StrategyContent();

				ctrl.SetStrategy(strategy);
				ctrl.LoadTemplate(contentTemplate ?? GetDefaultContentTemplate(strategy.StrategyInfo.Type, strategy.SessionType), false);

				return ctrl;
			});
		}

		private static string GetDefaultContentTemplate(StrategyInfoTypes infoType, SessionType sessionType)
		{
			switch (infoType)
			{
				case StrategyInfoTypes.SourceCode:
				case StrategyInfoTypes.Diagram:
				case StrategyInfoTypes.Assembly:
				{
					switch (sessionType)
					{
						case SessionType.Battle:
							return Properties.Resources.DefaultStrategyContent;

						case SessionType.Emulation:
							return Properties.Resources.EmulationStrategyContent;

						case SessionType.Optimization:
							return Properties.Resources.EmulationStrategyContent;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				case StrategyInfoTypes.Analytics:
					return Properties.Resources.DefaultAnalyticsContent;

				case StrategyInfoTypes.Terminal:
					return Properties.Resources.DefaultTerminalContent;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private IStudioControl OpenOprimizationControl(StrategyContainer strategy)
		{
			return OpenControl(strategy.Strategy.Id.To<string>(), typeof(OptimizatorContent), strategy, () =>
			{
				var c = new OptimizatorContent { Strategy = strategy };
				c.SetStrategy(strategy);
				return c;
			});
		}

		private IStudioControl OpenControl(StrategyInfo info)
		{
			IStudioControl ctrl = null;

			switch (StrategyInfo.Type)
			{
				case StrategyInfoTypes.SourceCode:
				case StrategyInfoTypes.Analytics:
					ctrl = OpenControl(info.Id.To<string>(), typeof(StrategyInfoCodeContent), info, () =>
					{
						var c = new StrategyInfoCodeContent { StrategyInfo = info };

						ConfigManager
							.GetService<IStudioCommandService>()
							.Bind(info.GetKey(), c);

						return c;
					});
					break;

				case StrategyInfoTypes.Diagram:
					ctrl = OpenControl(info.Id.To<string>(), typeof(DiagramPanel), info, () =>
					{
						var c = new DiagramPanel { StrategyInfo = info };

						ConfigManager
							.GetService<IStudioCommandService>()
							.Bind(info.GetKey(), c);

						return c;
					});
					break;

				case StrategyInfoTypes.Assembly:
				case StrategyInfoTypes.Terminal:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return ctrl;
		}

		private IStudioControl OpenControl(string id, Type ctrlType, object tag, Func<IStudioControl> getControl)
		{
			if (ctrlType == null)
				throw new ArgumentNullException(nameof(ctrlType));

			if (getControl == null)
				throw new ArgumentNullException(nameof(getControl));

			id = id.ToLowerInvariant();

			var wnd = _contents.SafeAdd(Tuple.Create(id, ctrlType), key =>
			{
				var docWnd = new ContentDocumentWindow
				{
					Tag = tag,
					Control = getControl(),
					Name = "Pane" + id.Replace("-", "") + ctrlType.Name,
					CanClose = !(tag is StrategyInfo)
				};
				DockSite.DocumentWindows.Add(docWnd);

				return docWnd;
			});

			((DockingWindow)wnd).Activate();

			return wnd.Control;
		}

		private void CloseWindow(string id, Type type)
		{
			var key = Tuple.Create(id, type);

			var ctrl = _contents.TryGetValue(key);

			if (ctrl == null)
				return;

			DockSite.DocumentWindows.Remove((DocumentWindow)ctrl);
		}

		private void DisposeControl(IContentWindow window)
		{
			_contents.RemoveByValue(window);

			window.Control.Dispose();

			if (window.Tag == null)
				return;

			var sender = window.Tag;

			var strategyContainer = window.Tag as StrategyContainer;
			if (strategyContainer != null)
				sender = strategyContainer.Strategy;

			var info = window.Tag as StrategyInfo;
			if (info != null)
				sender = window.Control.GetKey();

			ConfigManager
				.GetService<IStudioCommandService>()
				.UnBind(sender);
		}

		private void DockSite_OnWindowClosing(object sender, DockingWindowEventArgs e)
		{
			//окна могут открываться и закрываться в момент загрузки разметки
			if (_suspendChangedEvent)
				return;

			var window = e.Window as ContentDocumentWindow;
			if (window == null)
				return;

			e.Handled = true;

			var strategy = window.Tag as StrategyContainer;
			if (strategy != null && strategy.ProcessState != ProcessStates.Stopped)
			{
				if (strategy.StrategyInfo.Type != StrategyInfoTypes.Terminal)
				{
					e.Cancel = true;

					new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3617Params.Put(window.Title))
						.Warning()
						.Show();

					return;
				}

				new StopStrategyCommand(strategy).SyncProcess(strategy);
			}

			DisposeControl(window);
		}
	}
}
