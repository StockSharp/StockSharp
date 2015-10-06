namespace Terminal
{
    using System.Windows.Media;
    using System.Windows.Controls;

    public abstract class ToolItemViewModel : ViewModelBase
    {
        private ImageSource _imageSource;
        public ImageSource ImageSource
        {
            get { return _imageSource; }
            set
            {
                if (_imageSource.Equals(value)) return;
                _imageSource = value;
                NotifyPropertyChanged();
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;
                _title = value;
                NotifyPropertyChanged();
            }
        }

        private Dock _defaultDock;
        public Dock DefaultDock
        {
            get
            {
                return _defaultDock;
            }
            set
            {
                if (_defaultDock == value)
                    return;
                _defaultDock = value;
                NotifyPropertyChanged();
            }
        }

        private string _dockGroup;
        public string DockGroup
        {
            get
            {
                return _dockGroup;
            }
            set
            {
                if (_dockGroup == value)
                    return;
                _dockGroup = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isInitiallyAutoHidden;
        public bool IsInitiallyAutoHidden
        {
            get
            {
                return _isInitiallyAutoHidden;
            }
            set
            {
                if (_isInitiallyAutoHidden == value)
                    return;
                _isInitiallyAutoHidden = value;
                NotifyPropertyChanged();
            }
        }

    }
}
