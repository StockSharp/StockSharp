using System.Windows;
using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Media;

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    /// Partial class for handling layout docking tasks.
    /// </summary>
    public partial class LayoutManager
    {
        /// <summary>
        /// Creates the <see cref="DockSite" />.
        /// </summary>
        private void CreateDockSite()
        {
            // Create new dock site and add a Workspace
            DockSite = new DockSite { Content = new Workspace() };

            // Add a TabbedMdiHost
            var mdiHost = new TabbedMdiHost();
            DockSite.Workspace.Content = mdiHost;
        }

        /// <summary>
        /// Occurs when the <c>Layout.EvenlyDistributeDocumentsOnly</c> menu item is clicked.
        /// </summary>
        private void DistributeLayoutEvenly()
        {
            // TODO: determine when to call.  Possibly in conjuction with CreateDockSite
            var workspace = DockSite.Workspace;
            if (null == workspace) return;

            var descendents = VisualTreeHelperExtended.GetAllDescendants(workspace, typeof (SplitContainer));

            if (null == descendents) return;

            foreach (SplitContainer splitContainer in descendents)
                splitContainer.ResizeSlots();
        }

        /// <summary>
        /// Occurs when a <c>Button</c> is clicked.
        /// </summary>
        private void DockToDockSite(IDockTarget dockSite, ToolWindow toolWindow, Direction direction)
        {
            if ((direction == Direction.None) || (direction == Direction.Content))
                return;

            toolWindow.Dock(dockSite, direction);
        }

        /// <summary>
        /// Occurs when a <c>Button</c> is clicked.
        /// </summary>
        private void DockToToolWindow(IDockTarget originalToolWindow, ToolWindow toolWindow, Direction direction)
        {
            if (direction == Direction.None)
                return;

            if (Equals(toolWindow, originalToolWindow))
            {
                MessageBox.Show("Can't dock a tool window against itself.");
                return;
            }

            toolWindow.Dock(originalToolWindow, direction);
        }
    }
}