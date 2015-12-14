#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: CompositionDiagramElementGroup.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Interop;
	using Ecng.Xaml;

	using StockSharp.Logging;
	using StockSharp.Studio.Controls;
	using StockSharp.Studio.Controls.Commands;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Diagram;

	public partial class CompositionDiagramElementGroup
	{
		public static RoutedCommand AddCompositionCommand = new RoutedCommand();
		public static RoutedCommand CombineToCompositionCommand = new RoutedCommand();
		public static RoutedCommand EditCompositionCommand = new RoutedCommand();
		public static RoutedCommand RemoveCompositionCommand = new RoutedCommand();
		public static RoutedCommand ShareCommand = new RoutedCommand();
		public static RoutedCommand ExportCommand = new RoutedCommand();

		private DiagramPanel _diagramPanel;

		public DiagramPanel DiagramPanel
		{
			get { return _diagramPanel; }
			set
			{
				if (_diagramPanel == value)
					return;

				CompositionName.Text = string.Empty;
				CompositionCategory.Text = string.Empty;

				_diagramPanel = value;

				if (_diagramPanel == null)
					return;

				CompositionName.SetBindings(TextBox.TextProperty, GetParameter<string>(_diagramPanel.Composition, "Name"), "Value");
				CompositionCategory.SetBindings(TextBox.TextProperty, GetParameter<string>(_diagramPanel.Composition, "Category"), "Value");

				CompositionName.IsEnabled = CompositionCategory.IsEnabled = _diagramPanel.StrategyInfo == null;
			}
		}

		public CompositionDiagramElementGroup()
		{
			InitializeComponent();
		}

		private void ExecutedAddCompositionCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenCompositionCommand(new CompositionDiagramElement()).Process(this, true);
		}

		private void CanExecuteAddCompositionCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedCombineToCompositionCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenCompositionCommand(DiagramPanel.GetSelectionCopyElement()).Process(this, true);
		}

		private void CanExecuteCombineToCompositionCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DiagramPanel != null && DiagramPanel.SelectedElements.Count() > 1;
		}

		private void ExecutedEditCompositionCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenCompositionCommand((CompositionDiagramElement)DiagramPanel.PaletteElement).Process(this, true);
		}

		private void CanExecuteEditCompositionCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DiagramPanel != null && DiagramPanel.PaletteElement is CompositionDiagramElement;
		}

		private void ExecutedRemoveCompositionCommand(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				ConfigManager
					.GetService<CompositionRegistry>()
					.TryRemove((CompositionDiagramElement)DiagramPanel.PaletteElement);
			}
			catch (Exception exception)
			{
				exception.LogError();

				new MessageBoxBuilder()
					.Owner(this)
					.Error()
					.Text(exception.Message)
					.Show();
			}
		}

		private void CanExecuteRemoveCompositionCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DiagramPanel != null && DiagramPanel.PaletteElement is CompositionDiagramElement;
		}

		//private void ExecutedSaveComposition(object sender, ExecutedRoutedEventArgs e)
		//{
		//	var composition = (CompositionDiagramElement)DiagramPanel.PaletteElement;

		//	composition.Name = CompositionName.Text;
		//	composition.Parameters.First(p => p.Name.CompareIgnoreCase("Category")).Value = CompositionCategory.Text;

		//	ConfigManager
		//		.GetService<CompositionRegistry>()
		//		.Save(composition);
		//}

		//private void CanExecuteSaveComposition(object sender, CanExecuteRoutedEventArgs e)
		//{
		//	e.CanExecute = DiagramPanel != null && DiagramPanel.PaletteElement != null && DiagramPanel.PaletteElement is CompositionDiagramElement &&
		//				   (!CompositionName.Text.IsEmpty() && CompositionName.Text != DiagramPanel.PaletteElement.Name ||
		//				   !CompositionCategory.Text.IsEmpty() && CompositionCategory.Text != DiagramPanel.PaletteElement.Category);
		//}

		private static DiagramElementParam<T> GetParameter<T>(DiagramElement element, string name)
		{
			return (DiagramElementParam<T>)element.Parameters.First(p => p.Name.CompareIgnoreCase(name));
		}

		private void ExecutedShareCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var fileName = Path.Combine(Path.GetTempPath(), "Diagram_{0:yyyyMMdd_HHmmssfff}.png".Put(DateTime.Now));

			DiagramPanel.SaveToImage(fileName);

			var link = YandexDisk.Publish(fileName, this.GetWindow());

			if (link.IsEmpty())
				return;

			Clipboard.SetText(link);
			link.To<Uri>().OpenLinkInBrowser();
		}

		private void CanExecuteShareCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = DiagramPanel != null;
		}

		private void ExecutedExportCommand(object sender, ExecutedRoutedEventArgs e)
		{
			new ExportCompositionWindow(DiagramPanel.Composition).ShowModal(this);
		}

		private void CanExecuteExportCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
	}
}
