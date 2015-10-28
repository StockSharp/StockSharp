using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls.Docking;
using StockSharp.Logging;

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    /// Partial class for handling tool window tasks and creation.
    /// </summary>
    public partial class LayoutManager
    {
        /// <summary>
        /// </summary>
        public DeferrableObservableCollection<ToolWindow> ToolItems { get; set;  }

        /// <summary>
        /// Creates a new <see cref="ToolWindow" />.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <returns>The <see cref="ToolWindow" /> that was created.</returns>
        public ToolWindow CreateToolWindow(string title)
        {
            // Create a TextBox
            var textBox = new TextBox();
            textBox.BorderThickness = new Thickness();
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            // Initialize
            textBox.Text = string.Format("Tool window {0} created at {1}.", ++toolWindowIndex, DateTime.Now);
            var name = string.Format("ToolWindow{0}", toolWindowIndex);

            // Create the window (using this constructor registers the tool window with the DockSite)
            var toolWindow = new ToolWindow(DockSite, name, title,
                new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)), textBox);
            ToolItems.Add(toolWindow);

            // Activate the window
            toolWindow.Activate();

            return toolWindow;
        }

        public ToolWindow CreateToolWindow(DockSite dockSite, string name, string title, BitmapImage icon, Type contentType)
        {
            ToolWindow toolWindow = null;
            try
            {
                toolWindow = new ToolWindow(dockSite, name, title, icon, Activator.CreateInstance(contentType));
                ToolItems.Add(toolWindow);
            }
            catch (Exception e)
            {
                e.LogError();
            }

            return toolWindow;
        }
    }
}