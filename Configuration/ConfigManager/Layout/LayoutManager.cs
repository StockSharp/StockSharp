using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Controls.Docking.Serialization;
using ActiproSoftware.Windows.Media;
using StockSharp.Configuration.ConfigManager.Layout.Windows.ViewModels;
using StockSharp.Logging;

#pragma warning disable 1591

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    /// Layout manager.  Uses multiple partial classes for different functions.
    /// </summary>
    public partial class LayoutManager : Window
    {
        private bool _isLayoutLoading;
        private readonly ConfigurationManager _configurationManager;
        private DockSite _dockSite;
        private readonly string _layoutFile;
        private int toolWindowIndex;

        public LayoutManager(ConfigurationManager configurationManager, DockSite dockSite)
        {
            if (configurationManager == null) throw new ArgumentNullException(nameof(configurationManager));
            if (dockSite == null)
            {
                this.CreateDockSite();
                //throw new ArgumentNullException(nameof(dockSite));
            }

            _configurationManager = configurationManager;
            _isLayoutLoading = false;
            ToolItems = new DeferrableObservableCollection<ToolWindowViewModel>();
            DocumentItems = new DeferrableObservableCollection<DocumentWindowViewModel>();

            _dockSite = dockSite;
            _layoutFile = _configurationManager.FolderManager.LayoutFile;

            if (_layoutFile == null) throw new ArgumentNullException(nameof(_layoutFile));

            this.LayoutSerializer.DockingWindowDeserializing += LayoutSerializerOnDockingWindowDeserializing;
            this.Loaded += OnLoaded;
            this.Closing += OnClosing;
        }

        /// <summary>
        /// Destructor for unsubscribing from events.
        /// </summary>
        ~LayoutManager()
        {
            this.LayoutSerializer.DockingWindowDeserializing -= LayoutSerializerOnDockingWindowDeserializing;
            this.Loaded -= OnLoaded;
            this.Closing -= OnClosing;
        }

        /// <summary>
        /// Saves layout when closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            SaveLayout(this._dockSite);
        }

        /// <summary>
		/// Occurs when the sample is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">A <see cref="RoutedEventArgs"/> that contains the event data.</param>
		private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Invoke so that we can ensure everything is properly loaded before persisting the layout... even from the control's
            //   Loaded event handler, property values may not yet be properly set from the XAML load
            this.Dispatcher.BeginInvoke(DispatcherPriority.Send, (DispatcherOperationCallback)(arg =>
            {
                // Activate the first document
                if (this._dockSite.Documents.Count > 0)
                    _dockSite.Documents[0].Activate();
                return null;
            }), null);
        }
    }
    
}