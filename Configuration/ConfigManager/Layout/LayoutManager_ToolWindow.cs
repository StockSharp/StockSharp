using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls.Docking;

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    ///     Partial class for handling tool window tasks and creation.
    /// </summary>
    public partial class LayoutManager
    {
        /// <summary>
        /// </summary>
        public DeferrableObservableCollection<ToolWindow> ToolItems { get; }

        /// <summary>
        ///     Creates a new <see cref="ToolWindow" />.
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

            // Activate the window
            toolWindow.Activate();

            return toolWindow;
        }

        /// <summary>
        ///     Initializes the "Programmatic Tool Window 1" tool window.
        /// </summary>
        /// <param name="toolWindow">The tool window.</param>
        private void InitializeProgrammaticToolWindow1(ToolWindow toolWindow)
        {
            if (toolWindow == null)
                throw new ArgumentNullException(nameof(toolWindow));

            // Create the tool window content
            var textBox = new TextBox
            {
                BorderThickness = new Thickness(),
                IsReadOnly = true,
                Text = "This ToolWindow was programmatically created in the code-behind."
            };

            toolWindow.Name = "programmaticToolWindow1";
            toolWindow.Title = "Programmatic ToolWindow 1";
            toolWindow.ImageSource = new BitmapImage(new Uri("/Resources/Images/Properties16.png", UriKind.Relative));
            toolWindow.Content = textBox;
        }
    }
}