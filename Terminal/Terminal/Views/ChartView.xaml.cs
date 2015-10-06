namespace Terminal
{
    using System.Windows;
    using System.Windows.Controls;
    using StockSharp.BusinessEntities;

    public partial class ChartView : UserControl
    {
        private Root _root;
        public ChartView()
        {
            InitializeComponent();
            _root = Root.GetInstance();
        }

        public Security Security
        {
            get { return (Security)this.GetValue(SecurityProperty); }
            set { this.SetValue(SecurityProperty, value); }
        }

        public static readonly DependencyProperty SecurityProperty = DependencyProperty.Register(
          "Security", typeof(Security), typeof(ChartView), new PropertyMetadata(null, new PropertyChangedCallback(OnSecurityPropertyChanged)));

        private static void OnSecurityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var chView = (ChartView)obj;
            var security = (Security)e.NewValue;

            if (security == null) return;

        }

    }
}
