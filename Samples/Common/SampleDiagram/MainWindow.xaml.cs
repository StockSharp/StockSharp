namespace SampleDiagram
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Xaml.Diagram;

	public partial class MainWindow
	{
		public static RoutedCommand AddCommand = new RoutedCommand();
		public static RoutedCommand RemoveCommand = new RoutedCommand();
		public static RoutedCommand SaveCommand = new RoutedCommand();
		public static RoutedCommand DiscardCommand = new RoutedCommand();

		private readonly StrategiesRegistry _strategiesRegistry = new StrategiesRegistry();

		private bool _isCompositionSelected;

		public MainWindow()
		{
			InitializeComponent();

			DiagramEditorControl.PaletteElements = _strategiesRegistry.DiagramElements;
			StrategiesControl.Elements = _strategiesRegistry.Strategies;
			ElementsControl.Elements = _strategiesRegistry.Compositions;
			EmulationControl.StrategiesRegistry = _strategiesRegistry;
		}

		private void StrategiesControl_OnAdded(DiagramElementsControl ctrl, CompositionDiagramElement element)
		{
			_strategiesRegistry.Save(element, false);
			SelectElement(element, false);
		}

		private void StrategiesControl_OnRemoved(DiagramElementsControl ctrl, CompositionDiagramElement element)
		{
			_strategiesRegistry.Remove(element, false);
			SelectElement(null, false);
		}

		private void StrategiesControl_OnSelected(DiagramElementsControl ctrl, CompositionDiagramElement element)
		{
			CheckIsStrategyChanged();
			SelectElement(element, false);
		}

		private void ElementsControl_OnAdded(DiagramElementsControl ctrl, CompositionDiagramElement element)
		{
			_strategiesRegistry.Save(element, true);
			SelectElement(element, true);
		}

		private void ElementsControl_OnRemoved(DiagramElementsControl ctrl, CompositionDiagramElement element)
		{
			_strategiesRegistry.Remove(element, true);
			SelectElement(null, true);
		}

		private void ElementsControl_OnSelected(DiagramElementsControl ctrl, CompositionDiagramElement element)
		{
			CheckIsStrategyChanged();
			SelectElement(element, true);
		}

		private void SelectElement(CompositionDiagramElement element, bool isCompositionSelected)
		{
			//StrategiesListBox.SelectedItem = element;
			DiagramEditorControl.Composition = element;

			_isCompositionSelected = isCompositionSelected;
		}

		private void CheckIsStrategyChanged()
		{
			if (!DiagramEditorControl.IsChanged)
				return;

			var element = DiagramEditorControl.Composition;

			if (MessageBox.Show("Element {0} was changed. Save?".Put(element.Name), Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				_strategiesRegistry.Save(element, _isCompositionSelected);
			}
			else
				_strategiesRegistry.Discard(element, _isCompositionSelected);
		}

		private void SaveCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter != null && DiagramEditorControl.IsChanged;
		}

		private void SaveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			_strategiesRegistry.Save((CompositionDiagramElement)e.Parameter, _isCompositionSelected);
			DiagramEditorControl.ResetIsChanged();
		}

		private void DiscardCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter != null && DiagramEditorControl.IsChanged;
		}

		private void DiscardCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var element = _strategiesRegistry.Discard((CompositionDiagramElement)e.Parameter, _isCompositionSelected);

			SelectElement(element, _isCompositionSelected);
			DiagramEditorControl.ResetIsChanged();
		}
	}
}
