#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.Layout.TerminalPublic
File: ILayoutManager.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
using System.Collections.ObjectModel;
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
