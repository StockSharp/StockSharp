using System;
using ActiproSoftware.Windows.Controls.Docking;
using Ecng.ComponentModel;
using StockSharp.Configuration.ConfigManager.Layout;

#pragma warning disable 1591

namespace StockSharp.Configuration.ConfigManager
{
    public class ConfigurationManager : ManagerBase
    {
        /// <summary>
        ///     Manager for storing app configuration.
        /// </summary>
        /// <param name="appName">The app name.</param>
        /// <param name="dockSite">The dock site.  If null, a new dock site will be created.</param>
        /// <exception cref="ArgumentNullException">App name required.</exception>
        public ConfigurationManager([NotNull] string appName, DockSite dockSite)
        {
            if (appName == null) throw new ArgumentNullException("appName");

            ConfigurationConstants.ApplicationName = appName;
            FolderManager = new FolderManager(this);
            LayoutManager = new LayoutManager(this, dockSite);
            OutputManager = new OutputManager(this);
            SettingsManager = new SettingsManager(this);
        }

        public FolderManager FolderManager { get; private set; }
        public LayoutManager LayoutManager { get; private set; }
        public OutputManager OutputManager { get; private set; }
        public SettingsManager SettingsManager { get; private set; }
    }
}