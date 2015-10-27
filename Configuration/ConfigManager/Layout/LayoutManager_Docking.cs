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
    }
}