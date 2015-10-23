using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ActiproSoftware.Windows.Controls.Docking;
using StockSharp.Configuration.ConfigManager.Layout.Windows.ViewModels;

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    /// Partial class for handling document window tasks and creation.
    /// </summary>
    public partial class LayoutManager
    {
        /// <summary>
        /// 
        /// </summary>
        public IList<DocumentWindowViewModel> DocumentItems { get; }

        /// <summary>
        /// Creates a new <see cref="DocumentWindow"/>.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <returns>The <see cref="DocumentWindow"/> that was created.</returns>
        private DocumentWindow CreateDocumentWindow(string title)
        {
            // Create a TextBox
            TextBox textBox = new TextBox();
            textBox.BorderThickness = new Thickness();
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            // Initialize
            textBox.Text = $"Document window {++toolWindowIndex} created at {DateTime.Now}.";
            string name = $"DocumentWindow{toolWindowIndex}";

            // Create the window (using this constructor registers the document window with the DockSite)
            DocumentWindow window = new DocumentWindow(_dockSite, name, title,
                new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)), textBox);

            return window;
        }
    }
}
