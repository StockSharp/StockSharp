namespace Terminal
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using ActiproSoftware.Windows.Controls.Docking;

    public static class DockSiteViewModelBehavior
    {

        #region Dependency Properties

        /// <summary>
        /// Identifies the <c>IsManaged</c> attached dependency property.  This field is read-only.
        /// </summary>
        /// <value>The identifier for the <c>IsManaged</c> attached dependency property.</value>
        public static readonly DependencyProperty IsManagedProperty = DependencyProperty.RegisterAttached("IsManaged",
            typeof(bool), typeof(DockSiteViewModelBehavior), new FrameworkPropertyMetadata(false, OnIsManagedPropertyValueChanged));

        /// <summary>
        /// Identifies the <c>WindowsPendingOpen</c> attached dependency property.  This field is read-only.
        /// </summary>
        /// <value>The identifier for the <c>WindowsPendingOpen</c> attached dependency property.</value>
        private static readonly DependencyProperty _windowsPendingOpenProperty = DependencyProperty.RegisterAttached("_windowsPendingOpen",
            typeof(IList<DockingWindow>), typeof(DockSiteViewModelBehavior), new FrameworkPropertyMetadata(null));

        #endregion // Dependency Properties

        /// <summary>
        /// Gets the first <see cref="ToolWindow"/> associated with the specified dock group.
        /// </summary>
        /// <param name="dockSite">The dock site to search.</param>
        /// <param name="dockGroup">The dock group.</param>
        /// <returns>
        /// A <see cref="ToolWindow"/>; otherwise, <see langword="null"/>.
        /// </returns>
        private static ToolWindow GetToolWindow(DockSite dockSite, string dockGroup)
        {
            if (dockSite == null || string.IsNullOrEmpty(dockGroup))
                return null;
            foreach (var toolWindow in dockSite.ToolWindows)
            {
                var toolItemViewModel = toolWindow.DataContext as ToolItemViewModel;
                if (toolItemViewModel != null && toolItemViewModel.DockGroup == dockGroup)
                    return toolWindow;
            }

            return null;
        }

        /// <summary>
        /// Handles the <c>Loaded</c> event of the <c>DockSite</c> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private static void OnDockSiteLoaded(object sender, RoutedEventArgs e)
        {
            var dockSite = sender as DockSite;
            if (dockSite == null)
                return;

            // Open any windows that were waiting for the DockSite to be loaded
            var windowsPendingOpen = dockSite.GetValue(_windowsPendingOpenProperty) as IList<DockingWindow>;
            dockSite.ClearValue(_windowsPendingOpenProperty);

            if (windowsPendingOpen == null || windowsPendingOpen.Count == 0)
                return;
            foreach (var dockingWindow in windowsPendingOpen)
                OpenDockingWindow(dockSite, dockingWindow);
        }

        /// <summary>
        /// Handles the <c>WindowRegistered</c> event of the <c>DockSite</c> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DockingWindowEventArgs"/> instance containing the event data.</param>
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
                var viewModel = dockingWindow.DataContext as ViewModelBase;
                if (viewModel != null && !string.IsNullOrEmpty(viewModel.Name))
                    dockingWindow.Name = viewModel.Name;
            }

            // Open the DockingWindow, if it's not already open
            if (dockingWindow.IsOpen)
                return;
            if (!dockSite.IsLoaded)
            {
                // Need to delay the opening until after the DockSite is loaded because it's content will not be loaded
                var windowsPendingOpen = dockSite.GetValue(_windowsPendingOpenProperty) as IList<DockingWindow>;
                if (windowsPendingOpen == null)
                {
                    windowsPendingOpen = new List<DockingWindow>();
                    dockSite.SetValue(_windowsPendingOpenProperty, windowsPendingOpen);
                }

                windowsPendingOpen.Add(dockingWindow);
            }
            else
            {
                OpenDockingWindow(dockSite, dockingWindow);
            }
        }

        /// <summary>
        /// Handles the <c>WindowUnregistered</c> event of the <c>DockSite</c> control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DockingWindowEventArgs"/> instance containing the event data.</param>
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
            var windowsPendingOpen = dockSite.GetValue(_windowsPendingOpenProperty) as IList<DockingWindow>;
            if (windowsPendingOpen == null)
                return;
            var index = windowsPendingOpen.IndexOf(dockingWindow);
            if (index != -1)
                windowsPendingOpen.RemoveAt(index);
        }

        /// <summary>
        /// Called when <see cref="IsManagedProperty"/> is changed.
        /// </summary>
        /// <param name="d">The dependency object that was changed.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnIsManagedPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dockSite = d as DockSite;
            if (dockSite == null)
                return;

            // Add/Remove handlers for various events, which will allow us to open/position generated windows
            if ((bool)e.NewValue)
            {
                dockSite.Loaded += DockSiteViewModelBehavior.OnDockSiteLoaded;
                dockSite.WindowRegistered += DockSiteViewModelBehavior.OnDockSiteWindowRegistered;
                dockSite.WindowUnregistered += DockSiteViewModelBehavior.OnDockSiteWindowUnregistered;
            }
            else
            {
                dockSite.Loaded -= DockSiteViewModelBehavior.OnDockSiteLoaded;
                dockSite.WindowRegistered -= DockSiteViewModelBehavior.OnDockSiteWindowRegistered;
                dockSite.WindowUnregistered -= DockSiteViewModelBehavior.OnDockSiteWindowUnregistered;
            }
        }

        /// <summary>
        /// Opens the specified docking window.
        /// </summary>
        /// <param name="dockSite">The dock site that owns the docking window.</param>
        /// <param name="dockingWindow">The docking window to open.</param>
        private static void OpenDockingWindow(DockSite dockSite, DockingWindow dockingWindow)
        {
            if (dockingWindow.IsOpen)
                return;
            if (dockingWindow is DocumentWindow)
                dockingWindow.Open();
            else
            {
                var toolWindow = dockingWindow as ToolWindow;
                var toolItemViewModel = dockingWindow.DataContext as ToolItemViewModel;
                if (toolWindow != null && toolItemViewModel != null)
                {
                    // Look for a ToolWindow within the same group, if found then dock to that group, otherwise either dock or auto-hide the window
                    var targetToolWindow = GetToolWindow(dockSite, toolItemViewModel.DockGroup);
                    if (targetToolWindow != null && !Equals(targetToolWindow, toolWindow))
                        toolWindow.Dock(targetToolWindow, Direction.Content);
                    else if (toolItemViewModel.IsInitiallyAutoHidden)
                        toolWindow.AutoHide(toolItemViewModel.DefaultDock);
                    else
                        toolWindow.Dock(dockSite, toolItemViewModel.DefaultDock);
                }
                else
                {
                    dockingWindow.Open();
                }
            }
        }

        /// <summary>
        /// Gets the value of the <see cref="IsManagedProperty"/> attached property for a specified <see cref="DockSite"/>.
        /// </summary>
        /// <param name="obj">The object to which the attached property is retrieved.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="DockSite"/> is being managed; otherwise <c>false</c>.
        /// </returns>
        public static bool GetIsManaged(DockSite obj)
        {
            if (null == obj)
                throw new ArgumentNullException("obj");
            return (bool)obj.GetValue(DockSiteViewModelBehavior.IsManagedProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="IsManagedProperty"/> attached property to a specified <see cref="DockSite"/>.
        /// </summary>
        /// <param name="obj">The object to which the attached property is written.</param>
        /// <param name="value">
        /// A value indicating whether the specified <see cref="DockSite"/> is being managed.
        /// </param>
        public static void SetIsManaged(DockSite obj, bool value)
        {
            if (null == obj)
				throw new ArgumentNullException("obj");

            obj.SetValue(DockSiteViewModelBehavior.IsManagedProperty, value);
        }

    }
}