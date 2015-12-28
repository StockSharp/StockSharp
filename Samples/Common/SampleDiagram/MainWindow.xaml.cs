#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleDiagram
{
	using System;
	using System.ComponentModel;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using SampleDiagram.Layout;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Diagram;

	public partial class MainWindow
	{
		public static RoutedCommand AddCommand = new RoutedCommand();
		public static RoutedCommand OpenCommand = new RoutedCommand();
		public static RoutedCommand RemoveCommand = new RoutedCommand();
		public static RoutedCommand SaveCommand = new RoutedCommand();
		public static RoutedCommand DiscardCommand = new RoutedCommand();
		public static RoutedCommand EmulateStrategyCommand = new RoutedCommand();
		public static RoutedCommand ExecuteStrategyCommand = new RoutedCommand();

		private readonly string _settingsFile = "settings.xml";

		private readonly StrategiesRegistry _strategiesRegistry = new StrategiesRegistry();
		private readonly LayoutManager _layoutManager;

		public MainWindow()
		{
			InitializeComponent();

			var logManager = new LogManager();
			logManager.Listeners.Add(new FileLogListener("sample.log"));
			logManager.Listeners.Add(new GuiLogListener(Monitor));

			logManager.Sources.Add(_strategiesRegistry);
			_strategiesRegistry.Init();

			_layoutManager = new LayoutManager(DockingManager);
			_layoutManager.Changed += SaveSettings;
			logManager.Sources.Add(_layoutManager);

			ConfigManager.RegisterService(logManager);
			ConfigManager.RegisterService(_strategiesRegistry);
			ConfigManager.RegisterService(_layoutManager);

			SolutionExplorer.Compositions = _strategiesRegistry.Compositions;
			SolutionExplorer.Strategies = _strategiesRegistry.Strategies;

			//DesignerRibbonGroup.Visibility = Visibility.Collapsed;
			//EmulationRibbonGroup.Visibility = Visibility.Collapsed;
		}

		#region Event handlers

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			LoadSettings();
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			SaveSettings();
		}

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
					//DesignerRibbonGroup.Visibility = Visibility.Visible;
					Ribbon.SelectedTabItem = RibbonDesignerTab;
					//CompositionNameTextBox.SetBindings(TextBox.TextProperty, editor.Composition.Element, "Name");
				}, () =>
				{
					//DesignerRibbonGroup.Visibility = Visibility.Collapsed;
					RibbonDesignerTab.DataContext = null;
					//BindingOperations.ClearBinding(CompositionNameTextBox, TextBox.TextProperty);
				});

			DockingManager
				.ActiveContent
				.DoIfElse<EmulationControl>(editor =>
				{
					RibbonEmulationTab.DataContext = editor;
					//EmulationRibbonGroup.Visibility = Visibility.Visible;
					Ribbon.SelectedTabItem = RibbonEmulationTab;
				}, () =>
				{
					//EmulationRibbonGroup.Visibility = Visibility.Collapsed;
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
			var item = new CompositionItem(type, element);

			_strategiesRegistry.Save(item);

			OpenComposition(item);
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

			var res = new MessageBoxBuilder()
				.Owner(this)
				.Caption(Title)
				.Text(LocalizedStrings.Str2884Params.Put(item.Element.Name))
				.Button(MessageBoxButton.YesNo)
				.Icon(MessageBoxImage.Question)
				.Show();

			if (res != MessageBoxResult.Yes)
				return;

			var control = _layoutManager
				.DockingControls
				.OfType<DiagramEditorControl>()
				.FirstOrDefault(c => c.Key.CompareIgnoreCase(item.Key));

			if (control != null)
			{
				control.ResetIsChanged();
				_layoutManager.CloseDocumentWindow(control);
			}

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

			var content = new DiagramEditorControl
			{
				Composition = item
			};

            _layoutManager.OpenDocumentWindow(content);
		}

		private void OpenEmulation(CompositionItem item)
		{
			var strategy = new EmulationDiagramStrategy
			{
				Composition = _strategiesRegistry.Clone(item.Element)
			};

			var content = new EmulationControl
			{
				Strategy = strategy
			};

			_layoutManager.OpenDocumentWindow(content);
		}

		private void LoadSettings()
		{
			if (!File.Exists(_settingsFile))
				return;

			var settings = CultureInfo
				.InvariantCulture
				.DoInCulture(() => new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));

			_layoutManager.Load(settings);
		}

		private void SaveSettings()
		{
			CultureInfo
				.InvariantCulture
				.DoInCulture(() => new XmlSerializer<SettingsStorage>().Serialize(_layoutManager.Save(), _settingsFile));
		}
	}
}
