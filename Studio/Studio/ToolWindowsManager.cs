#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: ToolWindowsManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using ActiproSoftware.Windows.Controls.Docking;

	using Ecng.Common;

	public class ToolWindowsManager
	{
		private sealed class Item : Disposable
		{
			public ToolWindow Window { get; private set; }
			public MenuItem MenuItem { get; private set; }

			private DockSite DockSite { get; set; }

			public Item(ToolWindow window, DockSite dockSite)
			{
				Window = window;

				MenuItem = new MenuItem
				{
					Header = window.Title,
					IsCheckable = true,
					IsChecked = window.IsOpen,
					Tag = window,
					Name = "MenuItem" + window.Name,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center
				};

				MenuItem.Click += UpdateToolWindow;

				if (null == dockSite)
					return;

				DockSite = dockSite;

				DockSite.WindowClosed += WindowClosed;
				DockSite.WindowOpened += WindowOpened;
			}

			protected override void DisposeManaged()
			{
				MenuItem.Click -= UpdateToolWindow;

				if (null == DockSite)
					return;

				DockSite.WindowClosed -= WindowClosed;
				DockSite.WindowOpened -= WindowOpened;
			}

			private void UpdateToolWindow(object sender, RoutedEventArgs args)
			{
				if (MenuItem.IsChecked)
					Window.Activate();
				else
					Window.Close();
			}

			private void WindowClosed(object sender, DockingWindowEventArgs args)
			{
				if (Equals(Window, args.Window))
					MenuItem.IsChecked = false;
			}

			private void WindowOpened(object sender, DockingWindowEventArgs args)
			{
				if (Equals(Window, args.Window))
					MenuItem.IsChecked = true;
			}
		}

		private readonly List<Item> _items = new List<Item>();

		public void Add(ToolWindow window, DockSite dockSite)
		{
			var item = _items.FirstOrDefault(i => Equals(i.Window, window));

			if (null == item)
				_items.Add(new Item(window, dockSite));
		}

		public void Clear()
		{
			foreach (var item in _items)
				item.Dispose();

			_items.Clear();
		}

		public void AppendMenu(MenuItem menuView)
		{
			foreach (var item in _items)
				menuView.Items.Add(item.MenuItem);
		}

		public void UpdateMenus()
		{
			foreach (var item in _items)
			{
				item.MenuItem.IsChecked = item.Window.IsOpen;
			}
		}
	}
}