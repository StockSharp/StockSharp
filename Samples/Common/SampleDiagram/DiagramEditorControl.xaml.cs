namespace SampleDiagram
{
	using System.Windows;

	using Ecng.Collections;

	using StockSharp.Xaml.Diagram;

	public partial class DiagramEditorControl
	{
		public static readonly DependencyProperty CompositionProperty = DependencyProperty.Register("Composition", typeof (CompositionItem), typeof (DiagramEditorControl), 
			new PropertyMetadata(null, CompositionPropertyChanged));

		private static void CompositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var oldComposition = (CompositionItem)args.OldValue;
			var newComposition = (CompositionItem)args.NewValue;

			((DiagramEditorControl)sender).CompositionPropertyChanged(oldComposition, newComposition);
		}

		public CompositionItem Composition
		{
			get { return (CompositionItem)GetValue(CompositionProperty); }
			set { SetValue(CompositionProperty, value); }
		}

		public static readonly DependencyProperty IsChangedProperty = DependencyProperty.Register("IsChanged", typeof(bool), typeof(DiagramEditorControl),
			new PropertyMetadata(false));

		public bool IsChanged
		{
			get { return (bool)GetValue(IsChangedProperty); }
			set { SetValue(IsChangedProperty, value); }
		}

		public INotifyList<DiagramElement> PaletteElements
		{
			get { return PaletteControl.PaletteElements; }
			set { PaletteControl.PaletteElements = value; }
		}

		public DiagramEditorControl()
		{
			InitializeComponent();
		}

		public void ResetIsChanged()
		{
			IsChanged = false;
		}

		private void DiagramEditor_OnSelectionChanged(DiagramElement element)
		{
			PropertyGridControl.SelectedObject = element;
		}

		private void CompositionPropertyChanged(CompositionItem oldComposition, CompositionItem newComposition)
		{
			if (oldComposition != null)
				oldComposition.Element.Changed -= CompositionChanged;

			if (newComposition != null)
			{
				NoStrategyLabel.Visibility = Visibility.Hidden;
				DiagramEditor.Composition = newComposition.Element;

				newComposition.Element.Changed += CompositionChanged;
			}
			else
			{
				NoStrategyLabel.Visibility = Visibility.Visible;
				DiagramEditor.Composition = new CompositionDiagramElement { Name = string.Empty };
			}

			IsChanged = false;
		}

		private void CompositionChanged()
		{
			//TODO add CompositionDiagramElement.IsChanged
			IsChanged = true;
		}
	}
}
