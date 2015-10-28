namespace SampleDiagram
{
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Xaml;

	using StockSharp.Xaml.Diagram;

	public partial class DiagramDebuggerControl
	{
		public readonly static RoutedCommand AddBreakpointCommand = new RoutedCommand();
		public readonly static RoutedCommand RemoveBreakpointCommand = new RoutedCommand();
		public readonly static RoutedCommand StepToOutParamCommand = new RoutedCommand();
		public readonly static RoutedCommand StepNextCommand = new RoutedCommand();
		public readonly static RoutedCommand ContinueCommand = new RoutedCommand();

		public static readonly DependencyProperty CompositionProperty = DependencyProperty.Register("Composition", typeof(CompositionDiagramElement), typeof(DiagramDebuggerControl),
			new PropertyMetadata(null, CompositionPropertyChanged));

		private static void CompositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((DiagramDebuggerControl)sender).CompositionPropertyChanged((CompositionDiagramElement)args.NewValue);
		}

		public CompositionDiagramElement Composition
		{
			get { return (CompositionDiagramElement)GetValue(CompositionProperty); }
			set { SetValue(CompositionProperty, value); }
		}

		private DiagramDebugger _debugger;

		public DiagramDebuggerControl()
		{
			InitializeComponent();
		}

		private void DiagramEditor_OnSelectionChanged(DiagramElement element)
		{
			ShowElementProperties(element);
		}

		private void CompositionPropertyChanged(CompositionDiagramElement newComposition)
		{
			if (newComposition != null)
			{
				_debugger = new DiagramDebugger(newComposition);
				_debugger.Break += OnDebuggerBreak;

				NoStrategyLabel.Visibility = Visibility.Hidden;
				DiagramEditor.Composition = newComposition;
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
				CommandManager.InvalidateRequerySuggested();
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
				PropertyGridControl.SelectedObject = null;
		}

		private void AddBreakpointCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _debugger != null && DiagramEditor.SelectedElement != null && !_debugger.IsBreak(DiagramEditor.SelectedElement);
		}

		private void AddBreakpointCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_debugger.AddBreak(DiagramEditor.SelectedElement);
		}

		private void RemoveBreakpointCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _debugger != null && DiagramEditor.SelectedElement != null && _debugger.IsBreak(DiagramEditor.SelectedElement);
		}

		private void RemoveBreakpointCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_debugger.RemoveBreak(DiagramEditor.SelectedElement);
		}

		private void StepNextCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _debugger != null && _debugger.IsWaiting;
		}

		private void StepNextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_debugger.StepNext();
		}

		private void StepToOutParamCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _debugger != null && _debugger.IsWaitingOnInput;
		}

		private void StepToOutParamCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_debugger.StepOut();
		}

		private void ContinueCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _debugger != null && _debugger.IsWaiting;
		}

		private void ContinueCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_debugger.Continue();
		}
	}
}
