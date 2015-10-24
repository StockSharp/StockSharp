using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls.Docking;

#pragma warning disable 1591

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    ///     Layout manager.  Uses multiple partial classes for different functions.
    /// </summary>
    public partial class LayoutManager : Window
    {
        private readonly ConfigurationManager _configurationManager;
        private bool _isLayoutLoading;
        private int toolWindowIndex;

        public LayoutManager(ConfigurationManager configurationManager, DockSite dockSite)
        {
            if (configurationManager == null) throw new ArgumentNullException(nameof(configurationManager));
            if (dockSite == null)
            {
                CreateDockSite();
            }

            _configurationManager = configurationManager;
            //_layoutFile = _configurationManager.FolderManager.LayoutDirectory;
            LayoutFile =
                new FileInfo(Path.Combine(_configurationManager.FolderManager.LayoutDirectory,
                    _configurationManager.FolderManager.LayoutFileName));
            _isLayoutLoading = false;

            ToolItems = new DeferrableObservableCollection<ToolWindow>();
            DocumentItems = new DeferrableObservableCollection<DocumentWindow>();
            DockSite = dockSite;

            if (LayoutFile == null) throw new ArgumentNullException(nameof(LayoutFile));

            LayoutSerializer.DockingWindowDeserializing += LayoutSerializerOnDockingWindowDeserializing;
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        public DockSite DockSite { get; private set; }
        public FileInfo LayoutFile { get; }

        /// <summary>
        ///     Unsubscribe from events to prevent memory leaks.
        /// </summary>
        private void Dispose()
        {
            LayoutSerializer.DockingWindowDeserializing -= LayoutSerializerOnDockingWindowDeserializing;
            Loaded -= OnLoaded;
            Closing -= OnClosing;
        }

        /// <summary>
        ///     Saves layout when closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (DockSite != null) SaveLayout(DockSite);
        }

        /// <summary>
        ///     Occurs when the sample is loaded.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">A <see cref="RoutedEventArgs" /> that contains the event data.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Invoke so that we can ensure everything is properly loaded before persisting the layout... even from the control's
            //   Loaded event handler, property values may not yet be properly set from the XAML load
            Dispatcher.BeginInvoke(DispatcherPriority.Send, (DispatcherOperationCallback) (arg =>
            {
                // Activate the first document
                if (DockSite.Documents.Count > 0)
                    DockSite.Documents[0].Activate();
                return null;
            }), null);
        }
    }
}