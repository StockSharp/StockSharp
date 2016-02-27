#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: DiagramDebuggerPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Diagram;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3230Key)]
	[DescriptionLoc(LocalizedStrings.Str3231Key)]
	[Icon("images/bug_24x24.png")]
	public partial class DiagramDebuggerPanel
	{
		public static readonly RoutedCommand AddBreakpointCommand = new RoutedCommand();
		public static readonly RoutedCommand RemoveBreakpointCommand = new RoutedCommand();
		public static readonly RoutedCommand StepToOutParamCommand = new RoutedCommand();
		public static readonly RoutedCommand StepNextCommand = new RoutedCommand();
		public static readonly RoutedCommand StepIntoCommand = new RoutedCommand();
		public static readonly RoutedCommand StepOverCommand = new RoutedCommand();
		public static readonly RoutedCommand ContinueCommand = new RoutedCommand();

		private SettingsStorage _debuggerSettings = new SettingsStorage();
		private DiagramDebugger _debugger;
        private StrategyContainer _strategy;
	    private DiagramStrategy _diagramStrategy;

		public StrategyContainer Strategy
		{
			get { return _strategy; }
			set
			{
				if (value == _strategy)
					return;

				if (_diagramStrategy != null)
					_diagramStrategy.CompositionChanged -= OnCompositionChanged;

				if (value != null && !value.IsDiagramStrategy())
				{
					_strategy = null;
					
					WatermarkTextBlock.Text = LocalizedStrings.Str3232;
					WatermarkTextBlock.Visibility = Visibility.Visible;
					DiagramEditor.Visibility = Visibility.Collapsed;
				}
				else
				{
					WatermarkTextBlock.Visibility = Visibility.Collapsed;
					DiagramEditor.Visibility = Visibility.Visible;

					_strategy = value;

					if (_strategy == null)
						return;

					_diagramStrategy = (DiagramStrategy)_strategy.Strategy;
					_diagramStrategy.CompositionChanged += OnCompositionChanged;
					OnCompositionChanged(_diagramStrategy.Composition);
				}
				
			}
		}

		public DiagramDebugger Debugger
		{
			get { return _debugger; }
			private set
			{
				if (_debugger == value)
					return;

				if (_debugger != null)
				{
					_debugger.CompositionChanged += OnDebuggerCompositionChanged;
					_debugger.Break += OnDebuggerBreak;
				}

				_debugger = value;

				if (_debugger == null)
					return;

				_debugger.CompositionChanged += OnDebuggerCompositionChanged;
				_debugger.Break += OnDebuggerBreak;
			}
		}

		public DiagramDebuggerPanel()
		{
			InitializeComponent();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			//cmdSvc.Register<ResetedCommand>(this, true, cmd => OnReseted());
			cmdSvc.Register<BindStrategyCommand>(this, false, cmd =>
			{
				if (!cmd.CheckControl(this))
					return;

				Strategy = cmd.Source;
			});
			cmdSvc.Register<StartStrategyCommand>(this, true, cmd =>
			{
				Debugger.IsEnabled = true;
			});
			cmdSvc.Register<StopStrategyCommand>(this, true, cmd =>
			{
				Debugger.IsEnabled = false;
				Debugger.Continue();
			});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		private void OnDebuggerBreak(DiagramSocket socket)
		{
			GuiDispatcher.GlobalDispatcher.AddAction(() => ShowElementProperties(socket.Parent));
		}

		private void OnDebuggerCompositionChanged(CompositionDiagramElement composition)
		{
			GuiDispatcher.GlobalDispatcher.AddAction(() => DiagramEditor.Composition = composition);
		}

		private void OnCompositionChanged(CompositionDiagramElement composition)
		{
			composition.IsModifiable = false;

			if (Debugger != null)
				_debuggerSettings = Debugger.Save();

			Debugger = new DiagramDebugger(composition);
			SafeLoadDebuggerSettings();

			DiagramEditor.Composition = composition;
		}

		private void SafeLoadDebuggerSettings()
		{
			if (Debugger == null || _debuggerSettings == null)
				return;

			Debugger.Load(_debuggerSettings);
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		#region IStudioControl

		//TODO: дописать логику загрузки состояния для DockingManager
		public override void Load(SettingsStorage storage)
		{
			_debuggerSettings = storage.GetValue<SettingsStorage>("DebuggerSettings");
			SafeLoadDebuggerSettings();

			var layout = storage.GetValue<string>("Layout");

			//if (layout != null)
			//	DockingManager.LoadLayout(layout, true);
		}

		//TODO: дописать логику сохранения состояния для DockingManager
		public override void Save(SettingsStorage storage)
		{
			if (Debugger != null)
				storage.SetValue("DebuggerSettings", Debugger.Save());

			//storage.SetValue("Layout", DockingManager.SaveLayout(true));
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			//cmdSvc.UnRegister<ResetedCommand>(this);
			cmdSvc.UnRegister<BindStrategyCommand>(this);
		}

		#endregion

		private void ExecutedAddBreakpointCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Debugger.AddBreak(DiagramEditor.SelectedElement.SelectedSocket);
			RaiseChangedCommand();
		}

		private void CanExecuteAddBreakpointCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DiagramEditor?.SelectedElement != null && DebuggerSafeCheck(d => !d.IsBreak(DiagramEditor.SelectedElement.SelectedSocket));
		}

		private void ExecutedRemoveBreakpointCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Debugger.RemoveBreak(DiagramEditor.SelectedElement.SelectedSocket);
			RaiseChangedCommand();
		}

		private void CanExecuteRemoveBreakpointCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DiagramEditor?.SelectedElement != null && DebuggerSafeCheck(d => d.IsBreak(DiagramEditor.SelectedElement.SelectedSocket));
		}

		private void ExecutedStepNextCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Debugger.StepNext();
		}

		private void CanExecuteStepNextCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DebuggerSafeCheck(d => d.IsWaiting);
		}

		private void ExecutedStepToOutParamCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Debugger.StepOut();
		}

		private void CanExecuteStepToOutParamCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DebuggerSafeCheck(d => d.IsWaitingOnInput);
		}

		private void ExecutedStepIntoCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Debugger.StepInto();
		}

		private void CanExecuteStepIntoCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DebuggerSafeCheck(d => d.IsWaitingOnInput && d.CanStepInto);
		}

		private void ExecutedStepOverCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Debugger.StepOut();
		}

		private void CanExecuteStepOverCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DebuggerSafeCheck(d => d.CanStepOut);
		}

		private void ExecutedContinueCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Debugger.Continue();
		}

		private void CanExecuteContinueCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DebuggerSafeCheck(d => d.IsWaiting);
		}

		private bool DebuggerSafeCheck(Func<DiagramDebugger, bool> check)
		{
			return Debugger != null && check(Debugger);
		}

		private void DiagramEditor_OnSelectionChanged(DiagramElement element)
		{
			ShowElementProperties(element);
		}

		private void ShowElementProperties(DiagramElement element)
		{
			if (element != null)
			{
				if (PropertyGrid.SelectedObject == element)
					PropertyGrid.SelectedObject = null;

				PropertyGrid.SelectedObject = new DiagramElementParameters(element);
				PropertyGrid.IsReadOnly = true;
			}
			else
			{
				//TODO
				PropertyGrid.SelectedObject = null;
			}
		}
	}
}
