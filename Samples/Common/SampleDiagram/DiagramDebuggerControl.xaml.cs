#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: DiagramDebuggerControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleDiagram
{
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Xaml;

	using StockSharp.Xaml.Diagram;

	public partial class DiagramDebuggerControl
	{
		public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register("Strategy", typeof(EmulationDiagramStrategy), typeof(DiagramDebuggerControl),
			new PropertyMetadata(null, StrategyPropertyChanged));

		private static void StrategyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((DiagramDebuggerControl)sender).StrategyPropertyChanged((EmulationDiagramStrategy)args.NewValue);
		}

		public EmulationDiagramStrategy Strategy
		{
			get { return (EmulationDiagramStrategy)GetValue(StrategyProperty); }
			set { SetValue(StrategyProperty, value); }
		}

		private DiagramDebugger _debugger;

		public ICommand AddBreakpointCommand { get; private set; }

		public ICommand RemoveBreakpointCommand { get; private set; }

		public ICommand StepNextCommand { get; private set; }

		public ICommand StepToOutParamCommand { get; private set; }

		public ICommand StepIntoCommand { get; private set; }

		public ICommand StepOutCommand { get; private set; }

		public ICommand ContinueCommand { get; private set; }

		public DiagramDebuggerControl()
		{
			InitializeCommands();
            InitializeComponent();
		}

		private void InitializeCommands()
		{
			AddBreakpointCommand = new DelegateCommand(
				obj => _debugger.AddBreak(DiagramEditor.SelectedElement),
				obj => _debugger != null && DiagramEditor.SelectedElement != null && !_debugger.IsBreak(DiagramEditor.SelectedElement));

			RemoveBreakpointCommand = new DelegateCommand(
				obj => _debugger.RemoveBreak(DiagramEditor.SelectedElement),
				obj => _debugger != null && DiagramEditor.SelectedElement != null && _debugger.IsBreak(DiagramEditor.SelectedElement));

			StepNextCommand = new DelegateCommand(
				obj => _debugger.StepNext(),
				obj => _debugger != null && _debugger.IsWaiting);

			StepToOutParamCommand = new DelegateCommand(
				obj => _debugger.StepOut(),
				obj => _debugger != null && _debugger.IsWaitingOnInput);

			StepIntoCommand = new DelegateCommand(
				obj => _debugger.StepInto(),
				obj => _debugger != null && _debugger.IsWaitingOnInput && _debugger.CanStepInto);

			StepOutCommand = new DelegateCommand(
				obj => _debugger.StepOut(),
				obj => _debugger != null && _debugger.CanStepOut);

			ContinueCommand = new DelegateCommand(
				obj => _debugger.Continue(),
				obj => _debugger != null && _debugger.IsWaiting);
		}

		private void DiagramEditor_OnSelectionChanged(DiagramElement element)
		{
			ShowElementProperties(element);
		}

		private void StrategyPropertyChanged(EmulationDiagramStrategy strategy)
		{
			if (strategy != null)
			{
				var composition = strategy.Composition;

				_debugger = new DiagramDebugger(composition);
				_debugger.Break += OnDebuggerBreak;
				_debugger.CompositionChanged += OnDebuggerCompositionChanged;

				NoStrategyLabel.Visibility = Visibility.Hidden;
				DiagramEditor.Composition = composition;

				ShowElementProperties(null);
			}
			else
			{
				_debugger = null;

				NoStrategyLabel.Visibility = Visibility.Visible;
				DiagramEditor.Composition = new CompositionDiagramElement { Name = string.Empty };
			}

			DiagramEditor.Composition.IsModifiable = false;
		}

		private void OnDebuggerBreak(DiagramElement element)
		{
			this.GuiAsync(() =>
			{
				ShowElementProperties(element);
			});
		}

		private void OnDebuggerCompositionChanged(CompositionDiagramElement element)
		{
			this.GuiAsync(() =>
			{
				DiagramEditor.Composition = element;
			});
		}

		private void ShowElementProperties(DiagramElement element)
		{
			if (element != null)
			{
				if (PropertyGridControl.SelectedObject == element)
					PropertyGridControl.SelectedObject = null;

				PropertyGridControl.SelectedObject = new DiagramElementParameters(element);
				PropertyGridControl.IsReadOnly = true;
			}
			else
			{
				PropertyGridControl.SelectedObject = Strategy;
				PropertyGridControl.IsReadOnly = false;
			}
		}
	}
}
