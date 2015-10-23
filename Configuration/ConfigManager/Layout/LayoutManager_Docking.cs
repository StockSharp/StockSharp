using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
        /// Creates the <see cref="DockSite"/>.
        /// </summary>
        private void CreateDockSite()
        {
            // Create new dock site and add a Workspace
            _dockSite = new DockSite { Content = new Workspace() };

            // Add a TabbedMdiHost
            TabbedMdiHost mdiHost = new TabbedMdiHost();
            _dockSite.Workspace.Content = mdiHost;

            // Add a couple tool windows attached to each other on the right that are 300px wide
            ToolWindow toolWindowR1 = this.CreateToolWindow("DockedRight-1");
            DockSite.SetControlSize(toolWindowR1, new Size(300, 200));
            ToolWindow toolWindowR2 = this.CreateToolWindow("DockedRight-2");
            toolWindowR1.Dock(_dockSite, Direction.Right);
            toolWindowR2.Dock(toolWindowR1, Direction.Content);

            // Dock bottom
            ToolWindow toolWindowB = this.CreateToolWindow("DockedBottom");
            toolWindowB.Dock(_dockSite.Workspace, Direction.Bottom);

            // Auto hide left
            ToolWindow toolWindowAH = this.CreateToolWindow("Auto-Hidden");
            toolWindowAH.AutoHide(Dock.Left);

            // Floating
            ToolWindow toolWindowU = this.CreateToolWindow("Floating");
            DockSite.SetControlSize(toolWindowU, new Size(400, 200));
            toolWindowU.Float();

            // Add three documents
            DocumentWindow documentWindow1 = this.CreateDocumentWindow("Upper-1");
            documentWindow1.Activate();
            DocumentWindow documentWindow2 = this.CreateDocumentWindow("Upper-2");
            documentWindow2.Activate();
            DocumentWindow documentWindow3 = this.CreateDocumentWindow("Lower");
            documentWindow3.Activate();
            documentWindow3.MoveToNewHorizontalContainer();
        }

        /// <summary>
        /// Occurs when the <c>Layout.EvenlyDistributeDocumentsOnly</c> menu item is clicked.
        /// </summary>
        private void DistributeLayoutEvenly()
        {
            // TODO: determine when to call.  Possibly in conjuction with CreateDockSite
            Workspace workspace = this._dockSite.Workspace;
            if (null == workspace) return;

            IList<DependencyObject> descendents = VisualTreeHelperExtended.GetAllDescendants(workspace, typeof(SplitContainer));

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
