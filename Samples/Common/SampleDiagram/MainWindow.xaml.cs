namespace SampleDiagramPublic
{
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;

	using StockSharp.Xaml.Diagram;

	using ConfigurationExtensions = StockSharp.Configuration.Extensions;

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public static RoutedCommand AddCommand = new RoutedCommand();
		public static RoutedCommand RemoveCommand = new RoutedCommand();
		public static RoutedCommand SaveCommand = new RoutedCommand();
		public static RoutedCommand DiscardCommand = new RoutedCommand();

		private readonly StrategiesRegistry _strategiesRegistry = new StrategiesRegistry();

		public MainWindow()
		{
			InitializeComponent();

			DiagramEditorControl.PaletteElements = _strategiesRegistry.DiagramElements;
			StrategiesListBox.ItemsSource = _strategiesRegistry.Strategies;
			EmulationControl.StrategiesRegistry = _strategiesRegistry;
		}

		private void Strategies_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var element = (CompositionDiagramElement)StrategiesListBox.SelectedItem;

			if (element == null)
				return;

			CheckIsStrategyChanged();
			SelectStrategy(element);
		}

		private void SelectStrategy(CompositionDiagramElement element)
		{
			StrategiesListBox.SelectedItem = element;
			DiagramEditorControl.Composition = element;
		}

		private void CheckIsStrategyChanged()
		{
			if (!DiagramEditorControl.IsChanged)
				return;

			var element = DiagramEditorControl.Composition;

			if (MessageBox.Show("Strategy {0} was changed. Save?".Put(element.Name), Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				_strategiesRegistry.Save(element);
			}
			else
				_strategiesRegistry.Discard(element);
		}

		private void AddCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void AddCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var element = new CompositionDiagramElement
			{
				Name = "New strategy"
			};

			_strategiesRegistry.Save(element);
			SelectStrategy(element);
		}

		private void RemoveCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter != null;
		}

		private void RemoveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var element = (CompositionDiagramElement)e.Parameter;

			if (MessageBox.Show("Remove {0} strategy?".Put(element.Name), Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			SelectStrategy(null);
			_strategiesRegistry.Remove(element);
		}

		private void SaveCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter != null && DiagramEditorControl.IsChanged;
		}

		private void SaveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			_strategiesRegistry.Save((CompositionDiagramElement)e.Parameter);
			DiagramEditorControl.ResetIsChanged();
		}

		private void DiscardCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter != null && DiagramEditorControl.IsChanged;
		}

		private void DiscardCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var element = _strategiesRegistry.Discard((CompositionDiagramElement)e.Parameter);

			SelectStrategy(element);
			DiagramEditorControl.ResetIsChanged();
		}
	}
}
