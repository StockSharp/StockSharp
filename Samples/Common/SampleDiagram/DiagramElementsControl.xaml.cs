namespace SampleDiagram
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;

	using StockSharp.Xaml.Diagram;

	/// <summary>
	/// Interaction logic for DiagramElementsControl.xaml
	/// </summary>
	public partial class DiagramElementsControl
	{
		public static RoutedCommand AddCommand = new RoutedCommand();
		public static RoutedCommand RemoveCommand = new RoutedCommand();

		public IEnumerable<CompositionDiagramElement> Elements
		{
			get { return (IEnumerable<CompositionDiagramElement>)StrategiesListBox.ItemsSource; }
			set { StrategiesListBox.ItemsSource = value; }
		}

		public event Action<DiagramElementsControl, CompositionDiagramElement> Added;

		public event Action<DiagramElementsControl, CompositionDiagramElement> Removed;

		public event Action<DiagramElementsControl, CompositionDiagramElement> Selected;

		public DiagramElementsControl()
		{
			InitializeComponent();
		}

		private void Strategies_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var element = (CompositionDiagramElement)StrategiesListBox.SelectedItem;

			if (element == null)
				return;

			Selected.SafeInvoke(this, element);
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

			Added.SafeInvoke(this, element);
		}

		private void RemoveCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter != null;
		}

		private void RemoveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var element = (CompositionDiagramElement)e.Parameter;

			if (MessageBox.Show("Remove {0} strategy?".Put(element.Name), "Remove", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			Removed.SafeInvoke(this, element);
		}
	}
}
