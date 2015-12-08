namespace SampleDiagram
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using Fluent;

	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Diagram;

	using Xceed.Wpf.AvalonDock;
	using Xceed.Wpf.AvalonDock.Layout;

	public partial class MainWindow
	{
		public static RoutedCommand AddCommand = new RoutedCommand();
		public static RoutedCommand OpenCommand = new RoutedCommand();
		public static RoutedCommand RemoveCommand = new RoutedCommand();
		public static RoutedCommand SaveCommand = new RoutedCommand();
		public static RoutedCommand DiscardCommand = new RoutedCommand();
		public static RoutedCommand EmulateStrategyCommand = new RoutedCommand();
		public static RoutedCommand ExecuteStrategyCommand = new RoutedCommand();

		private readonly Dictionary<object, LayoutDocument> _documents = new Dictionary<object, LayoutDocument>();
		private readonly StrategiesRegistry _strategiesRegistry = new StrategiesRegistry();

		private readonly LogManager _logManager;

		private IEnumerable<LayoutDocumentPane> Tabs => DockingManager.Layout.Descendents().OfType<LayoutDocumentPane>().ToArray();

		public MainWindow()
		{
			InitializeComponent();

			_logManager = new LogManager();
			_logManager.Listeners.Add(new FileLogListener("sample.log"));
			_logManager.Listeners.Add(new GuiLogListener(Monitor));

			ConfigManager.RegisterService(_logManager);

			SolutionExplorer.Compositions = _strategiesRegistry.Compositions;
			SolutionExplorer.Strategies = _strategiesRegistry.Strategies;

			DesignerRibbonGroup.Visibility = Visibility.Collapsed;
			EmulationRibbonGroup.Visibility = Visibility.Collapsed;
		}

		#region Event handlers

		private void SolutionExplorer_OnOpen(CompositionItem element)
		{
			OpenComposition(element);
		}

		private void DockingManager_OnActiveContentChanged(object sender, EventArgs e)
		{
			DockingManager
				.ActiveContent
				.DoIfElse<DiagramEditorControl>(editor =>
				{
					RibbonDesignerTab.DataContext = editor.Composition;
					DesignerRibbonGroup.Visibility = Visibility.Visible;
					Ribbon.SelectedTabItem = RibbonDesignerTab;
					//CompositionNameTextBox.SetBindings(TextBox.TextProperty, editor.Composition.Element, "Name");
				}, () =>
				{
					DesignerRibbonGroup.Visibility = Visibility.Collapsed;
					RibbonDesignerTab.DataContext = null;
					//BindingOperations.ClearBinding(CompositionNameTextBox, TextBox.TextProperty);
				});

			DockingManager
				.ActiveContent
				.DoIfElse<EmulationControl>(editor =>
				{
					RibbonEmulationTab.DataContext = editor;
					EmulationRibbonGroup.Visibility = Visibility.Visible;
					Ribbon.SelectedTabItem = RibbonEmulationTab;
				}, () =>
				{
					EmulationRibbonGroup.Visibility = Visibility.Collapsed;
					RibbonEmulationTab.DataContext = null;
				});

			DockingManager
				.ActiveContent
				.DoIfElse<SolutionExplorerControl>(editor =>
				{
					Ribbon.SelectedTabItem = RibbonCommonTab;
				}, () =>
				{
				});
		}

		private void DockingManager_OnDocumentClosing(object sender, DocumentClosingEventArgs e)
		{
			var content = e.Document.Content;

			content.DoIfElse<DiagramEditorControl>(diagramEditor =>
			{
				var element = diagramEditor.Composition;

				if (diagramEditor.IsChanged)
				{
					if (MessageBox.Show("Element {0} was changed. Save?".Put(element.Element.Name), Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						_strategiesRegistry.Save(element);
					}
					else
						_strategiesRegistry.Discard(element);
				}

				_documents.Remove(element);
			}, () => { });

			content.DoIfElse<EmulationControl>(emulationControl =>
			{
				_documents.Remove(emulationControl.Strategy);
			}, () => { });
		}

		#endregion

		#region Commands

		private void AddCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void AddCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var type = (CompositionType)e.Parameter;

			var element = new CompositionDiagramElement
			{
				Name = "New " + type.ToString().ToLower()
			};

			_strategiesRegistry.Save(element, type == CompositionType.Composition);
		}

		private void OpenCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter is CompositionItem;
		}

		private void OpenCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			OpenComposition((CompositionItem)e.Parameter);
		}

		private void RemoveCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter is CompositionItem;
		}

		private void RemoveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var item = (CompositionItem)e.Parameter;

			if (MessageBox.Show("Remove {0} {1}?".Put(item.Element.Name, item.Type.ToString().ToLower()), "Remove", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			_strategiesRegistry.Remove(item);
		}

		private void SaveCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var diagramEditor = DockingManager.ActiveContent as DiagramEditorControl;
			e.CanExecute = diagramEditor != null && diagramEditor.IsChanged;
		}

		private void SaveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var diagramEditor = (DiagramEditorControl)DockingManager.ActiveContent;
			var item = diagramEditor.Composition;

			_strategiesRegistry.Save(item);

			diagramEditor.ResetIsChanged();
		}

		private void DiscardCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var diagramEditor = DockingManager.ActiveContent as DiagramEditorControl;
			e.CanExecute = diagramEditor != null && diagramEditor.IsChanged;
		}

		private void DiscardCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var diagramEditor = (DiagramEditorControl)DockingManager.ActiveContent;
			var item = diagramEditor.Composition;

			var discardedItem = _strategiesRegistry.Discard(item);

			diagramEditor.Composition = discardedItem;
			diagramEditor.ResetIsChanged();
		}

		private void EmulateStrategyCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var item = e.Parameter as CompositionItem;
			e.CanExecute = item != null && item.Type == CompositionType.Strategy;
		}

		private void EmulateStrategyCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			OpenEmulation((CompositionItem)e.Parameter);
		}

		private void ExecuteStrategyCommand_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
		}

		private void ExecuteStrategyCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
		}

		#endregion

		private void OpenComposition(CompositionItem item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			var document = _documents.TryGetValue(item);

			if (document == null)
			{
				document = new LayoutDocument
				{
					Title = item.Element.Name,
					Content = new DiagramEditorControl
					{
						PaletteElements = _strategiesRegistry.DiagramElements,
						Composition = item
					}
				};

				_documents.Add(item, document);
				Tabs.First().Children.Add(document);
			}

			DockingManager.ActiveContent = document.Content;
		}

		private void OpenEmulation(CompositionItem item)
		{
			var strategy = new EmulationDiagramStrategy
			{
				Composition = _strategiesRegistry.Clone(item.Element)
			};

			var document = new LayoutDocument
			{
				Title = "Emulation " + item.Element.Name,
				Content = new EmulationControl
				{
					Strategy = strategy
				}
			};

			_documents.Add(strategy, document);
			Tabs.First().Children.Add(document);

			DockingManager.ActiveContent = document.Content;
		}
	}
}
