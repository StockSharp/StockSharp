using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;
using StockSharp.Configuration.ConfigManager.Layout.Windows.Views;

namespace StockSharp.Configuration.ConfigManager.Layout.Windows.ViewModels
{
    /// <summary>
    /// Generic view model for reusable <see cref="ToolWindowView"/>
    /// </summary>
    public class ToolWindowViewModel : ViewModelBase
    {
        public static readonly DependencyProperty IsLastChildFillProperty = DependencyProperty.Register(
            nameof(IsLastChildFill), typeof (bool), typeof (ToolWindowViewModel), new PropertyMetadata(default(bool)));

        private bool _isLastChildFill;
        private string dockGroup;
        private bool isInitiallyAutoHidden;
        private Dock defaultDock;

        /// <summary>
        /// Fill empty space.
        /// </summary>
        public bool IsLastChildFill
        {
            get { return (bool) _isLastChildFill; }
            set
            {
                if (Equals(_isLastChildFill, value)) return;

                _isLastChildFill = value;
                NotifyPropertyChanged(nameof(IsLastChildFill));
            }
        }

        /// <summary>
		/// Gets or sets the default dock side of the window.
		/// </summary>
		/// <value>The default dock side of the window.</value>
		public Dock DefaultDock
        {
            get
            {
                return this.defaultDock;
            }
            set
            {
                if (this.defaultDock != value)
                {
                    this.defaultDock = value;
                    this.NotifyPropertyChanged(nameof(DefaultDock));
                }
            }
        }

        /// <summary>
		/// Gets or sets a value indicating whether the window should be auto-hidden.
		/// </summary>
		/// <value><c>true</c> if the window should be auto-hidden; otherwise, <c>false</c>.</value>
		public bool IsInitiallyAutoHidden
        {
            get
            {
                return this.isInitiallyAutoHidden;
            }
            set
            {
                if (this.isInitiallyAutoHidden != value)
                {
                    this.isInitiallyAutoHidden = value;
                    this.NotifyPropertyChanged(nameof(IsInitiallyAutoHidden));
                }
            }
        }

        /// <summary>
		/// Gets or sets the dock group associated with the window.
		/// </summary>
		/// <value>The dock group associated with the window.</value>
		public string DockGroup
        {
            get
            {
                return this.dockGroup;
            }
            set
            {
                if (this.dockGroup != value)
                {
                    this.dockGroup = value;
                    this.NotifyPropertyChanged(nameof(DockGroup));
                }
            }
        }
    }
}