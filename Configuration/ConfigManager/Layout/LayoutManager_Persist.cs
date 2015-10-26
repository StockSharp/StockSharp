using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Controls.Docking.Serialization;
using Ecng.Common;
using Ecng.Serialization;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    /// Partial class for handling saving and loading the layout.
    /// </summary>
    public partial class LayoutManager
    {
        private readonly DockSiteLayoutSerializer _layoutSerializer = new DockSiteLayoutSerializer
        {
            SerializationBehavior = DockSiteSerializationBehavior.All,
            DocumentWindowDeserializationBehavior = DockingWindowDeserializationBehavior.AutoCreate,
            ToolWindowDeserializationBehavior = DockingWindowDeserializationBehavior.AutoCreate
        };

        /// <summary>
        /// Handles the <c>DockingWindowDeserializing</c> event of the <see cref="_layoutSerializer" /> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="DockingWindowDeserializingEventArgs" /> instance containing the event data.</param>
        private void LayoutSerializerOnDockingWindowDeserializing(object sender,
            DockingWindowDeserializingEventArgs args)
        {
            // The e.Node property contains the XML data. The e.Window property contains the associated DocumentWindow or ToolWindow, if any. The
            //   window may have been retrieved from the DockSite, or automatically created (when using DockingWindowDeserializationBehavior.AutoCreate).
            //   For the latter case, the e.Window property can be set to a new window. In both cases, any properties can be set.

            // TODO: change to type checking and generic windows

            if (args.Node.Name == "programmaticToolWindow1")
            {
                InitializeProgrammaticToolWindow1(args.Window as ToolWindow);

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
        /// Load the layout from XML file created in <see cref="FolderManager.LayoutFileName" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void LoadLayout()
        {
            if (LayoutFile == null)
                throw new ArgumentNullException(ConfigurationConstants.Layout, @"Layout file is null.");

            if (_isLayoutLoading) return;

            _isLayoutLoading = true;
            try
            {
                //BUG: does not load tool window tab titles or tool window content
                _layoutSerializer.LoadFromFile(LayoutFile.FullName, DockSite);
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
                throw new ArgumentNullException(ConfigurationConstants.Layout, @"Layout file is null or invalid.");

            if (_isLayoutLoading) return;

            _isLayoutLoading = true;
            try
            {
                _layoutSerializer.LoadFromFile(file.FullName, DockSite);
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
        /// Saves layout asynchronously using dispatcher.
        /// </summary>
        /// <param name="dockSite">The dock site to save.</param>
        public void SaveLayout(DockSite dockSite, string fileName)
        {
            if (dockSite == null) throw new ArgumentNullException("dockSite");

            var layoutString = _layoutSerializer.SaveToString(dockSite);

            if(File.Exists(fileName)) File.Delete(fileName);

            try
            {
                File.WriteAllText(fileName, layoutString);
            }
            catch (Exception exception)
            {
                exception.LogError();
            }
        }

        /// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(DockSite dockSite, string fileName)
        {
            //new XmlSerializer<SettingsStorage>().Serialize(dockSite, fileName);
        }

        /// <summary>
        /// Load settings.
        /// </summary>
        /// <param name="storage">Settings storage.</param>
        public void Load(string file)
        {
            new XmlSerializer<SettingsStorage>().Deserialize(file);
        }
    }
}