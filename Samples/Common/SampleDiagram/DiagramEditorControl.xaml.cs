#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: DiagramEditorControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleDiagram
{
	using System;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Serialization;

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

		public override object Key => Composition.Element.TypeId;

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

			PaletteElements = ConfigManager.GetService<StrategiesRegistry>().DiagramElements;
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

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			var compositionType = storage.GetValue<CompositionType>("CompositionType");
			var compositionId = storage.GetValue<Guid>("CompositionId");

			var registry = ConfigManager.GetService<StrategiesRegistry>();

			var composition = compositionType == CompositionType.Composition
				                  ? registry.Compositions.FirstOrDefault(c => c.TypeId == compositionId)
				                  : registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			Composition = new CompositionItem(compositionType, (CompositionDiagramElement)composition);
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (Composition == null)
				return;

			storage.SetValue("CompositionType", Composition.Type);
			storage.SetValue("CompositionId", Composition.Element.TypeId);
		}

		#endregion
	}
}
