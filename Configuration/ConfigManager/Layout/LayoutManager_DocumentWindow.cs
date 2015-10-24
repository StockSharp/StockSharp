using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls.Docking;

namespace StockSharp.Configuration.ConfigManager.Layout
{
    /// <summary>
    ///     Partial class for handling document window tasks and creation.
    /// </summary>
    public partial class LayoutManager
    {
        /// <summary>
        /// </summary>
        public DeferrableObservableCollection<DocumentWindow> DocumentItems { get; private set; }

        /// <summary>
        ///     Creates a new <see cref="DocumentWindow" />.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <returns>The <see cref="DocumentWindow" /> that was created.</returns>
        private DocumentWindow CreateDocumentWindow(string title)
        {
            // Create a TextBox
            var textBox = new TextBox();
            textBox.BorderThickness = new Thickness();
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            // Initialize
            textBox.Text = string.Format("Document window {0} created at {1}.", ++toolWindowIndex, DateTime.Now);
            string name = string.Format("DocumentWindow{0}",toolWindowIndex);

            // Create the window (using this constructor registers the document window with the DockSite)
            var window = new DocumentWindow(DockSite, name, title,
                new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)), textBox);

            return window;
        }
    }
}