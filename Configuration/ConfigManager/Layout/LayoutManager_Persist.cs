using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Controls.Docking.Serialization;
using StockSharp.Logging;

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    /// Partial class for handling saving and loading the layout.
    /// </summary>
    public partial class LayoutManager
    {
        private DockSiteLayoutSerializer LayoutSerializer => new DockSiteLayoutSerializer
        {
            SerializationBehavior = DockSiteSerializationBehavior.All,
            DocumentWindowDeserializationBehavior = DockingWindowDeserializationBehavior.AutoCreate,
            ToolWindowDeserializationBehavior = DockingWindowDeserializationBehavior.LazyLoad
        };

        /// <summary>
        /// Handles the <c>DockingWindowDeserializing</c> event of the <see cref="LayoutSerializer"/> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="DockingWindowDeserializingEventArgs"/> instance containing the event data.</param>
        private void LayoutSerializerOnDockingWindowDeserializing(object sender, DockingWindowDeserializingEventArgs args)
        {
            // The e.Node property contains the XML data. The e.Window property contains the associated DocumentWindow or ToolWindow, if any. The
            //   window may have been retrieved from the DockSite, or automatically created (when using DockingWindowDeserializationBehavior.AutoCreate).
            //   For the latter case, the e.Window property can be set to a new window. In both cases, any properties can be set.


            if (args.Node.Name == "programmaticToolWindow1")
            {
                this.InitializeProgrammaticToolWindow1(args.Window as ToolWindow);

                // Change the menu item's header
                //this.activeProgrammaticToolWindow1.Header = "Activate Programmatic ToolWindow 1";
            }
            else if (args.Node.Name == "programmaticToolWindow2")
            {
                // NOTE: We don't need to initialize "programmaticToolWindow2", because it is a custom ToolWindow that sets the appropriate properties when constructed.

                // Change the menu item's header
                //this.activeProgrammaticToolWindow2.Header = "Activate Programmatic ToolWindow 2";
            }
        }

        /// <summary>
        /// Load the layout from XML file created in <see cref="FolderManager.LayoutFile"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void LoadLayout()
        {
            if (_layoutFile == null)
                throw new ArgumentNullException(ConfigConstants.Layout, @"Layout file is null.");

            if (_isLayoutLoading) return;

            _isLayoutLoading = true;
            try
            {
                LayoutSerializer.LoadFromString(_layoutFile, _dockSite);
            }
            catch (Exception e)
            {
                e.LogError();
            }
            finally
            {
                _isLayoutLoading = false;
            }
        }

        /// <summary>
        /// Load layout from XML file located from file dialog.
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void LoadLayout(FileInfo file)
        {
            //TODO: implement file browsing dialog functionality

            if (file == null)
                throw new ArgumentNullException(ConfigConstants.Layout, @"Layout file is null or invalid.");

            if (_isLayoutLoading) return;

            _isLayoutLoading = true;
            try
            {
                LayoutSerializer.LoadFromString(file.FullName, _dockSite);
            }
            catch (Exception e)
            {
                e.LogError();
            }
            finally
            {
                _isLayoutLoading = false;
            }
            
        }

        /// <summary>
        /// Occurs when the <c>File.Load Layout</c> menu item is clicked.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">A <see cref="RoutedEventArgs"/> that contains the event data.</param>
        private void OnFileLoadLayoutMenuItemClick(object sender, RoutedEventArgs e)
        {
            this.LoadLayout();
            MessageBox.Show("Layout loaded from static member variable.");
        }

        /// <summary>
        /// Saves layout asynchronously using dispatcher.
        /// </summary>
        /// <param name="dockSite">The dock site to save.</param>
        public void SaveLayout(DockSite dockSite)
        {
            if (dockSite == null) throw new ArgumentNullException(nameof(dockSite));

            this.Dispatcher.BeginInvoke(DispatcherPriority.Send, (DispatcherOperationCallback)(arg =>
            {
                LayoutSerializer.SaveToFile(_layoutFile, dockSite);
                return null;
            }), null);
        }
    }
}
