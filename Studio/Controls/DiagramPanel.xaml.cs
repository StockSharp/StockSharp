namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Studio.Controls.Commands;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Diagram;
	using StockSharp.Localization;
	using StockSharp.Xaml.Actipro;

	public partial class DiagramPanel : IStudioControl, IStudioCommandScope
    {
		private readonly Timer _timer;
		private bool _needToSave;
		private string _title;

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(DiagramPanel), new PropertyMetadata(LocalizedStrings.Str3181, TitleChanged));

		private static void TitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = (DiagramPanel)d;
			var title = (string)e.NewValue;

			ctrl._title = title;
		}

		public string Title
		{
			get { return _title; }
			set { SetValue(TitleProperty, value); }
		}

		private CompositionDiagramElement _composition;

		public CompositionDiagramElement Composition
		{
			get { return _composition; }
			set
			{
				if (_composition == value)
					return;

				if (_composition != null)
					_composition.Changed -= MarkDirty;

				_composition = value;

				Title = GetTitle();

				if (_composition == null)
					return;

				DiagramEditor.Composition = _composition;
				_composition.Changed += MarkDirty;
			}
		}

		public DiagramElement PaletteElement { get { return Palette.PaletteElement; } }

		public IEnumerable<DiagramElement> SelectedElements { get { return DiagramEditor.SelectedElements; } }

		public event Action<DiagramElement> PaletteSelectionChanged;

        public DiagramPanel()
        {
            InitializeComponent();
			
			Palette.PaletteElements = ConfigManager.GetService<CompositionRegistry>().DiagramElements;

			_timer = ThreadingHelper.Timer(() =>
			{
				lock (this)
				{
					if (!_needToSave)
						return;

					GuiDispatcher.GlobalDispatcher.AddAction(Save);
					_needToSave = false;
				}
			}).Interval(TimeSpan.FromSeconds(5));

			if (!DiagramEditor.IndicatorTypes.IsEmpty())
				return;

			DiagramEditor.IndicatorTypes.AddRange(Configuration.Extensions.GetIndicatorTypes());
        }

		private StrategyInfo _strategyInfo;

		public StrategyInfo StrategyInfo
		{
			get { return _strategyInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (value.Type != StrategyInfoTypes.Diagram)
					throw new InvalidOperationException();

				_strategyInfo = value;

				Composition = ConfigManager.GetService<CompositionRegistry>().Deserialize(_strategyInfo.Body.LoadSettingsStorage());
			}
		}

		private void MarkDirty()
		{
			_needToSave = true;

			var title = GetTitle();

			if (!_title.CompareIgnoreCase(title))
				Title = title;
		}

		private string GetTitle()
		{
			return StrategyInfo != null || Composition == null ? LocalizedStrings.Str3181 : LocalizedStrings.Str3181 + " - " + Composition.Name;
		}

		private void Save()
		{
			if (_strategyInfo != null)
			{
				var storage = ConfigManager
					.GetService<CompositionRegistry>()
					.Serialize(Composition);

				_strategyInfo.Body = storage.SaveSettingsStorage();
			}
			else
			{
				ConfigManager
					.GetService<CompositionRegistry>()
					.Save(_composition);
			}
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			var diagramEditor = storage.GetValue<SettingsStorage>("DiagramEditor");
			if (diagramEditor != null)
				DiagramEditor.Load(diagramEditor);

			var layout = storage.GetValue<string>("Layout");
			if (layout != null)
				DockSite.LoadLayout(layout, true);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("DiagramEditor", DiagramEditor.Save());
			storage.SetValue("Layout", DockSite.SaveLayout(true));
		}

		void IDisposable.Dispose()
		{
			Palette.PaletteElements = null;

			if (_timer != null)
				_timer.Dispose();

			if (_needToSave)
				Save();
		}

		string IStudioControl.Title
		{
			get { return LocalizedStrings.Str3181; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}

		public CompositionDiagramElement GetSelectionCopyElement()
		{
			return DiagramEditor.GetSelectionCopyElement();
		}

		public void SaveToImage(string fileName)
		{
			DiagramEditor.SaveToImage(fileName);
		}

		private void DiagramEditor_OnElementDoubleClicked(DiagramElement element)
		{
			var composition = element as CompositionDiagramElement;

			if (composition == null)
				return;

			var originalComposition = (CompositionDiagramElement)ConfigManager
				.GetService<CompositionRegistry>()
				.DiagramElements
				.First(e => e.TypeId == composition.TypeId);

			new OpenCompositionCommand(originalComposition).Process(this);
		}

		private void Palette_OnElementDoubleClicked(DiagramElement element)
		{
			var composition = element as CompositionDiagramElement;

			if (composition != null)
				new OpenCompositionCommand(composition).Process(this, true);
		}

		private void DiagramEditor_OnPaletteSelectionChanged(DiagramElement element)
		{
			PaletteSelectionChanged.SafeInvoke(element);
		}

		private void DiagramEditor_OnSelectionChanged(DiagramElement element)
		{
			PropertyGrid.SelectedObject = element;
		}
    }
}
