#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.TerminalPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 3:22 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using System.IO;
using System.Linq;
using System.Windows;

using StockSharp.Terminal.Layout;
using StockSharp.Terminal.Controls;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock;
using System.Windows.Controls;

namespace StockSharp.Terminal
{
	public partial class MainWindow
	{
		public LayoutManager LayoutManager { get; set; }

		private int _countWorkArea = 2;

		public MainWindow()
		{
			InitializeComponent();

			LayoutManager = new LayoutManager(DockingManager);

			//AddDocumentElement.IsSelectedChanged += AddDocumentElement_IsSelectedChanged;
			DockingManager.DocumentClosed += DockingManager_DocumentClosed;
		}

		private void DockingManager_DocumentClosed(object sender, DocumentClosedEventArgs e)
		{
			var manager = (DockingManager)sender;

			if (LayoutDocuments.Children.Count == 0 &&
				manager.FloatingWindows.ToList().Count == 0)
				_countWorkArea = 0;
		}

		//private void DockingManager_DocumentClosing(object sender, Xceed.Wpf.AvalonDock.DocumentClosingEventArgs e)
		//{
		//	var element = e.Document;
		//	var manager = (Xceed.Wpf.AvalonDock.DockingManager)sender;

		//	var item = manager.FloatingWindows.FirstOrDefault(x =>
		//	{
		//		var doc = (LayoutDocumentFloatingWindow)x.Model;
		//		return doc.Children.Contains(element);
		//	});

		//	if (item != null)
		//	{
		//		manager.FloatingWindows.ToList().Remove(item);
		//	}
		//	else
		//		LayoutDocuments.RemoveChild(element);

		//	if (LayoutDocuments.Children.Count == 0)
		//		_countWorkArea = 0;

		//	//if (LayoutDocuments.Children.Count == 2)
		//	//{
		//	//	var item = LayoutDocuments.Children.FirstOrDefault(x => x.Title != "+");
		//	//	item.CanClose = false;
		//	//	LayoutDocuments.SelectedContentIndex = LayoutDocuments.IndexOfChild(item);
		//	//}
		//}

		private void AddDocument(object sender, RoutedEventArgs e)
		{
			var newWorkArea = new LayoutDocument()
			{
				Title = "Work area #" + ++_countWorkArea,
				Content = new WorkAreaControl()
			};

			newWorkArea.Closing += NewWorkArea_Closing;

			LayoutDocuments.Children.Add(newWorkArea);
			
			var offset = LayoutDocuments.Children.Count - 1;
			offset = (offset < 0) ? 0 : offset;

			LayoutDocuments.SelectedContentIndex = offset;
		}

		private void NewWorkArea_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			
		}

		//private void AddDocumentElement_IsSelectedChanged(object sender, EventArgs e)
		//{
		//	var element = (LayoutDocument)sender;

		//	if (!element.IsSelected || LayoutDocuments.Children.Count == 1)
		//		return;

		//	LayoutDocument newWorkArea = new LayoutDocument()
		//	{
		//		Title = "Рабочая область " + ++_countWorkArea,
		//		Content = new WorkAreaControl()
		//	};

		//	var offset = LayoutDocuments.Children.Count - 1;

		//	//if (offset != LayoutDocuments.IndexOfChild(element))
		//	//	return;

		//	LayoutDocuments.Children.RemoveAt(offset);

		//	LayoutDocuments.Children.Add(newWorkArea);
		//	LayoutDocuments.Children.Add(element);
		//	LayoutDocuments.SelectedContentIndex = offset;

		//	//var offset = LayoutDocuments.IndexOfChild(element);
		//	//LayoutDocuments.InsertChildAt(offset, newWorkArea);
		//	//LayoutDocuments.SelectedContentIndex = offset;
		//}

		private void DockingManager_OnActiveContentChanged(object sender, EventArgs e)
        {
            DockingManager.ActiveContent.DoIfElse<WorkAreaControl>(editor =>
			{
				var element = (Xceed.Wpf.AvalonDock.DockingManager)sender;

			}, () =>
			{
				var element = (Xceed.Wpf.AvalonDock.DockingManager)sender;

			});
        }
		
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

		}
		
		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{

		}

		private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (NewControlComboBox.SelectedIndex != -1)
			{
				var workArea = (WorkAreaControl)DockingManager.ActiveContent;
				workArea.AddControl(((ComboBoxItem)NewControlComboBox.SelectedItem).Content.ToString());
				NewControlComboBox.SelectedIndex = -1;
			}
		}
	}
}