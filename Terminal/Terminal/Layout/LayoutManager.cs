using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Controls.Docking.Serialization;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;

namespace StockSharp.Terminal.Layout
{
	/// <summary>
	/// Class for programmatically creating UI tool windows.
	/// 
	/// Proper use: 
	/// 1) Create DockSite object in Xaml Window or UserControl.
	/// 2) Set x:Name="SomeDockSiteName" and add no other child Xaml components.
	/// 3) Instantiate LayoutManager with the x:Name="SomeDockSiteName" DockSite object.
	/// </summary>
	public class LayoutManager : ILayoutManager
	{
		private int _lastChartWindowId;
		private int _lastDepthWindowId;
		private readonly DockSite _dockSite;

		private readonly ObservableCollection<DockingWindow> _toolItems = new ObservableCollection<DockingWindow>();
		private readonly MainWindow _parent;

		public ObservableCollection<DockingWindow> ToolItems
		{
			get { return _toolItems; }
		}

		private static DockSiteLayoutSerializer LayoutSerializer
		{
			get
			{
				return new DockSiteLayoutSerializer
				{
					SerializationBehavior = DockSiteSerializationBehavior.All,
					DocumentWindowDeserializationBehavior = DockingWindowDeserializationBehavior.AutoCreate,
					ToolWindowDeserializationBehavior = DockingWindowDeserializationBehavior.LazyLoad
				};
			}
		}

		public string LayoutFile;
		public bool IsLoaded;

		public LayoutManager(MainWindow parent, DockSite dockSite)
		{
			ParameterNullCheck(dockSite);
			ParameterNullCheck(parent);

			_parent = parent;

			_dockSite = dockSite;
			_dockSite.Content = new Workspace();
		}

		public void AddTabbedMdiHost(DockSite dockSite)
		{
			ParameterNullCheck(dockSite);

			var mdiHost = new TabbedMdiHost();
			dockSite.Workspace.Content = mdiHost;
		}

		public DocumentWindow CreateDocumentWindow(DockSite dockSite, string name, string title, ImageSource image,
			object content)
		{
			ParameterNullCheck(dockSite);
			// Create the window (using this constructor registers the document window with the DockSite)
			var doc = new DocumentWindow(dockSite, name, title, image, content);

			return doc;
		}

		public void CreateNewChart(Security security)
		{
			if (security == null)
				return;

			_lastChartWindowId++;

			CreateToolWindow(_dockSite, security.Id, "Chart" + _lastChartWindowId, new ChartPanel(), true);
		}

		public void CreateNewMarketDepth(Security security)
		{
			if (security == null)
				return;

			_lastDepthWindowId++;

			if (!_parent.Connector.RegisteredMarketDepths.Contains(security))
				_parent.Connector.RegisterMarketDepth(security);

			var depthControl = new MarketDepthControl();
			depthControl.UpdateFormat(security);

			_parent.Depths.Add(security, depthControl);

			CreateToolWindow(_dockSite, security.Id, "Depth" + _lastDepthWindowId, depthControl, true);
		}

		/// <summary>
		/// Create a simple tool window.
		/// </summary>
		/// <param name="layoutKey">The layout <see cref="LayoutKey"/> key</param>
		/// <param name="name">Window name.</param>
		/// <param name="title">The title.  May not contain spaces.</param>
		/// <param name="content">The window content.</param>
		/// <param name="canClose">Nullable bool.</param>
		/// <returns>A new tool window.</returns>
		public ToolWindow CreateToolWindow(string layoutKey, string name, string title, object content, bool? canClose)
		{
			ParameterNullCheck(layoutKey);

			if (name.Contains(" "))
				throw new ArgumentException(string.Format("Parameter {0} may not contain space(s)", name));


			var tool = new ToolWindow(_dockSite)
			{
				Name = name,
				Title = title,
				Content = content,
				CanClose = canClose
			};
			ToolItems.Add(tool);
			OpenDockingWindow(tool);

			return tool;
		}

		private void CreateToolWindow(DockSite dockSite, string title, string name, object content, bool canClose = false)
		{
			var wnd = new ToolWindow(dockSite)
			{
				Name = name,
				Title = title,
				Content = content,
				CanClose = canClose
			};
			ToolItems.Add(wnd);
			OpenDockingWindow(wnd);
			//return wnd;
		}

		public void DockToolWindowToDockSite(DockSite dockSite, ToolWindow toolWindow, Dock dock)
		{
			ParameterNullCheck(dockSite);
			try
			{
				toolWindow.Dock(dockSite, dock);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Invalid dock site.");
			}
		}

		public void DockToolWindowToToolWindow(ToolWindow toolWindowParent, ToolWindow toolWindow, Direction direction)
		{
			ParameterNullCheck(toolWindowParent);
			try
			{
				toolWindow.Dock(toolWindowParent, direction);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Invalid parent tool window.");
			}
		}

		internal void OpenDockingWindow(DockingWindow dockingWindow)
		{
			if (dockingWindow.IsOpen)
				return;

			var toolWindow = dockingWindow as ToolWindow;

			if (toolWindow != null)
				toolWindow.Dock(_dockSite, Dock.Top);

			if (!IsLoaded)
				return;

			LayoutSerializer.SaveToFile(LayoutFile, _parent.DockSite1);
		}

		private static void ParameterNullCheck(object parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException();
		}
	}
}

/* example code usage
	 LayoutManager.AddTabbedMdiHost(dockSite);

			var docWindow1 = LayoutManager.CreateDocumentWindow(dockSite, LayoutKey.Window, "Chart title", null, new ChartPanel());
			docWindow1.Activate(true);

			// Top right
			var twNews = LayoutManager.CreateToolWindow(LayoutKey.OrderLog, "News", LocalizedStrings.News, new NewsGrid(), true);
			LayoutManager.DockToolWindowToDockSite(dockSite, twNews, Dock.Right);

			// Bottom left
			var twSecurities = LayoutManager.CreateToolWindow(LayoutKey.Security, "Securities", LocalizedStrings.Securities, _secView, true);
			LayoutManager.DockToolWindowToDockSite(dockSite, twSecurities, Dock.Bottom);

			var twMyTrades = LayoutManager.CreateToolWindow(LayoutKey.Trade, "MyTrades", LocalizedStrings.MyTrades, new MyTradeGrid(), true);
			LayoutManager.DockToolWindowToToolWindow(twSecurities, twMyTrades, Direction.Content);

			// Bottom right
			var twOrders = LayoutManager.CreateToolWindow(LayoutKey.Order, "Orders", LocalizedStrings.Orders, new OrderGrid(), true);
			LayoutManager.DockToolWindowToToolWindow(twSecurities, twOrders, Direction.ContentRight);

			var twOrderLog = LayoutManager.CreateToolWindow(LayoutKey.OrderLog, "OrderLog", LocalizedStrings.OrderLog, new OrderLogGrid(), true);
			LayoutManager.DockToolWindowToToolWindow(twOrders, twOrderLog, Direction.Content);

			// Right bottom
			var twPositions = LayoutManager.CreateToolWindow(LayoutKey.Portfolio, "Positions", LocalizedStrings.Str972, new PortfolioGrid(), true);
			LayoutManager.DockToolWindowToToolWindow(twNews, twPositions, Direction.ContentBottom);

			var twTrades = LayoutManager.CreateToolWindow(LayoutKey.Trade, "Trades", LocalizedStrings.Ticks, new TradeGrid(), true);
			LayoutManager.DockToolWindowToToolWindow(twPositions, twTrades, Direction.Content);

			LayoutManager._isLoaded = true;
	*/