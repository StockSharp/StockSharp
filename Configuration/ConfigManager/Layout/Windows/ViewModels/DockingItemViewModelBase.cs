using System.Windows.Media;

namespace StockSharp.Configuration.ConfigManager.Layout.Windows.ViewModels
{
    /// <summary>
    /// Represents a base class for all docking item view-models.
    /// </summary>
    public abstract class DockingItemViewModelBase : ViewModelBase
    {
        private string description;
        private ImageSource imageSource;
        private string title;

        /// <summary>
        /// Gets or sets the description associated with the view-model.
        /// </summary>
        /// <value>The description associated with the view-model.</value>
        public string Description
        {
            get { return description; }
            set
            {
                if (description != value)
                {
                    description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        /// <summary>
        /// Gets or sets the image associated with the view-model.
        /// </summary>
        /// <value>The image associated with the view-model.</value>
        public ImageSource ImageSource
        {
            get { return imageSource; }
            set
            {
                if (imageSource != value)
                {
                    imageSource = value;
                    NotifyPropertyChanged("ImageSource");
                }
            }
        }

        /// <summary>
        /// Gets or sets the title associated with the view-model.
        /// </summary>
        /// <value>The title associated with the view-model.</value>
        public string Title
        {
            get { return title; }
            set
            {
                if (title != value)
                {
                    title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }
    }
}