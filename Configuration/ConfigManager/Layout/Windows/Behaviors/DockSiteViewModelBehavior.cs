using System;
using System.Collections.Generic;
using System.Windows;
using ActiproSoftware.Windows.Controls.Docking;
using StockSharp.Configuration.ConfigManager.Layout.Windows.ViewModels;

namespace StockSharp.Configuration.ConfigManager.Layout.Windows.Behaviors
{
    /// <summary>
    /// Provides attached behaviors for <see cref="DockSite" /> that properly initializes/opens windows associated with
    /// view-models.
    /// </summary>
    public static class DockSiteViewModelBehavior
    {
        /// <summary>
        /// Gets the first <see cref="ToolWindow" /> associated with the specified dock group.
        /// </summary>
        /// <param name="dockSite">The dock site to search.</param>
        /// <param name="dockGroup">The dock group.</param>
        /// <returns>
        /// A <see cref="ToolWindow" />; otherwise, <see langword="null" />.
        /// </returns>
        private static ToolWindow GetToolWindow(DockSite dockSite, string dockGroup)
        {
            if (dockSite != null && !string.IsNullOrEmpty(dockGroup))
            {
                foreach (var toolWindow in dockSite.ToolWindows)
                {
                    var window = toolWindow.DataContext as ToolWindowViewModel;
                    if (window != null && window.DockGroup == dockGroup)
                        return toolWindow;
                }
            }

            return null;
        }

        /// <summary>
        /// Handles the <c>Loaded</c> event of the <c>DockSite</c> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private static void OnDockSiteLoaded(object sender, RoutedEventArgs e)
        {
            var dockSite = sender as DockSite;
            if (dockSite == null)
                return;

            // Open any windows that were waiting for the DockSite to be loaded
            var windowsPendingOpen = dockSite.GetValue(WindowsPendingOpenProperty) as IList<DockingWindow>;
            dockSite.ClearValue(WindowsPendingOpenProperty);

            if (windowsPendingOpen != null && windowsPendingOpen.Count != 0)
            {
                foreach (var dockingWindow in windowsPendingOpen)
                    OpenDockingWindow(dockSite, dockingWindow);
            }
        }

        /// <summary>
        /// Handles the <c>WindowRegistered</c> event of the <c>DockSite</c> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DockingWindowEventArgs" /> instance containing the event data.</param>
        private static void OnDockSiteWindowRegistered(object sender, DockingWindowEventArgs e)
        {
            var dockSite = sender as DockSite;
            if (dockSite == null)
                return;

            // Ensure the DockingWindow exists and is generated for an item
            var dockingWindow = e.Window;
            if (dockingWindow == null || !dockingWindow.IsContainerForItem)
                return;

            // Pass down the name, if any as this cannot be done via a Style
            if (string.IsNullOrEmpty(dockingWindow.Name))
            {
                ViewModelBase viewModel = dockingWindow.DataContext as ViewModelBase;
                if (viewModel != null && !string.IsNullOrEmpty(viewModel.Name))
                    dockingWindow.Name = viewModel.Name;
            }

            // Open the DockingWindow, if it's not already open
            if (!dockingWindow.IsOpen)
            {
                if (!dockSite.IsLoaded)
                {
                    // Need to delay the opening until after the DockSite is loaded because it's content will not be loaded
                    var windowsPendingOpen = dockSite.GetValue(WindowsPendingOpenProperty) as IList<DockingWindow>;
                    if (windowsPendingOpen == null)
                    {
                        windowsPendingOpen = new List<DockingWindow>();
                        dockSite.SetValue(WindowsPendingOpenProperty, windowsPendingOpen);
                    }

                    windowsPendingOpen.Add(dockingWindow);
                }
                else
                {
                    OpenDockingWindow(dockSite, dockingWindow);
                }
            }
        }

        /// <summary>
        /// Handles the <c>WindowUnregistered</c> event of the <c>DockSite</c> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DockingWindowEventArgs" /> instance containing the event data.</param>
        private static void OnDockSiteWindowUnregistered(object sender, DockingWindowEventArgs e)
        {
            var dockSite = sender as DockSite;
            if (dockSite == null)
                return;

            // Ensure the DockingWindow exists and is generated for an item
            var dockingWindow = e.Window;
            if (dockingWindow == null || !dockingWindow.IsContainerForItem)
                return;

            // Need to remove the window from the list of windows that are waiting to be opened
            var windowsPendingOpen = dockSite.GetValue(WindowsPendingOpenProperty) as IList<DockingWindow>;
            if (windowsPendingOpen != null)
            {
                var index = windowsPendingOpen.IndexOf(dockingWindow);
                if (index != -1)
                    windowsPendingOpen.RemoveAt(index);
            }
        }

