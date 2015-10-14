using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ActiproSoftware.Windows.Controls.Docking;

namespace StockSharp.Terminal.Layout
{
	public interface ILayoutManager
	{
		ObservableCollection<DockingWindow> ToolItems { get; }

		void AddTabbedMdiHost(DockSite dockSite);
		DocumentWindow CreateDocumentWindow(DockSite dockSite, string name, string title, ImageSource image, object content);
		void DockToolWindowToDockSite(DockSite dockSite, ToolWindow toolWindow, Dock dock);
		void DockToolWindowToToolWindow(ToolWindow toolWindowParent, ToolWindow toolWindow, Direction direction);

		ToolWindow CreateToolWindow(string layoutKey, string name, string title, object content, bool? canClose);
	}
}
