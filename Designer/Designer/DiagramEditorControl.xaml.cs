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
namespace StockSharp.Designer
{
	using System;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Designer.Layout;
	using StockSharp.Localization;
	using StockSharp.Xaml.Diagram;

	public partial class DiagramEditorControl
	{
		private readonly LayoutManager _layoutManager;

		#region Composition property

		public static readonly DependencyProperty CompositionProperty = DependencyProperty.Register("Composition", typeof(CompositionItem), typeof(DiagramEditorControl),
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

		#endregion

		public override string Key => Composition.Key;

		public bool IsChanged { get; set; }

		public INotifyList<DiagramElement> PaletteElements
		{
			get { return PaletteControl.PaletteElements; }
			set { PaletteControl.PaletteElements = value; }
		}

		public DiagramEditorControl()
		{
			InitializeComponent();

			_layoutManager = new LayoutManager(DockingManager);

			if (DiagramEditor.IndicatorTypes.IsEmpty())
				DiagramEditor.IndicatorTypes.AddRange(Configuration.Extensions.GetIndicatorTypes());

			PaletteElements = ConfigManager.GetService<StrategiesRegistry>().DiagramElements;
		}

		public override bool CanClose()
		{
			if (!IsChanged)
				return true;

			var res = new MessageBoxBuilder()
				.Owner(this)
				.Caption(Title)
				.Text(LocalizedStrings.Str3676Params.Put(Composition.Element.Name))
				.Icon(MessageBoxImage.Question)
				.Button(MessageBoxButton.YesNo)
				.Show();

			if (res == MessageBoxResult.Yes)
				ConfigManager.GetService<StrategiesRegistry>().Save(Composition);
			else
				ConfigManager.GetService<StrategiesRegistry>().Discard(Composition);

			ResetIsChanged();

			return true;
		}

		public void ResetIsChanged()
		{
			IsChanged = false;
		}

		private void DiagramEditor_OnSelectionChanged(DiagramElement element)
		{
			PropertyGridControl.SelectedObject = element;
		}

		private void DiagramEditor_OnElementDoubleClicked(DiagramElement element)
		{
			var compositionElement = element as CompositionDiagramElement;

			if (compositionElement == null)
				return;

			var originalComposition = ConfigManager
				.GetService<StrategiesRegistry>()
				.DiagramElements
				.OfType<CompositionDiagramElement>()
				.First(c => c.TypeId == compositionElement.TypeId);

			ConfigManager
				.GetService<LayoutManager>()
				.OpenDocumentWindow(new DiagramEditorControl
				{
					Composition = new CompositionItem(CompositionType.Composition, originalComposition)
				});
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
			//base.Load(storage);

			var compositionType = storage.GetValue<CompositionType>("CompositionType");
			var compositionId = storage.GetValue<Guid>("CompositionId");

			var registry = ConfigManager.GetService<StrategiesRegistry>();

			var composition = compositionType == CompositionType.Composition
				                  ? registry.Compositions.FirstOrDefault(c => c.TypeId == compositionId)
				                  : registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			Composition = new CompositionItem(compositionType, (CompositionDiagramElement)composition);

			var layout = storage.GetValue<string>("Layout");

			if (!layout.IsEmpty())
				_layoutManager.LoadLayout(layout);

			var diagramEditor = storage.GetValue<SettingsStorage>("DiagramEditor");

			if (diagramEditor != null)
				DiagramEditor.Load(diagramEditor);
		}

		public override void Save(SettingsStorage storage)
		{
			//base.Save(storage);

			if (Composition != null)
			{
				storage.SetValue("CompositionType", Composition.Type);
				storage.SetValue("CompositionId", Composition.Element.TypeId);
			}
			
			storage.SetValue("Layout", _layoutManager.SaveLayout());
			storage.SetValue("DiagramEditor", DiagramEditor.Save());
		}

		#endregion
	}
}