        /// <summary>
        /// Called when <see cref="IsManagedProperty" /> is changed.
        /// </summary>
        /// <param name="d">The dependency object that was changed.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void OnIsManagedPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dockSite = d as DockSite;
            if (dockSite == null)
                return;

            // Add/Remove handlers for various events, which will allow us to open/position generated windows
            if ((bool) e.NewValue)
            {
                dockSite.Loaded += OnDockSiteLoaded;
                dockSite.WindowRegistered += OnDockSiteWindowRegistered;
                dockSite.WindowUnregistered += OnDockSiteWindowUnregistered;
            }
            else
            {
                dockSite.Loaded -= OnDockSiteLoaded;
                dockSite.WindowRegistered -= OnDockSiteWindowRegistered;
                dockSite.WindowUnregistered -= OnDockSiteWindowUnregistered;
            }
        }

        /// <summary>
        /// Opens the specified docking window.
        /// </summary>
        /// <param name="dockSite">The dock site that owns the docking window.</param>
        /// <param name="dockingWindow">The docking window to open.</param>
        private static void OpenDockingWindow(DockSite dockSite, DockingWindow dockingWindow)
        {
            if (!dockingWindow.IsOpen)
            {
                if (dockingWindow is DocumentWindow)
                    dockingWindow.Open();
                else
                {
                    var toolWindow = dockingWindow as ToolWindow;
                    ToolWindowViewModel ToolWindowViewModel = dockingWindow.DataContext as ToolWindowViewModel;
                    if (toolWindow != null && ToolWindowViewModel != null)
                    {
                        // Look for a ToolWindow within the same group, if found then dock to that group, otherwise either dock or auto-hide the window
                        var targetToolWindow = GetToolWindow(dockSite, ToolWindowViewModel.DockGroup);
                        if (targetToolWindow != null && targetToolWindow != toolWindow)
                            toolWindow.Dock(targetToolWindow, Direction.Content);
                        else if (ToolWindowViewModel.IsInitiallyAutoHidden)
                            toolWindow.AutoHide(ToolWindowViewModel.DefaultDock);
                        else
                            toolWindow.Dock(dockSite, ToolWindowViewModel.DefaultDock);
                    }
                    else
                    {
                        dockingWindow.Open();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the value of the <see cref="IsManagedProperty" /> attached property for a specified <see cref="DockSite" />.
        /// </summary>
        /// <param name="obj">The object to which the attached property is retrieved.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="DockSite" /> is being managed; otherwise <c>false</c>.
        /// </returns>
        public static bool GetIsManaged(DockSite obj)
        {
            if (null == obj) throw new ArgumentNullException(nameof(obj));
            return (bool) obj.GetValue(IsManagedProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="IsManagedProperty" /> attached property to a specified <see cref="DockSite" />.
        /// </summary>
        /// <param name="obj">The object to which the attached property is written.</param>
        /// <param name="value">
        /// A value indicating whether the specified <see cref="DockSite" /> is being managed.
        /// </param>
        public static void SetIsManaged(DockSite obj, bool value)
        {
            if (null == obj) throw new ArgumentNullException(nameof(obj));
            obj.SetValue(IsManagedProperty, value);
        }

        #region Dependency Properties

        /// <summary>
        /// Identifies the <c>IsManaged</c> attached dependency property.  This field is read-only.
        /// </summary>
        /// <value>The identifier for the <c>IsManaged</c> attached dependency property.</value>
        public static readonly DependencyProperty IsManagedProperty = DependencyProperty.RegisterAttached("IsManaged",
            typeof (bool), typeof (DockSiteViewModelBehavior),
            new FrameworkPropertyMetadata(false, OnIsManagedPropertyValueChanged));

        /// <summary>
        /// Identifies the <c>WindowsPendingOpen</c> attached dependency property.  This field is read-only.
        /// </summary>
        /// <value>The identifier for the <c>WindowsPendingOpen</c> attached dependency property.</value>
        private static readonly DependencyProperty WindowsPendingOpenProperty =
            DependencyProperty.RegisterAttached("WindowsPendingOpen",
                typeof (IList<DockingWindow>), typeof (DockSiteViewModelBehavior), new FrameworkPropertyMetadata(null));

        #endregion // Dependency Properties
    }
}